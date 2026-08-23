using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WeaponWheelUI : MonoBehaviour
{
    private const int SlotCount = 3;

    [Serializable]
    private sealed class WeaponWheelSlotVisual
    {
        public Image background;
        public Image icon;
        public GameObject equipped;
        public GameObject highlight;
    }

    [Header("References")]
    [SerializeField] private PlayerWeaponInputController weaponInputController;
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private CanvasGroup visualCanvasGroup;
    [SerializeField] private RectTransform wheelCenter;
    [SerializeField] private Image sideIndicator;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private RectTransform pointerLine;
    [SerializeField] private Image pointerLineImage;
    [SerializeField] private WeaponWheelSlotVisual[] slots =
        new WeaponWheelSlotVisual[SlotCount];

    [Header("Colors")]
    [SerializeField] private Color leftSideColor =
        new Color(0.25f, 0.65f, 1f, 1f);
    [SerializeField] private Color rightSideColor =
        new Color(1f, 0.45f, 0.2f, 1f);
    [SerializeField] private Color filledBackgroundColor =
        new Color(0.12f, 0.14f, 0.17f, 0.92f);
    [SerializeField] private Color emptyBackgroundColor =
        new Color(0.05f, 0.06f, 0.08f, 0.78f);

    private bool referencesValid;
    private bool wasOpen;
    private WeaponSlotSide displayedSide;

    private void Awake()
    {
        if (visualCanvasGroup != null)
        {
            visualCanvasGroup.interactable = false;
            visualCanvasGroup.blocksRaycasts = false;
        }

        if (pointerLine != null)
        {
            pointerLine.pivot = new Vector2(0f, 0.5f);
        }

        HideVisual();
        referencesValid = ValidateReferences();
    }

    private void OnEnable()
    {
        HideVisual();
    }

    private void OnDisable()
    {
        HideVisual();
    }

    private void Update()
    {
        if (!referencesValid)
        {
            HideVisual();
            return;
        }

        if (!weaponInputController.IsWeaponWheelOpen)
        {
            HideVisual();
            return;
        }

        WeaponSlotSide activeSide =
            weaponInputController.ActiveWeaponWheelSide;
        bool refreshContent = !wasOpen || displayedSide != activeSide;

        if (!wasOpen)
        {
            SetVisualVisible(true);
        }

        if (refreshContent)
        {
            UpdatePosition();
            RefreshContent(activeSide);
            displayedSide = activeSide;
        }

        int highlightedIndex =
            weaponInputController.HighlightedWeaponSlotIndex;

        RefreshHighlights(activeSide, highlightedIndex);
        RefreshWeaponName(activeSide, highlightedIndex);
        UpdatePointerLine();

        wasOpen = true;
    }

    private void HideVisual()
    {
        SetVisualVisible(false);

        if (weaponNameText != null)
        {
            weaponNameText.text = string.Empty;
        }

        if (pointerLine != null)
        {
            pointerLine.gameObject.SetActive(false);
        }

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i]?.highlight != null)
                {
                    slots[i].highlight.SetActive(false);
                }

                if (slots[i]?.equipped != null)
                {
                    slots[i].equipped.SetActive(false);
                }
            }
        }

        wasOpen = false;
    }

    private void SetVisualVisible(bool visible)
    {
        if (visualCanvasGroup != null)
        {
            visualCanvasGroup.alpha = visible ? 1f : 0f;
            visualCanvasGroup.interactable = false;
            visualCanvasGroup.blocksRaycasts = false;
        }

        if (visualRoot != null && visualRoot != gameObject)
        {
            visualRoot.SetActive(visible);
        }
    }

    private void UpdatePosition()
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRoot,
                weaponInputController.WeaponWheelOrigin,
                null,
                out Vector2 localPoint))
        {
            wheelCenter.anchoredPosition = localPoint;
        }
    }

    private void RefreshContent(WeaponSlotSide side)
    {
        IReadOnlyList<WeaponDefinition> loadout = GetLoadout(side);
        int activeSlotIndex =
            side == WeaponSlotSide.Left
                ? weaponInputController.ActiveLeftLoadoutIndex
                : weaponInputController.ActiveRightLoadoutIndex;
        Color sideColor =
            side == WeaponSlotSide.Left
                ? leftSideColor
                : rightSideColor;

        sideIndicator.color = sideColor;
        pointerLineImage.color = sideColor;

        for (int i = 0; i < SlotCount; i++)
        {
            WeaponDefinition weapon = GetWeapon(loadout, i);
            WeaponWheelSlotVisual slot = slots[i];
            bool hasWeapon = weapon != null;
            bool hasIcon = hasWeapon && weapon.Icon != null;

            slot.background.color =
                hasWeapon
                    ? filledBackgroundColor
                    : emptyBackgroundColor;
            slot.icon.sprite = hasIcon ? weapon.Icon : null;
            slot.icon.gameObject.SetActive(hasIcon);
            slot.equipped.SetActive(hasWeapon && i == activeSlotIndex);
            slot.highlight.SetActive(false);
        }
    }

    private void RefreshHighlights(
        WeaponSlotSide side,
        int highlightedIndex)
    {
        IReadOnlyList<WeaponDefinition> loadout = GetLoadout(side);

        for (int i = 0; i < SlotCount; i++)
        {
            bool highlighted =
                i == highlightedIndex && GetWeapon(loadout, i) != null;

            slots[i].highlight.SetActive(highlighted);
        }
    }

    private void RefreshWeaponName(
        WeaponSlotSide side,
        int highlightedIndex)
    {
        IReadOnlyList<WeaponDefinition> loadout = GetLoadout(side);
        WeaponDefinition weapon =
            highlightedIndex >= 0 && highlightedIndex < SlotCount
                ? GetWeapon(loadout, highlightedIndex)
                : null;

        weaponNameText.text =
            weapon != null ? weapon.DisplayName ?? string.Empty : string.Empty;
    }

    private void UpdatePointerLine()
    {
        if (Mouse.current == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRoot,
                Mouse.current.position.ReadValue(),
                null,
                out Vector2 mouseLocalPoint))
        {
            pointerLine.gameObject.SetActive(false);
            return;
        }

        Vector2 delta = mouseLocalPoint - wheelCenter.anchoredPosition;
        Vector2 size = pointerLine.sizeDelta;
        size.x = delta.magnitude;

        pointerLine.gameObject.SetActive(true);
        pointerLine.anchoredPosition = Vector2.zero;
        pointerLine.sizeDelta = size;
        pointerLine.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private IReadOnlyList<WeaponDefinition> GetLoadout(WeaponSlotSide side)
    {
        return side == WeaponSlotSide.Left
            ? weaponInputController.LeftCombatLoadout
            : weaponInputController.RightCombatLoadout;
    }

    private static WeaponDefinition GetWeapon(
        IReadOnlyList<WeaponDefinition> loadout,
        int index)
    {
        return loadout != null && index < loadout.Count
            ? loadout[index]
            : null;
    }

    private bool ValidateReferences()
    {
        if (weaponInputController == null)
            return LogValidationError("Weapon Input Controller is missing.");
        if (canvasRoot == null)
            return LogValidationError("Canvas Root is missing.");
        if (visualRoot == null)
            return LogValidationError("Visual Root is missing.");
        if (visualCanvasGroup == null)
            return LogValidationError("Visual CanvasGroup is missing.");
        if (wheelCenter == null)
            return LogValidationError("Wheel Center is missing.");
        if (sideIndicator == null)
            return LogValidationError("Side Indicator is missing.");
        if (weaponNameText == null)
            return LogValidationError("Weapon Name Text is missing.");
        if (pointerLine == null)
            return LogValidationError("Pointer Line is missing.");
        if (pointerLineImage == null)
            return LogValidationError("Pointer Line Image is missing.");
        if (slots == null || slots.Length != SlotCount)
            return LogValidationError("Slots must contain exactly 3 entries.");

        for (int i = 0; i < slots.Length; i++)
        {
            WeaponWheelSlotVisual slot = slots[i];

            if (slot == null ||
                slot.background == null ||
                slot.icon == null ||
                slot.equipped == null ||
                slot.highlight == null)
            {
                return LogValidationError(
                    "Slot " + i + " has missing visual references.");
            }
        }

        return true;
    }

    private bool LogValidationError(string message)
    {
        Debug.LogError("[WeaponWheelUI] " + message, this);
        return false;
    }
}
