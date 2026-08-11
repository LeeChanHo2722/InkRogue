using System;
using System.Collections.Generic;
using UnityEngine;


// ==================================================
// Enemy Type
// ==================================================

public enum WaveEnemyType
{
    Chaser,
    Shooter,
    Tank,
    Bomber,
    Sprinkler
}


// ==================================================
// Enemy Entry
// ==================================================

[Serializable]
public class WaveEnemyEntry
{
    [Tooltip("생성할 Enemy 종류")]
    public WaveEnemyType enemyType;


    [Min(1)]
    [Tooltip("생성할 수")]
    public int count = 1;
}


// ==================================================
// Wave
// ==================================================

[Serializable]
public class WaveData
{
    [Header("Enemies")]

    public List<WaveEnemyEntry> enemies =
        new List<WaveEnemyEntry>();


    [Header("Advance Condition")]

    [Tooltip(
        "이 시간이 지나면 처치량과 관계없이 "
        + "다음 Wave가 시작됩니다."
    )]
    public float maxWaveDuration = 20f;


    [Range(0.1f, 1f)]
    [Tooltip(
        "다음 Wave 조기 시작에 필요한 "
        + "현재 Wave 적 처치 비율"
    )]
    public float killRatioToAdvance =
        2f / 3f;
}


// ==================================================
// Floor
// ==================================================

[Serializable]
public class FloorWaveData
{
    [Header("Waves")]

    public List<WaveData> waves =
        new List<WaveData>();
}