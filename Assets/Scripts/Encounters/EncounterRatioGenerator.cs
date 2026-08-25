using System;
using System.Collections.Generic;

[Serializable]
public struct EncounterEnemyRatio
{
    public WaveEnemyType enemyType;
    public float percent;

    public EncounterEnemyRatio(
        WaveEnemyType enemyType,
        float percent)
    {
        this.enemyType = enemyType;
        this.percent = percent;
    }
}

public static class EncounterRatioGenerator
{
    public const float TotalPercent = 100f;
    public const float TotalTolerance = 0.001f;

    public static bool TryGenerate(
        EncounterProfileDefinition profile,
        int seed,
        out EncounterEnemyRatio[] result,
        out string error)
    {
        if (profile == null)
        {
            return Fail(
                "Encounter profile is null.",
                out result,
                out error);
        }

        IReadOnlyList<EnemyRatioRange> ranges =
            profile.EnemyRatios;

        if (ranges == null || ranges.Count == 0)
        {
            return Fail(
                $"Encounter profile {profile.Profile} has no enemy ratios.",
                out result,
                out error);
        }

        int count = ranges.Count;
        double[] percentages = new double[count];
        double[] capacities = new double[count];
        HashSet<WaveEnemyType> enemyTypes =
            new HashSet<WaveEnemyType>();
        double minimumTotal = 0d;
        double maximumTotal = 0d;

        for (int index = 0; index < count; index++)
        {
            EnemyRatioRange range = ranges[index];

            if (range == null)
            {
                return Fail(
                    $"Encounter profile {profile.Profile} contains a null ratio entry.",
                    out result,
                    out error);
            }

            if (!IsFinite(range.minPercent)
                || !IsFinite(range.maxPercent)
                || range.minPercent < 0f
                || range.maxPercent > TotalPercent
                || range.minPercent > range.maxPercent)
            {
                return Fail(
                    $"Invalid ratio range for {range.enemyType}: "
                    + $"{range.minPercent}~{range.maxPercent}.",
                    out result,
                    out error);
            }

            if (!enemyTypes.Add(range.enemyType))
            {
                return Fail(
                    $"Duplicate enemy ratio entry: {range.enemyType}.",
                    out result,
                    out error);
            }

            percentages[index] = range.minPercent;
            capacities[index] =
                range.maxPercent - range.minPercent;
            minimumTotal += range.minPercent;
            maximumTotal += range.maxPercent;
        }

        if (minimumTotal > TotalPercent)
        {
            return Fail(
                $"Minimum ratio total exceeds {TotalPercent}: {minimumTotal}.",
                out result,
                out error);
        }

        if (maximumTotal < TotalPercent)
        {
            return Fail(
                $"Maximum ratio total is below {TotalPercent}: {maximumTotal}.",
                out result,
                out error);
        }

        Random random = new Random(seed);
        int[] order = CreateShuffledOrder(count, random);
        double remaining = TotalPercent - minimumTotal;
        double futureCapacity = maximumTotal - minimumTotal;

        for (int orderIndex = 0;
             orderIndex < order.Length;
             orderIndex++)
        {
            int ratioIndex = order[orderIndex];
            double capacity = capacities[ratioIndex];
            futureCapacity -= capacity;

            double minAdd = Math.Max(
                0d,
                remaining - futureCapacity);
            double maxAdd = Math.Min(
                capacity,
                remaining);
            double add = maxAdd <= minAdd
                ? minAdd
                : minAdd
                    + random.NextDouble()
                    * (maxAdd - minAdd);

            percentages[ratioIndex] += add;
            remaining -= add;
        }

        if (Math.Abs(remaining) > TotalTolerance)
        {
            return Fail(
                $"Ratio generation left {remaining} percent unassigned.",
                out result,
                out error);
        }

        result = new EncounterEnemyRatio[count];
        float generatedTotal = 0f;

        for (int index = 0; index < count; index++)
        {
            float percent = (float)percentages[index];

            if (percent < ranges[index].minPercent
                    - TotalTolerance
                || percent > ranges[index].maxPercent
                    + TotalTolerance)
            {
                return Fail(
                    $"Generated ratio for {ranges[index].enemyType} "
                    + $"is outside its range: {percent}.",
                    out result,
                    out error);
            }

            result[index] = new EncounterEnemyRatio(
                ranges[index].enemyType,
                percent);
            generatedTotal += percent;
        }

        if (Math.Abs(generatedTotal - TotalPercent)
            > TotalTolerance)
        {
            return Fail(
                $"Generated ratio total is {generatedTotal}, not {TotalPercent}.",
                out result,
                out error);
        }

        error = string.Empty;
        return true;
    }

    private static int[] CreateShuffledOrder(
        int count,
        Random random)
    {
        int[] order = new int[count];

        for (int index = 0; index < count; index++)
        {
            order[index] = index;
        }

        for (int index = count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            int value = order[index];
            order[index] = order[swapIndex];
            order[swapIndex] = value;
        }

        return order;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)
            && !float.IsInfinity(value);
    }

    private static bool Fail(
        string message,
        out EncounterEnemyRatio[] result,
        out string error)
    {
        result = Array.Empty<EncounterEnemyRatio>();
        error = message;
        return false;
    }
}
