using UnityEngine;
using UnityEngine.InputSystem;

// Mouse aim shared by the hitscan-style weapons. Pulled out so a second
// weapon does not have to copy the camera/screen-to-world math.
public static class WeaponAim
{
    public static bool TryGetAimAngle(
        Transform usePoint,
        Camera camera,
        out float angleDegrees)
    {
        angleDegrees = 0f;


        if (usePoint == null ||
            camera == null ||
            Mouse.current == null)
        {
            return false;
        }


        Vector2 mouseScreenPosition =
            Mouse.current
                .position
                .ReadValue();


        float cameraDistance =
            Mathf.Abs(
                usePoint.position.z
                -
                camera.transform.position.z
            );


        Vector3 mouseWorldPosition =
            camera.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    cameraDistance
                )
            );


        Vector2 aimDirection =
            new Vector2(
                mouseWorldPosition.x
                -
                usePoint.position.x,

                mouseWorldPosition.y
                -
                usePoint.position.y
            );


        if (aimDirection.sqrMagnitude <
            0.0001f)
        {
            return false;
        }


        angleDegrees =
            Mathf.Atan2(
                aimDirection.y,
                aimDirection.x
            )
            *
            Mathf.Rad2Deg;


        return true;
    }
}
