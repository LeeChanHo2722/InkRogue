using System;
using System.Collections.Generic;

public static class EncounterPlanGenerator
{
    private const int TotalQuotaSeedSalt = 0x0A11CE01;
    private const int WaveSplitSeedSalt = 0x1B22DF12;
    private const int ProfileSeedSalt = 0x2C33E023;
    private const int RefillSeedSalt = 0x3D44F134;

    private static readonly int[] WaveSeedSalts =
    {
        0x4E550245,
        0x5F661356,
        0x60772467
    };

    private static readonly EncounterProfile[] FirstFloorProfiles =
    {
        EncounterProfile.Rush,
        EncounterProfile.Crossfire,
        EncounterProfile.Mixed
    };

    // minTotalQuota / maxTotalQuota arrive already scaled for the current
    // Floor. Generation and validation both use them, so a scaled Plan can
    // never fail against the unscaled asset range.
    public static bool TryGenerate(
        EncounterDifficultyDefinition difficulty,
        int seed,
        bool isFirstFloor,
        int minTotalQuota,
        int maxTotalQuota,
        out EncounterPlan plan,
        out string error)
    {
        if (!TryValidateDifficulty(
                difficulty,
                isFirstFloor,
                out List<EncounterProfilePoolEntry> selectableProfiles,
                out error))
        {
            plan = null;
            return false;
        }

        int totalQuota = GenerateTotalQuota(
            seed,
            isFirstFloor,
            minTotalQuota,
            maxTotalQuota);

        if (!TryGenerateWaveQuotas(
                totalQuota,
                DeriveSeed(seed, WaveSplitSeedSalt),
                out int[] waveQuotas,
                out error))
        {
            plan = null;
            return false;
        }

        if (!TrySelectProfiles(
                selectableProfiles,
                seed,
                isFirstFloor,
                out EncounterProfileDefinition[] profiles,
                out error))
        {
            plan = null;
            return false;
        }

        plan = new EncounterPlan
        {
            seed = seed,
            difficulty = difficulty.Difficulty,
            totalQuota = totalQuota,
            waves = new EncounterWavePlan[3]
        };

        for (int waveIndex = 0; waveIndex < plan.waves.Length; waveIndex++)
        {
            int waveSeed = DeriveSeed(
                seed,
                WaveSeedSalts[waveIndex]);
            EncounterProfileDefinition profile =
                profiles[waveIndex];

            if (!EncounterRatioGenerator.TryGenerate(
                    profile,
                    waveSeed,
                    out EncounterEnemyRatio[] ratios,
                    out error))
            {
                error = $"Wave {waveIndex + 1} ratio generation failed: "
                    + error;
                plan = null;
                return false;
            }

            if (!EncounterSpawnBagGenerator.TryGenerate(
                    ratios,
                    waveQuotas[waveIndex],
                    waveSeed,
                    out WaveEnemyType[] spawnBag,
                    out error))
            {
                error = $"Wave {waveIndex + 1} spawn bag generation failed: "
                    + error;
                plan = null;
                return false;
            }

            plan.waves[waveIndex] = new EncounterWavePlan
            {
                waveIndex = waveIndex,
                seed = waveSeed,
                waveQuota = waveQuotas[waveIndex],
                profile = profile,
                maxAlive = Math.Max(
                    1,
                    difficulty.BaseMaxAlive
                        + profile.MaxAliveModifier),
                refillDelay = GenerateRefillDelay(
                    difficulty,
                    waveSeed),
                enemyRatios = ratios,
                spawnBag = spawnBag
            };
        }

        if (!TryValidatePlan(
                plan,
                difficulty,
                isFirstFloor,
                minTotalQuota,
                maxTotalQuota,
                out error))
        {
            plan = null;
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateDifficulty(
        EncounterDifficultyDefinition difficulty,
        bool isFirstFloor,
        out List<EncounterProfilePoolEntry> selectableProfiles,
        out string error)
    {
        selectableProfiles = new List<EncounterProfilePoolEntry>();

        if (difficulty == null)
        {
            error = "Encounter difficulty is null.";
            return false;
        }

        if (isFirstFloor
            && difficulty.Difficulty != FloorDifficulty.Easy)
        {
            error = "First Floor requires the Easy difficulty definition.";
            return false;
        }

        if (difficulty.MinTotalQuota <= 0
            || difficulty.MinTotalQuota > difficulty.MaxTotalQuota)
        {
            error = $"Invalid quota range: "
                + $"{difficulty.MinTotalQuota}~{difficulty.MaxTotalQuota}.";
            return false;
        }

        if (difficulty.BaseMaxAlive <= 0)
        {
            error = $"Base MaxAlive must be positive: "
                + $"{difficulty.BaseMaxAlive}.";
            return false;
        }

        if (!IsFinite(difficulty.MinRefillDelay)
            || !IsFinite(difficulty.MaxRefillDelay)
            || difficulty.MinRefillDelay < 0f
            || difficulty.MinRefillDelay
                > difficulty.MaxRefillDelay)
        {
            error = $"Invalid refill delay range: "
                + $"{difficulty.MinRefillDelay}~"
                + $"{difficulty.MaxRefillDelay}.";
            return false;
        }

        IReadOnlyList<EncounterProfilePoolEntry> profilePool =
            difficulty.ProfilePool;

        if (profilePool == null || profilePool.Count == 0)
        {
            error = "Encounter profile pool is null or empty.";
            return false;
        }

        HashSet<EncounterProfile> profileTypes =
            new HashSet<EncounterProfile>();

        foreach (EncounterProfilePoolEntry entry in profilePool)
        {
            if (entry == null || entry.profile == null)
            {
                error = "Encounter profile pool contains a null profile.";
                return false;
            }

            if (!profileTypes.Add(entry.profile.Profile))
            {
                error = $"Duplicate EncounterProfile in pool: "
                    + $"{entry.profile.Profile}.";
                return false;
            }

            if (entry.weight > 0)
            {
                selectableProfiles.Add(entry);
            }
        }

        if (selectableProfiles.Count < 3)
        {
            error = "Encounter profile pool needs at least "
                + "3 positive-weight profiles.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static int GenerateTotalQuota(
        int seed,
        bool isFirstFloor,
        int minTotalQuota,
        int maxTotalQuota)
    {
        Random random = new Random(
            DeriveSeed(seed, TotalQuotaSeedSalt));

        return isFirstFloor
            ? random.Next(14, 16)
            : NextInclusive(
                random,
                minTotalQuota,
                maxTotalQuota);
    }

    private static bool TryGenerateWaveQuotas(
        int totalQuota,
        int seed,
        out int[] waveQuotas,
        out string error)
    {
        List<int[]> candidates = new List<int[]>();

        for (int wave1 = 1; wave1 <= totalQuota - 2; wave1++)
        {
            for (int wave2 = 1;
                 wave2 <= totalQuota - wave1 - 1;
                 wave2++)
            {
                int wave3 = totalQuota - wave1 - wave2;

                if (wave1 <= wave2
                    && wave2 <= wave3
                    && IsWithinPercent(wave1, totalQuota, 20, 30)
                    && IsWithinPercent(wave2, totalQuota, 30, 40)
                    && IsWithinPercent(wave3, totalQuota, 35, 45))
                {
                    candidates.Add(new[] { wave1, wave2, wave3 });
                }
            }
        }

        if (candidates.Count == 0)
        {
            waveQuotas = Array.Empty<int>();
            error = $"No valid Wave quota split for total {totalQuota}.";
            return false;
        }

        Random random = new Random(seed);
        waveQuotas = candidates[random.Next(candidates.Count)];
        error = string.Empty;
        return true;
    }

    private static bool TrySelectProfiles(
        List<EncounterProfilePoolEntry> selectableProfiles,
        int seed,
        bool isFirstFloor,
        out EncounterProfileDefinition[] profiles,
        out string error)
    {
        Random random = new Random(
            DeriveSeed(seed, ProfileSeedSalt));

        if (isFirstFloor)
        {
            profiles = new EncounterProfileDefinition[3];

            for (int index = 0;
                 index < FirstFloorProfiles.Length;
                 index++)
            {
                profiles[index] = FindProfile(
                    selectableProfiles,
                    FirstFloorProfiles[index]);

                if (profiles[index] == null)
                {
                    error = $"First Floor profile is missing or disabled: "
                        + $"{FirstFloorProfiles[index]}.";
                    profiles = Array.Empty<EncounterProfileDefinition>();
                    return false;
                }
            }

            Shuffle(profiles, random);
            error = string.Empty;
            return true;
        }

        List<EncounterProfilePoolEntry> remaining =
            new List<EncounterProfilePoolEntry>(selectableProfiles);
        profiles = new EncounterProfileDefinition[3];

        for (int selection = 0; selection < profiles.Length; selection++)
        {
            long totalWeight = 0;

            foreach (EncounterProfilePoolEntry entry in remaining)
            {
                totalWeight += entry.weight;
            }

            if (totalWeight <= 0)
            {
                error = "Profile weights cannot select 3 profiles.";
                profiles = Array.Empty<EncounterProfileDefinition>();
                return false;
            }

            double roll = random.NextDouble() * totalWeight;
            long cumulativeWeight = 0;
            int selectedIndex = remaining.Count - 1;

            for (int index = 0; index < remaining.Count; index++)
            {
                cumulativeWeight += remaining[index].weight;

                if (roll < cumulativeWeight)
                {
                    selectedIndex = index;
                    break;
                }
            }

            profiles[selection] = remaining[selectedIndex].profile;
            remaining.RemoveAt(selectedIndex);
        }

        error = string.Empty;
        return true;
    }

    private static EncounterProfileDefinition FindProfile(
        List<EncounterProfilePoolEntry> profiles,
        EncounterProfile profileType)
    {
        foreach (EncounterProfilePoolEntry entry in profiles)
        {
            if (entry.profile.Profile == profileType)
            {
                return entry.profile;
            }
        }

        return null;
    }

    private static float GenerateRefillDelay(
        EncounterDifficultyDefinition difficulty,
        int waveSeed)
    {
        Random random = new Random(
            DeriveSeed(waveSeed, RefillSeedSalt));

        return difficulty.MinRefillDelay
            + (float)random.NextDouble()
            * (difficulty.MaxRefillDelay
                - difficulty.MinRefillDelay);
    }

    private static bool TryValidatePlan(
        EncounterPlan plan,
        EncounterDifficultyDefinition difficulty,
        bool isFirstFloor,
        int minTotalQuota,
        int maxTotalQuota,
        out string error)
    {
        int minQuota = isFirstFloor
            ? 14
            : minTotalQuota;
        int maxQuota = isFirstFloor
            ? 15
            : maxTotalQuota;

        if (plan.totalQuota < minQuota
            || plan.totalQuota > maxQuota
            || plan.difficulty != difficulty.Difficulty
            || plan.waves == null
            || plan.waves.Length != 3)
        {
            error = "Generated Plan has invalid total quota or Wave count.";
            return false;
        }

        int waveQuotaTotal = 0;
        int previousQuota = 0;
        HashSet<EncounterProfile> profiles =
            new HashSet<EncounterProfile>();

        foreach (EncounterWavePlan wave in plan.waves)
        {
            if (wave == null
                || wave.profile == null
                || wave.waveQuota < previousQuota
                || wave.maxAlive < 1
                || !IsFinite(wave.refillDelay)
                || wave.refillDelay < difficulty.MinRefillDelay
                || wave.refillDelay > difficulty.MaxRefillDelay
                || wave.enemyRatios == null
                || wave.spawnBag == null
                || wave.spawnBag.Length != wave.waveQuota
                || !IsValidWavePercent(
                    wave.waveIndex,
                    wave.waveQuota,
                    plan.totalQuota))
            {
                error = "Generated Wave Plan failed validation.";
                return false;
            }

            if (!profiles.Add(wave.profile.Profile))
            {
                error = "Generated Plan contains duplicate profiles.";
                return false;
            }

            double ratioTotal = 0d;

            foreach (EncounterEnemyRatio ratio in wave.enemyRatios)
            {
                ratioTotal += ratio.percent;
            }

            if (Math.Abs(
                    ratioTotal
                    - EncounterRatioGenerator.TotalPercent)
                > EncounterRatioGenerator.TotalTolerance)
            {
                error = "Generated Wave ratio total is not 100.";
                return false;
            }

            waveQuotaTotal += wave.waveQuota;
            previousQuota = wave.waveQuota;
        }

        if (waveQuotaTotal != plan.totalQuota)
        {
            error = "Generated Wave quotas do not match total quota.";
            return false;
        }

        if (isFirstFloor
            && (plan.difficulty != FloorDifficulty.Easy
                || !profiles.SetEquals(FirstFloorProfiles)))
        {
            error = "Generated First Floor Plan has invalid profiles.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsWithinPercent(
        int value,
        int total,
        int minPercent,
        int maxPercent)
    {
        long scaledValue = (long)value * 100;
        return scaledValue >= (long)total * minPercent
            && scaledValue <= (long)total * maxPercent;
    }

    private static bool IsValidWavePercent(
        int waveIndex,
        int quota,
        int totalQuota)
    {
        switch (waveIndex)
        {
            case 0:
                return IsWithinPercent(
                    quota,
                    totalQuota,
                    20,
                    30);
            case 1:
                return IsWithinPercent(
                    quota,
                    totalQuota,
                    30,
                    40);
            case 2:
                return IsWithinPercent(
                    quota,
                    totalQuota,
                    35,
                    45);
            default:
                return false;
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)
            && !float.IsInfinity(value);
    }

    private static int NextInclusive(
        Random random,
        int minimum,
        int maximum)
    {
        long range = (long)maximum - minimum + 1;
        return minimum + (int)(random.NextDouble() * range);
    }

    private static int DeriveSeed(int seed, int salt)
    {
        return unchecked(seed * 397 ^ salt);
    }

    private static void Shuffle(
        EncounterProfileDefinition[] profiles,
        Random random)
    {
        for (int index = profiles.Length - 1;
             index > 0;
             index--)
        {
            int swapIndex = random.Next(index + 1);
            EncounterProfileDefinition value = profiles[index];
            profiles[index] = profiles[swapIndex];
            profiles[swapIndex] = value;
        }
    }
}
