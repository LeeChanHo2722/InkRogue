using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloorLoadoutUI : MonoBehaviour
{
    private const int SlotCount =
        RunManager.FloorLoadoutSlotsPerHand;

    [Header("Flow")]
    [SerializeField]
    private RunManager runManager;

    [SerializeField]
    private GameObject floorSelectionRoot;

    [SerializeField]
    private GameObject loadoutRoot;

    [SerializeField]
    private Button openButton;

    [SerializeField]
    private Button backButton;

    [Header("Inventory")]
    [SerializeField]
    private FloorLoadoutWeaponItemUI[] inventoryItems;

    [Header("Left Slots: 0 = 12, 1 = 4, 2 = 8")]
    [SerializeField]
    private FloorLoadoutWeaponItemUI[] leftSlots =
        new FloorLoadoutWeaponItemUI[SlotCount];

    [Header("Right Slots: 0 = 12, 1 = 4, 2 = 8")]
    [SerializeField]
    private FloorLoadoutWeaponItemUI[] rightSlots =
        new FloorLoadoutWeaponItemUI[SlotCount];

    private bool initialized;

    private void Awake()
    {
        initialized = ValidateReferences();

        if (!initialized)
            return;

        openButton.onClick.AddListener(Open);
        backButton.onClick.AddListener(Back);
        loadoutRoot.SetActive(false);
    }

    public bool TryAssignWeapon(
        WeaponSlotSide side,
        int slotIndex,
        WeaponDefinition weapon)
    {
        if (!initialized ||
            !runManager.TrySetFloorLoadoutWeapon(
                side,
                slotIndex,
                weapon
            ))
        {
            Debug.LogWarning(
                "Floor Loadout weapon assignment was rejected.",
                this
            );
            return false;
        }

        Refresh();
        return true;
    }

    public bool TryClearSlot(
        WeaponSlotSide side,
        int slotIndex)
    {
        if (!initialized ||
            !runManager.ClearFloorLoadoutSlot(side, slotIndex))
        {
            return false;
        }

        Refresh();
        return true;
    }

    private void Open()
    {
        if (!initialized)
            return;

        loadoutRoot.SetActive(true);
        floorSelectionRoot.SetActive(false);
        Refresh();
    }

    private void Back()
    {
        loadoutRoot.SetActive(false);
        floorSelectionRoot.SetActive(true);
    }

    private void Refresh()
    {
        RefreshInventory();
        RefreshSlots(
            leftSlots,
            WeaponSlotSide.Left,
            runManager.LeftFloorLoadout
        );
        RefreshSlots(
            rightSlots,
            WeaponSlotSide.Right,
            runManager.RightFloorLoadout
        );
    }

    private void RefreshInventory()
    {
        IReadOnlyList<WeaponDefinition> inventory =
            runManager.RunInventory;

        for (int i = 0; i < inventoryItems.Length; i++)
        {
            WeaponDefinition weapon =
                i < inventory.Count ? inventory[i] : null;

            inventoryItems[i].ConfigureInventory(this, weapon);
        }

        if (inventory.Count > inventoryItems.Length)
        {
            Debug.LogWarning(
                "FloorLoadoutUI does not have enough Inventory item views.",
                this
            );
        }
    }

    private void RefreshSlots(
        FloorLoadoutWeaponItemUI[] slots,
        WeaponSlotSide side,
        IReadOnlyList<WeaponDefinition> loadout)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            WeaponDefinition weapon =
                loadout != null && i < loadout.Count
                    ? loadout[i]
                    : null;

            slots[i].ConfigureSlot(
                this,
                side,
                i,
                weapon
            );
        }
    }

    private bool ValidateReferences()
    {
        if (runManager == null ||
            floorSelectionRoot == null ||
            loadoutRoot == null ||
            openButton == null ||
            backButton == null ||
            inventoryItems == null ||
            inventoryItems.Length == 0 ||
            !ValidateSlots(leftSlots) ||
            !ValidateSlots(rightSlots))
        {
            Debug.LogError(
                "FloorLoadoutUI has missing Inspector references.",
                this
            );
            return false;
        }

        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
            {
                Debug.LogError(
                    "FloorLoadoutUI Inventory item " + i + " is missing.",
                    this
                );
                return false;
            }
        }

        return true;
    }

    private static bool ValidateSlots(
        FloorLoadoutWeaponItemUI[] slots)
    {
        if (slots == null || slots.Length != SlotCount)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return false;
        }

        return true;
    }
}
