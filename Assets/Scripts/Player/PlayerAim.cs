using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    private Camera mainCamera;

    // ==================================================
    // Player Visual
    // ==================================================

    [Header("Player Visual")]
    [SerializeField]
    private PlayerVisualDirectionController visualDirectionController;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        mainCamera = Camera.main;


        // Inspector에서 직접 연결하지 않아도
        // Player 아래의 PlayerVisual을 자동 탐색
        if (visualDirectionController == null)
        {
            visualDirectionController =
                transform.root
                    .GetComponentInChildren<
                        PlayerVisualDirectionController
                    >(true);
        }


        if (visualDirectionController == null)
        {
            Debug.LogWarning(
                "[PlayerAim] PlayerVisualDirectionController를 찾지 못했습니다.",
                this
            );
        }
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (Mouse.current == null ||
            mainCamera == null)
        {
            return;
        }


        // ==========================================
        // Mouse Screen Position
        // ==========================================

        Vector2 mouseScreenPosition =
            Mouse.current.position.ReadValue();


        // ==========================================
        // Mouse World Position
        // ==========================================

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                mouseScreenPosition
            );


        // ==========================================
        // Aim Direction
        // ==========================================

        Vector2 direction =
            mouseWorldPosition
            - transform.position;


        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }


        // ==========================================
        // AimPivot Rotation
        // ==========================================

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            )
            * Mathf.Rad2Deg;


        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );


        // ==========================================
        // Character Direction
        // ==========================================

        visualDirectionController
            ?.SetFacingFromVector(
                direction
            );
    }
}