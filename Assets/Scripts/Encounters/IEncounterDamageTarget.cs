using UnityEngine;

// Anything an Encounter enemy is allowed to attack. PlayerShield and
// DefenseTarget both implement it so enemy damage code needs no branch.
public interface IEncounterDamageTarget
{
    Transform TargetTransform { get; }

    void TakeDamage(
        float damage,
        Vector2 hitSourcePosition);
}
