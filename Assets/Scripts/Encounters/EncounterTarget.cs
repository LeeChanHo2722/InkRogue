using UnityEngine;

// Single place every enemy asks "what am I attacking?". Defaults to the
// Player, and only a Defense Floor points it somewhere else. Enemies read
// this once on Start, so the Floor must set the mode before it spawns any.
public static class EncounterTarget
{
    private static DefenseTarget defenseTarget;

    public static void UsePlayerTarget()
    {
        defenseTarget = null;
    }

    public static void SetDefenseTarget(
        DefenseTarget target)
    {
        defenseTarget = target;
    }

    // Returns null when the scene has no Player tag, matching the old
    // FindGameObjectWithTag behaviour every enemy already handles.
    public static GameObject ResolveGameObject()
    {
        if (defenseTarget != null)
        {
            return defenseTarget.gameObject;
        }

        return GameObject.FindGameObjectWithTag(
            "Player"
        );
    }
}
