using System.Linq;
using TMPro;
using UnityEngine;

public partial class FloorSelectionUI
{
    private readonly UnityEngine.UI.Image[] slotIcons = new UnityEngine.UI.Image[6];
    private readonly TMP_Text[] slotStates = new TMP_Text[6];
    private readonly UnityEngine.UI.Outline[] slotBorders = new UnityEngine.UI.Outline[6];
    private readonly UnityEngine.UI.Image[] summaryIcons = new UnityEngine.UI.Image[6];
    private readonly TMP_Text[] summaryNames = new TMP_Text[6];
    private readonly System.Collections.Generic.List<TMP_Text> inventoryStates = new();
    private GameObject loadoutSummary;
    private TMP_Text editingLabel, draftLabel;
    private UnityEngine.UI.Button clearSlot;
    private int editingSlot;
    private bool appliedPreparation;
    private float nextPrimaryInputTime;
    private static readonly Vector2[] SlotPositions = { new(0, 115), new(126, -94), new(-126, -94) };

    private static string Role(WeaponDefinition weapon) => weapon == null ? "빈 슬롯" : weapon.Type switch
    {
        WeaponType.Shooter => "기본 연사 · 누르고 계속 사격",
        WeaponType.SplashBomb => "범위 폭발 · 길게 눌러 멀리 투척",
        WeaponType.MachineGun => "지속 사격 · 쏠수록 연사 가속",
        WeaponType.Breach => "관통 투척 · 재입력으로 돌진",
        WeaponType.Blade => "근접 휩쓸기 · 클릭마다 베기",
        _ => "무기를 선택해 슬롯에 배치"
    };
    private void SelectEquipmentSlot(int index)
    {
        if (!IsPreparing || confirming || index < 0 || index >= slots.Length) return;
        editingSlot = index;
        pickedWeapon = null;
        RefreshEquipment(); // Focusing a slot never changes its weapon.
    }
    private void EquipPickedWeapon(WeaponDefinition weapon)
    {
        if (!IsPreparing || confirming) return;
        PickWeapon(weapon);
        AssignSlot(editingSlot);
    }
    private void ApplyPreparation()
    {
        if (!IsPreparing || confirming) return;
        // One explicit commit point. Repeated apply after closing does nothing.
        for (int i = 0; i < 3; i++) { run.ClearFloorLoadoutSlot(WeaponSlotSide.Left, i); run.ClearFloorLoadoutSlot(WeaponSlotSide.Right, i); }
        for (int i = 0; i < 3; i++)
        {
            if (draftLeft[i] != null) run.TrySetFloorLoadoutWeapon(WeaponSlotSide.Left, i, draftLeft[i]);
            if (draftRight[i] != null) run.TrySetFloorLoadoutWeapon(WeaponSlotSide.Right, i, draftRight[i]);
        }
        RunPersistence.Instance?.Save(RunSaveStage.RouteChoice);
        equipment.SetActive(false);
        pickedWeapon = null;
        appliedPreparation = true;
        nextPrimaryInputTime = Time.unscaledTime + .3f;
        Refresh();
    }
    private void PrimaryAction()
    {
        if (Time.unscaledTime < nextPrimaryInputTime) return;
        if (IsPreparing) ApplyPreparation(); else Confirm();
    }
    private void RefreshEquipment()
    {
        loadoutSummary.SetActive(!IsPreparing);
        for (int i = 0; i < 6; i++)
        {
            var saved = i < 3 ? run.LeftFloorLoadout : run.RightFloorLoadout;
            var draft = i < 3 ? draftLeft : draftRight;
            var weapon = draft[i % 3];
            bool focus = editingSlot == i;
            SetButton(slots[i], weapon != null ? weapon.DisplayName : "비어 있음");
            slotIcons[i].sprite = weapon != null ? weapon.Icon : null;
            slotIcons[i].enabled = weapon != null && weapon.Icon != null;
            slotBorders[i].enabled = focus;
            slotStates[i].text = focus ? "편집 중" : weapon == null ? "빈 슬롯" : "장착됨";
            slotStates[i].color = focus ? Accent : Muted;
            slots[i].GetComponent<UnityEngine.UI.Image>().color = focus ? new Color(.16f, .24f, .19f) : new Color(.10f, .13f, .18f);
            slots[i].interactable = IsPreparing && !confirming;
            var current = saved[i % 3];
            summaryIcons[i].sprite = current != null ? current.Icon : null;
            summaryIcons[i].enabled = current != null && current.Icon != null;
            summaryNames[i].text = current != null ? current.DisplayName : "빈 슬롯";
        }
        if (!IsPreparing)
        {
            hint.text = appliedPreparation ? "편성 적용됨 · 선택한 구역으로 출발하면 이 무기를 사용합니다." : "현재 편성으로 바로 출발하거나, 무기 편성에서 바꿀 수 있습니다.";
            return;
        }
        var selectedDraft = editingSlot < 3 ? draftLeft : draftRight;
        var selectedWeapon = selectedDraft[editingSlot % 3];
        editingLabel.text = $"{(editingSlot < 3 ? "왼손" : "오른손")} 편집 중  ·  {(selectedWeapon != null ? selectedWeapon.DisplayName : "빈 슬롯")}";
        bool dirty = !draftLeft.SequenceEqual(run.LeftFloorLoadout) || !draftRight.SequenceEqual(run.RightFloorLoadout);
        draftLabel.text = dirty ? "변경사항 미적용 · 아래 ‘편성 적용’으로 확정" : "현재 편성 · 슬롯을 고르고 무기를 클릭하세요";
        clearSlot.interactable = selectedWeapon != null;
        hint.text = "취소 / Esc: 변경 취소   ·   같은 손 중복 불가   ·   양손에 같은 무기 배치 가능";
        for (int i = 0; i < inventory.Count; i++)
        {
            var weapon = run.RunInventory[i];
            bool current = selectedWeapon == weapon;
            bool duplicate = WeaponLoadoutRules.ContainsInOtherSlot(selectedDraft, editingSlot % 3, weapon);
            inventory[i].interactable = !duplicate;
            inventory[i].GetComponent<UnityEngine.UI.Image>().color = current ? new Color(.16f, .24f, .19f) : Card;
            inventoryStates[i].text = current ? "현재 슬롯에 장착됨" : duplicate ? "같은 손의 다른 슬롯에 장착됨" : "클릭하여 편집 중인 슬롯에 배치";
            inventoryStates[i].color = current ? Accent : Muted;
        }
    }
    private UnityEngine.UI.Image Icon(Transform parent, WeaponDefinition weapon, float x, float y, float size)
    {
        var go = new GameObject("WeaponIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false); Place((RectTransform)go.transform, x, y, size, size);
        var image = go.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = false; image.preserveAspect = true;
        image.sprite = weapon != null ? weapon.Icon : null; image.enabled = image.sprite != null;
        return image;
    }
    private void BuildEquipment(Transform parent)
    {
        loadoutSummary = new GameObject("CurrentLoadout", typeof(RectTransform));
        loadoutSummary.transform.SetParent(parent, false); Place((RectTransform)loadoutSummary.transform, 0, -303, 1640, 96);
        for (int hand = 0; hand < 2; hand++)
        {
            float x = hand == 0 ? -810 : 20;
            Label(loadoutSummary.transform, hand == 0 ? "왼손  ·  E" : "오른손  ·  Q", 22, x, 34, 190, 38, Accent);
            for (int j = 0; j < 3; j++)
            {
                int i = hand * 3 + j; float cx = x + 30 + j * 260;
                summaryIcons[i] = Icon(loadoutSummary.transform, null, cx, -14, 44);
                summaryNames[i] = Label(loadoutSummary.transform, "", 22, cx + 30, -14, 205, 42, Ink);
            }
        }
        equipment = new GameObject("WeaponPreparation", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        equipment.transform.SetParent(parent, false); Place((RectTransform)equipment.transform, 0, -25, 1740, 650);
        equipment.GetComponent<UnityEngine.UI.Image>().color = Background;
        for (int hand = 0; hand < 2; hand++)
        {
            float center = hand == 0 ? -580 : -30;
            var title = Label(equipment.transform, hand == 0 ? "왼손" : "오른손", 34, center - 230, 265, 460, 48, Ink);
            title.alignment = TextAlignmentOptions.Center;
            var key = Label(equipment.transform, hand == 0 ? "E 휠 · 마우스 오른쪽 공격" : "Q 휠 · 마우스 왼쪽 공격", 22, center - 250, 222, 500, 38, Muted);
            key.alignment = TextAlignmentOptions.Center;
            var centerLabel = Label(equipment.transform, hand == 0 ? "E" : "Q", 32, center - 38, 4, 76, 50, Muted);
            centerLabel.alignment = TextAlignmentOptions.Center;
            for (int j = 0; j < 3; j++)
            {
                int index = hand * 3 + j;
                var p = new Vector2(center, 0) + SlotPositions[j];
                // Same top / lower-right / lower-left arrangement as the combat wheel.
                slots[index] = Button(equipment.transform, "", p.x, p.y, 226, 148, () => SelectEquipmentSlot(index));
                slots[index].name = (hand == 0 ? "LeftSlot" : "RightSlot") + j;
                var label = slots[index].GetComponentInChildren<TMP_Text>(); label.fontSize = 23;
                Place(label.rectTransform, 0, -22, 210, 40);
                slotIcons[index] = Icon(slots[index].transform, null, 0, 31, 60);
                slotStates[index] = Label(slots[index].transform, "", 19, -105, -55, 210, 30, Muted);
                slotStates[index].alignment = TextAlignmentOptions.Center;
                slotBorders[index] = slots[index].gameObject.AddComponent<UnityEngine.UI.Outline>();
                slotBorders[index].effectColor = Accent; slotBorders[index].effectDistance = new Vector2(3, -3);
            }
        }
        editingLabel = Label(equipment.transform, "", 27, -825, -215, 1060, 48, Ink);
        clearSlot = Button(equipment.transform, "이 슬롯 비우기", 70, -255, 310, 54, () => EquipPickedWeapon(null));
        Label(equipment.transform, "슬롯 위치는 전투 중 Q / E 휠과 같습니다.", 22, -825, -255, 730, 46, Muted);
        Label(equipment.transform, "보유 무기", 30, 305, 286, 520, 48, Ink);
        for (int i = 0; i < run.RunInventory.Count; i++)
        {
            var weapon = run.RunInventory[i];
            var button = Button(equipment.transform, weapon.DisplayName, 565, 198 - i * 106, 520, 98, () => EquipPickedWeapon(weapon));
            button.name = "Choose" + weapon.Type;
            var name = button.GetComponentInChildren<TMP_Text>(); name.fontSize = 26; name.alignment = TextAlignmentOptions.MidlineLeft;
            Place(name.rectTransform, 44, 27, 394, 36);
            Icon(button.transform, weapon, -215, 0, 66);
            Label(button.transform, Role(weapon), 21, -152, -5, 395, 32, Ink);
            inventoryStates.Add(Label(button.transform, "", 17, -152, -34, 395, 26, Muted));
            inventory.Add(button);
        }
        draftLabel = Label(equipment.transform, "", 23, -825, -306, 1650, 40, Accent);
        equipment.SetActive(false);
    }
}
