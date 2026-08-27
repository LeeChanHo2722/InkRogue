using UnityEngine;

// Pure arithmetic for Run depth. No Random, no Encounter seed, no Unity
// object dependencies: the same Floor number always yields the same result,
// so a Plan stays deterministic and a retry reproduces it exactly.
public static class EncounterFloorScaling
{
    // Floor 1 is the baseline, so every curve starts from an index of 0.
    public static int FloorIndex(
        int floorNumber)
    {
        return Mathf.Max(0, floorNumber - 1);
    }


    // Capped so spawn bag size, Wave-split search cost and pending queues
    // stay bounded. The cap is on the multiplier, never on the absolute
    // quota, so Easy/Normal/Hard never converge at depth.
    public static float QuotaMultiplier(
        int floorNumber,
        float growthPerFloor,
        float maxMultiplier)
    {
        float multiplier =
            1f
            + FloorIndex(floorNumber) * growthPerFloor;

        return Mathf.Clamp(
            multiplier,
            1f,
            Mathf.Max(1f, maxMultiplier));
    }


    // Fast growth up to the knee Floor, then a much slower linear tail.
    // No cap: deep Endless Floors keep asking for more firepower.
    public static float HealthMultiplier(
        int floorNumber,
        float growthPerFloor,
        int softKneeFloor,
        float tailGrowthPerFloor)
    {
        int safeKneeFloor = Mathf.Max(1, softKneeFloor);

        if (floorNumber <= safeKneeFloor)
        {
            return 1f
                + FloorIndex(floorNumber) * growthPerFloor;
        }

        float kneeMultiplier =
            1f
            + (safeKneeFloor - 1) * growthPerFloor;

        return kneeMultiplier
            + (floorNumber - safeKneeFloor)
                * tailGrowthPerFloor;
    }


    // Fast growth, then a soft knee into a very slow linear tail. Integer
    // division keeps it a clean step function with no hard maximum, so deep
    // Endless Floors keep creeping up instead of flat-lining.
    public static int MaxAliveBonus(
        int floorNumber,
        int fastStepFloors,
        int softKneeBonus,
        int tailStepFloors)
    {
        int index = FloorIndex(floorNumber);

        int safeFastStep = Mathf.Max(1, fastStepFloors);
        int safeKnee = Mathf.Max(0, softKneeBonus);
        int safeTailStep = Mathf.Max(1, tailStepFloors);

        int fastSpan = safeFastStep * safeKnee;

        if (index < fastSpan)
        {
            return index / safeFastStep;
        }

        return safeKnee
            + (index - fastSpan) / safeTailStep;
    }
}
