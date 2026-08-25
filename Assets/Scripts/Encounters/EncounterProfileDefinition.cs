using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyRatioRange
{
    public WaveEnemyType enemyType;

    [Range(0f, 100f)]
    public float minPercent;

    [Range(0f, 100f)]
    public float maxPercent;
}

[CreateAssetMenu(
    fileName = "EncounterProfile_",
    menuName = "InkRogue/Encounter Profile Definition")]
public class EncounterProfileDefinition : ScriptableObject
{
    [SerializeField]
    private EncounterProfile profile;

    [SerializeField]
    private List<EnemyRatioRange> enemyRatios =
        new List<EnemyRatioRange>();

    [SerializeField]
    private int maxAliveModifier;

    public EncounterProfile Profile => profile;

    public IReadOnlyList<EnemyRatioRange> EnemyRatios =>
        enemyRatios;

    public int MaxAliveModifier => maxAliveModifier;

    private void OnValidate()
    {
        foreach (EnemyRatioRange ratio in enemyRatios)
        {
            if (ratio == null)
            {
                continue;
            }

            ratio.minPercent = Mathf.Clamp(
                ratio.minPercent,
                0f,
                100f);
            ratio.maxPercent = Mathf.Clamp(
                ratio.maxPercent,
                ratio.minPercent,
                100f);
        }
    }
}
