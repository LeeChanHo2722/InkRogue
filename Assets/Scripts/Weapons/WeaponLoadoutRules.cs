using System.Collections.Generic;

// Shared by committed loadouts and the preparation draft/availability display.
// Empty slots are allowed; the caller owns inventory and index validation.
public static class WeaponLoadoutRules
{
    public static bool ContainsInOtherSlot(IReadOnlyList<WeaponDefinition> slots, int slotIndex, WeaponDefinition weapon)
    {
        if (weapon == null) return false;
        for (int i = 0; i < slots.Count; i++)
            if (i != slotIndex && slots[i] == weapon) return true;
        return false;
    }
}
