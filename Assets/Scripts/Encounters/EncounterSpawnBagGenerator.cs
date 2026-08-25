using System;
using System.Collections.Generic;

public static class EncounterSpawnBagGenerator
{
    private const int AllocationSeedSalt = 0x51ED270B;
    private const int ShuffleSeedSalt = 0x2C1B3C6D;

    public static bool TryGenerate(
        EncounterEnemyRatio[] ratios,
        int waveQuota,
        int seed,
        out WaveEnemyType[] bag,
        out string error)
    {
        if (ratios == null || ratios.Length == 0)
        {
            return Fail(
                "Encounter ratios are null or empty.",
                out bag,
                out error);
        }

        if (waveQuota <= 0)
        {
            return Fail(
                $"Wave quota must be positive: {waveQuota}.",
                out bag,
                out error);
        }

        HashSet<WaveEnemyType> enemyTypes =
            new HashSet<WaveEnemyType>();
        double totalPercent = 0d;

        foreach (EncounterEnemyRatio ratio in ratios)
        {
            if (float.IsNaN(ratio.percent)
                || float.IsInfinity(ratio.percent)
                || ratio.percent < 0f)
            {
                return Fail(
                    $"Invalid percent for {ratio.enemyType}: "
                    + $"{ratio.percent}.",
                    out bag,
                    out error);
            }

            if (!enemyTypes.Add(ratio.enemyType))
            {
                return Fail(
                    $"Duplicate enemy ratio entry: {ratio.enemyType}.",
                    out bag,
                    out error);
            }

            totalPercent += ratio.percent;
        }

        if (Math.Abs(
                totalPercent
                - EncounterRatioGenerator.TotalPercent)
            > EncounterRatioGenerator.TotalTolerance)
        {
            return Fail(
                $"Ratio total must be "
                + $"{EncounterRatioGenerator.TotalPercent}: "
                + $"{totalPercent}.",
                out bag,
                out error);
        }

        int entryCount = ratios.Length;
        int[] counts = new int[entryCount];
        double[] remainders = new double[entryCount];
        int[] tieValues = new int[entryCount];
        int[] allocationOrder = new int[entryCount];
        Random allocationRandom = new Random(
            DeriveSeed(seed, AllocationSeedSalt));
        long baseTotal = 0;

        for (int index = 0; index < entryCount; index++)
        {
            double exactCount =
                waveQuota
                * (double)ratios[index].percent
                / EncounterRatioGenerator.TotalPercent;
            int baseCount = (int)Math.Floor(exactCount);

            counts[index] = baseCount;
            remainders[index] = exactCount - baseCount;
            tieValues[index] = allocationRandom.Next();
            allocationOrder[index] = index;
            baseTotal += baseCount;
        }

        long remainingSlots = waveQuota - baseTotal;

        if (remainingSlots < 0
            || remainingSlots > entryCount)
        {
            return Fail(
                $"Largest Remainder cannot assign "
                + $"{remainingSlots} remaining slots.",
                out bag,
                out error);
        }

        Array.Sort(
            allocationOrder,
            (left, right) => CompareAllocationPriority(
                left,
                right,
                remainders,
                tieValues));

        for (int slot = 0; slot < remainingSlots; slot++)
        {
            counts[allocationOrder[slot]]++;
        }

        int countTotal = 0;

        foreach (int count in counts)
        {
            if (count < 0)
            {
                return Fail(
                    "Generated a negative enemy count.",
                    out bag,
                    out error);
            }

            countTotal += count;
        }

        if (countTotal != waveQuota)
        {
            return Fail(
                $"Generated count total is {countTotal}, "
                + $"not {waveQuota}.",
                out bag,
                out error);
        }

        bag = new WaveEnemyType[waveQuota];
        int bagIndex = 0;

        for (int ratioIndex = 0;
             ratioIndex < entryCount;
             ratioIndex++)
        {
            for (int count = 0;
                 count < counts[ratioIndex];
                 count++)
            {
                bag[bagIndex++] = ratios[ratioIndex].enemyType;
            }
        }

        Shuffle(
            bag,
            new Random(DeriveSeed(seed, ShuffleSeedSalt)));

        if (bagIndex != waveQuota
            || bag.Length != waveQuota)
        {
            return Fail(
                "Generated spawn bag length does not match the quota.",
                out bag,
                out error);
        }

        error = string.Empty;
        return true;
    }

    private static int CompareAllocationPriority(
        int left,
        int right,
        double[] remainders,
        int[] tieValues)
    {
        int remainderComparison =
            remainders[right].CompareTo(remainders[left]);

        if (remainderComparison != 0)
        {
            return remainderComparison;
        }

        int tieComparison =
            tieValues[left].CompareTo(tieValues[right]);

        return tieComparison != 0
            ? tieComparison
            : left.CompareTo(right);
    }

    private static int DeriveSeed(int seed, int salt)
    {
        return unchecked(seed * 397 ^ salt);
    }

    private static void Shuffle(
        WaveEnemyType[] bag,
        Random random)
    {
        for (int index = bag.Length - 1;
             index > 0;
             index--)
        {
            int swapIndex = random.Next(index + 1);
            WaveEnemyType value = bag[index];
            bag[index] = bag[swapIndex];
            bag[swapIndex] = value;
        }
    }

    private static bool Fail(
        string message,
        out WaveEnemyType[] bag,
        out string error)
    {
        bag = Array.Empty<WaveEnemyType>();
        error = message;
        return false;
    }
}
