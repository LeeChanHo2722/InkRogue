using UnityEngine;

public static class KnockbackUtility
{
    // Uses GetComponentInParent so a hit on a child collider (the separate
    // trigger/solid colliders a push-target needs) still finds the owner.
    public static bool TryApply(
        Component hit,
        Vector2 sourcePosition,
        float force)
    {
        if (hit == null || force <= 0f)
        {
            return false;
        }

        IKnockbackReceiver receiver =
            hit.GetComponentInParent<IKnockbackReceiver>();

        if (receiver == null)
        {
            return false;
        }

        receiver.ApplyKnockback(
            sourcePosition,
            force);

        return true;
    }
}
