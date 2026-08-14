using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(400)]
public class PlayerWeaponAimRigController : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [SerializeField]
    private Transform aimPivot;


    [SerializeField]
    private Transform rightWeaponGripPoint;


    [SerializeField]
    private Transform leftWeaponGripPoint;


    // ==================================================
    // Weapon Pivots
    // ==================================================

    [Header("Weapon Pivots")]

    [SerializeField]
    private Transform rightWeaponPivot;


    [SerializeField]
    private Transform leftWeaponPivot;


    // ==================================================
    // Camera
    // ==================================================

    private Camera mainCamera;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        mainCamera =
            Camera.main;


        AutoFindReferences();
    }


    // ==================================================
    // Late Update
    // ==================================================

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }


        if (mainCamera == null ||
            Mouse.current == null)
        {
            return;
        }


        Vector3 mouseWorldPosition =
            GetMouseWorldPosition();


        // ==========================================
        // RIGHT SLOT
        // ==========================================

        UpdateWeaponPivot(
            rightWeaponPivot,
            rightWeaponGripPoint,
            mouseWorldPosition
        );


        // ==========================================
        // LEFT SLOT
        // ==========================================

        UpdateWeaponPivot(
            leftWeaponPivot,
            leftWeaponGripPoint,
            mouseWorldPosition
        );
    }


    // ==================================================
    // Update Weapon Pivot
    // ==================================================

    private void UpdateWeaponPivot(
        Transform weaponPivot,
        Transform gripPoint,
        Vector3 mouseWorldPosition
    )
    {
        if (weaponPivot == null ||
            gripPoint == null)
        {
            return;
        }


        // ==========================================
        // 무기의 시작 위치
        // = 실제 손 Grip Point
        // ==========================================

        weaponPivot.position =
            gripPoint.position;


        // ==========================================
        // 중요:
        //
        // Player 중심 기준 AimPivot 회전을
        // 복사하지 않는다.
        //
        // 현재 손 위치 → Mouse
        // 방향을 새로 계산한다.
        // ==========================================

        Vector2 direction =
            new Vector2(
                mouseWorldPosition.x
                    - weaponPivot.position.x,

                mouseWorldPosition.y
                    - weaponPivot.position.y
            );


        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            )
            * Mathf.Rad2Deg;


        weaponPivot.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }


    // ==================================================
    // Mouse World
    // ==================================================

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPosition =
            Mouse.current
                .position
                .ReadValue();


        float distanceFromCamera =
            Mathf.Abs(
                transform.position.z
                - mainCamera.transform.position.z
            );


        Vector3 screenPosition =
            new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                distanceFromCamera
            );


        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                screenPosition
            );


        worldPosition.z =
            transform.position.z;


        return worldPosition;
    }


    // ==================================================
    // AUTO FIND
    // ==================================================

    [ContextMenu(
        "AUTO FIND - Weapon Aim Rig References"
    )]
    private void AutoFindReferences()
    {
        Transform playerRoot =
            transform.root;


        // ==========================================
        // AimPivot
        // ==========================================

        if (aimPivot == null)
        {
            aimPivot =
                playerRoot.Find(
                    "AimPivot"
                );
        }


        // ==========================================
        // PlayerVisual
        // ==========================================

        Transform playerVisual =
            playerRoot.Find(
                "PlayerVisual"
            );


        if (playerVisual == null)
        {
            Debug.LogError(
                "[WeaponAimRig] PlayerVisual을 찾지 못했습니다.",
                this
            );

            return;
        }


        // ==========================================
        // RIGHT GRIP
        // ==========================================

        rightWeaponGripPoint =
            playerVisual.Find(
                "VisualOffset/Hands/"
                + "RightHandAnchor/"
                + "RightHandAimAnchor/"
                + "RightWeaponGripPoint"
            );


        // ==========================================
        // LEFT GRIP
        // ==========================================

        leftWeaponGripPoint =
            playerVisual.Find(
                "VisualOffset/Hands/"
                + "LeftHandAnchor/"
                + "LeftHandAimAnchor/"
                + "LeftWeaponGripPoint"
            );


        // ==========================================
        // WeaponRig
        // ==========================================

        Transform weaponRig =
            playerRoot.Find(
                "WeaponRig"
            );


        if (weaponRig == null)
        {
            Debug.LogError(
                "[WeaponAimRig] WeaponRig을 찾지 못했습니다.",
                this
            );

            return;
        }


        rightWeaponPivot =
            weaponRig.Find(
                "RightWeaponPivot"
            );


        leftWeaponPivot =
            weaponRig.Find(
                "LeftWeaponPivot"
            );


#if UNITY_EDITOR

        UnityEditor.EditorUtility.SetDirty(
            this
        );

#endif


        Debug.Log(
            "[WeaponAimRig] Grip + Mouse Aim 연결 완료.",
            this
        );
    }


    // ==================================================
    // Gizmo
    // ==================================================

    private void OnDrawGizmos()
    {
        if (rightWeaponPivot != null)
        {
            Gizmos.DrawWireSphere(
                rightWeaponPivot.position,
                0.05f
            );


            Gizmos.DrawLine(
                rightWeaponPivot.position,
                rightWeaponPivot.position
                +
                rightWeaponPivot.right
                * 0.5f
            );
        }


        if (leftWeaponPivot != null)
        {
            Gizmos.DrawWireSphere(
                leftWeaponPivot.position,
                0.05f
            );


            Gizmos.DrawLine(
                leftWeaponPivot.position,
                leftWeaponPivot.position
                +
                leftWeaponPivot.right
                * 0.5f
            );
        }
    }
}