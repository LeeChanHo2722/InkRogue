using UnityEngine;

// Anything an attack can push. Deliberately independent of damage: a
// Player shot pushes the DefenseTarget without hurting it, and a future
// melee weapon will push Enemies through this same call.
public interface IKnockbackReceiver
{
    void ApplyKnockback(
        Vector2 sourcePosition,
        float force);
}
