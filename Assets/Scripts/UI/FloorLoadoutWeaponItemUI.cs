using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FloorLoadoutWeaponItemUI :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [SerializeField]
    private Image icon;

    [SerializeField]
    private TMP_Text weaponNameText;

    [SerializeField]
    private CanvasGroup dragCanvasGroup;

    [SerializeField]
    private Button clearButton;

    private FloorLoadoutUI owner;
    private WeaponDefinition weapon;
    private WeaponSlotSide slotSide;
    private int slotIndex;
    private bool inventorySource;
    private bool slotTarget;
    private bool dragging;
    private RectTransform rectTransform;
    private Vector3 dragStartPosition;

    public WeaponDefinition Weapon => weapon;
    public bool IsInventorySource => inventorySource;

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearSlot);
    }

    private void OnDisable()
    {
        EndDrag();
    }

    public void ConfigureInventory(
        FloorLoadoutUI loadoutUI,
        WeaponDefinition definition)
    {
        owner = loadoutUI;
        weapon = definition;
        inventorySource = true;
        slotTarget = false;
        RefreshVisual();
        gameObject.SetActive(definition != null);
    }

    public void ConfigureSlot(
        FloorLoadoutUI loadoutUI,
        WeaponSlotSide side,
        int index,
        WeaponDefinition definition)
    {
        owner = loadoutUI;
        slotSide = side;
        slotIndex = index;
        weapon = definition;
        inventorySource = false;
        slotTarget = true;
        gameObject.SetActive(true);
        RefreshVisual();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!inventorySource ||
            weapon == null ||
            rectTransform == null ||
            dragCanvasGroup == null)
        {
            return;
        }

        dragging = true;
        dragStartPosition = rectTransform.position;
        dragCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging)
            rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!slotTarget ||
            owner == null ||
            eventData.pointerDrag == null)
        {
            return;
        }

        FloorLoadoutWeaponItemUI source =
            eventData.pointerDrag.GetComponent<
                FloorLoadoutWeaponItemUI>();

        if (source == null ||
            !source.IsInventorySource ||
            source.Weapon == null)
        {
            return;
        }

        owner.TryAssignWeapon(
            slotSide,
            slotIndex,
            source.Weapon
        );
    }

    private void ClearSlot()
    {
        if (slotTarget && owner != null)
            owner.TryClearSlot(slotSide, slotIndex);
    }

    private void RefreshVisual()
    {
        bool hasWeapon = weapon != null;
        bool hasIcon = hasWeapon && weapon.Icon != null;

        if (icon != null)
        {
            icon.sprite = hasIcon ? weapon.Icon : null;
            icon.gameObject.SetActive(hasIcon);
        }

        if (weaponNameText != null)
        {
            weaponNameText.text =
                hasWeapon
                    ? weapon.DisplayName ?? string.Empty
                    : string.Empty;
        }

        if (clearButton != null)
            clearButton.interactable = slotTarget && hasWeapon;
    }

    private void EndDrag()
    {
        if (!dragging)
            return;

        dragging = false;

        if (rectTransform != null)
            rectTransform.position = dragStartPosition;

        if (dragCanvasGroup != null)
            dragCanvasGroup.blocksRaycasts = true;
    }
}
