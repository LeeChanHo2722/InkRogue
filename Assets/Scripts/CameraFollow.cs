using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public Transform player;

    public PlayerShield playerShield;


    // ==================================================
    // Follow
    // ==================================================

    [Header("Follow")]

    public float smoothTime = 0.25f;


    // ==================================================
    // Mouse Look Ahead
    // ==================================================

    [Header("Mouse Look Ahead")]

    public float lookAheadDistance = 2.2f;

    public float deadZone = 0.3f;


    // ==================================================
    // Camera Bounds
    // ==================================================

    private Tilemap boundsTilemap;


    [Header("Camera Bounds")]

    [Tooltip(
        "카메라 화면 가장자리와 외벽 Bounds 사이의 여유. "
        + "값을 키우면 카메라가 벽 안쪽에서 더 일찍 멈춥니다."
    )]
    public float boundsPadding = 0.2f;


    // ==================================================
    // Camera Shake
    // ==================================================

    [Header("Normal Hit Shake")]

    public float hitShakeDuration = 0.11f;

    public float hitShakeStrength = 0.10f;


    [Header("Shield Break Shake")]

    public float breakShakeDuration = 0.30f;

    public float breakShakeStrength = 0.32f;


    // ==================================================
    // Runtime
    // ==================================================

    private Vector3 velocity;

    private Vector3 followPosition;

    private float cameraZ;

    private Camera cam;

    private bool cinematicMode = false;

    private float shakeTimer = 0f;

    private float shakeDuration = 0f;

    private float shakeStrength = 0f;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        cam =
            GetComponent<Camera>();


        cameraZ =
            transform.position.z;


        followPosition =
            transform.position;


        if (playerShield == null &&
            player != null)
        {
            playerShield =
                player.GetComponent<PlayerShield>();
        }
    }


    // ==================================================
    // Events
    // ==================================================

    private void OnEnable()
    {
        BindShieldEvents();
    }


    private void OnDisable()
    {
        UnbindShieldEvents();
    }


    private void BindShieldEvents()
    {
        if (playerShield == null &&
            player != null)
        {
            playerShield =
                player.GetComponent<PlayerShield>();
        }


        if (playerShield == null)
            return;


        playerShield.ShieldHit +=
            OnPlayerHit;


        playerShield.ShieldBroken +=
            OnShieldBroken;
    }


    private void UnbindShieldEvents()
    {
        if (playerShield == null)
            return;


        playerShield.ShieldHit -=
            OnPlayerHit;


        playerShield.ShieldBroken -=
            OnShieldBroken;
    }


    // ==================================================
    // Late Update
    // ==================================================

    private void LateUpdate()
    {
        if (cinematicMode)
            return;

        if (player == null ||
            boundsTilemap == null ||
            cam == null)
        {
            return;
        }


        // ==========================================
        // 1. Mouse Look Ahead
        // ==========================================

        Vector3 lookAheadOffset =
            CalculateLookAhead();


        // ==========================================
        // 2. Target
        // ==========================================

        Vector3 targetPosition =
            player.position
            + lookAheadOffset;


        targetPosition.z =
            cameraZ;


        // ==========================================
        // 3. Wall Bounds
        // ==========================================

        targetPosition =
            ClampToMapBounds(
                targetPosition
            );


        // ==========================================
        // 4. Smooth Follow
        // ==========================================

        if (Time.timeScale > 0f)
        {
            followPosition =
                Vector3.SmoothDamp(
                    followPosition,
                    targetPosition,
                    ref velocity,
                    smoothTime
                );
        }


        followPosition.z =
            cameraZ;


        // ==========================================
        // 5. Shake
        // ==========================================

        Vector3 shakeOffset =
            CalculateShakeOffset();


        // ==========================================
        // 6. Final
        // ==========================================

        Vector3 finalPosition =
            followPosition
            + shakeOffset;


        finalPosition.z =
            cameraZ;


        // Shake도 외벽 밖으로 못 나가게
        finalPosition =
            ClampToMapBounds(
                finalPosition
            );


        transform.position =
            finalPosition;
    }


    // ==================================================
    // Switch Camera Bounds
    //
    // Walls → BossWalls
    // ==================================================

    public void SwitchBoundsTilemap(
        Tilemap newBoundsTilemap,
        bool snapImmediately = true)
    {
        if (newBoundsTilemap == null)
        {
            Debug.LogError(
                "CameraFollow: 교체할 Bounds Tilemap이 없습니다."
            );

            return;
        }


        boundsTilemap =
            newBoundsTilemap;


        boundsTilemap.CompressBounds();


        // 이전 맵 방향의 SmoothDamp 관성 제거
        velocity =
            Vector3.zero;


        // 이전 맵 Shake도 제거
        shakeTimer =
            0f;


        shakeDuration =
            0f;


        shakeStrength =
            0f;


        if (snapImmediately)
        {
            SnapToPlayer();
        }


        Debug.Log(
            "Camera Bounds switched to: "
            + boundsTilemap.name
        );
    }


    // ==================================================
    // 이전 코드 호환용
    //
    // 기존 Manager가 아직
    // SwitchGroundTilemap()을 호출해도
    // 컴파일 오류가 나지 않게 유지
    // ==================================================

    public void SwitchGroundTilemap(
        Tilemap newTilemap,
        bool snapImmediately = true)
    {
        SwitchBoundsTilemap(
            newTilemap,
            snapImmediately
        );
    }


    // ==================================================
    // Snap To Player
    // ==================================================

    public void SnapToPlayer()
    {
        if (player == null ||
            boundsTilemap == null ||
            cam == null)
        {
            return;
        }


        Vector3 targetPosition =
            player.position;


        targetPosition.z =
            cameraZ;


        targetPosition =
            ClampToMapBounds(
                targetPosition
            );


        velocity =
            Vector3.zero;


        followPosition =
            targetPosition;


        transform.position =
            targetPosition;


        Debug.Log(
            "CAMERA SNAP"
            + " | Player: "
            + player.position
            + " | Camera: "
            + transform.position
        );
    }


    // ==================================================
    // Mouse Look Ahead
    // ==================================================

    private Vector3 CalculateLookAhead()
    {
        if (Mouse.current == null)
        {
            return Vector3.zero;
        }


        Vector2 mousePosition =
            Mouse.current.position
                .ReadValue();


        Vector2 screenCenter =
            new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            );


        Vector2 mouseOffset =
            new Vector2(
                (mousePosition.x - screenCenter.x)
                    / (Screen.width * 0.5f),

                (mousePosition.y - screenCenter.y)
                    / (Screen.height * 0.5f)
            );


        mouseOffset =
            Vector2.ClampMagnitude(
                mouseOffset,
                1f
            );


        float magnitude =
            mouseOffset.magnitude;


        if (magnitude < deadZone)
        {
            mouseOffset =
                Vector2.zero;
        }
        else
        {
            float adjustedMagnitude =
                (magnitude - deadZone)
                / (1f - deadZone);


            mouseOffset =
                mouseOffset.normalized
                * adjustedMagnitude;
        }


        return new Vector3(
            mouseOffset.x,
            mouseOffset.y,
            0f
        )
        * lookAheadDistance;
    }


    // ==================================================
    // Hit
    // ==================================================

    private void OnPlayerHit()
    {
        StartShake(
            hitShakeDuration,
            hitShakeStrength
        );
    }


    private void OnShieldBroken()
    {
        StartShake(
            breakShakeDuration,
            breakShakeStrength
        );
    }


    // ==================================================
    // Shake
    // ==================================================

    public void StartShake(
        float duration,
        float strength)
    {
        if (duration <= 0f ||
            strength <= 0f)
        {
            return;
        }


        shakeDuration =
            Mathf.Max(
                duration,
                0.001f
            );


        shakeTimer =
            shakeDuration;


        shakeStrength =
            Mathf.Max(
                shakeStrength,
                strength
            );
    }


    private Vector3 CalculateShakeOffset()
    {
        if (shakeTimer <= 0f)
        {
            shakeTimer =
                0f;


            shakeStrength =
                0f;


            return Vector3.zero;
        }


        shakeTimer -=
            Time.unscaledDeltaTime;


        float remaining =
            Mathf.Clamp01(
                shakeTimer
                /
                Mathf.Max(
                    shakeDuration,
                    0.001f
                )
            );


        float fade =
            remaining
            * remaining;


        Vector2 randomDirection =
            Random.insideUnitCircle;


        Vector2 offset =
            randomDirection
            * shakeStrength
            * fade;


        if (shakeTimer <= 0f)
        {
            shakeTimer =
                0f;


            shakeStrength =
                0f;
        }


        return new Vector3(
            offset.x,
            offset.y,
            0f
        );
    }


    // ==================================================
    // Bounds
    // ==================================================

    private Vector3 ClampToMapBounds(
        Vector3 targetPosition)
    {
        if (boundsTilemap == null ||
            cam == null)
        {
            return targetPosition;
        }


        Bounds localBounds =
            boundsTilemap.localBounds;


        Vector3 worldMin =
            boundsTilemap.transform
                .TransformPoint(
                    localBounds.min
                );


        Vector3 worldMax =
            boundsTilemap.transform
                .TransformPoint(
                    localBounds.max
                );


        float cameraHalfHeight =
            cam.orthographicSize;


        float cameraHalfWidth =
            cameraHalfHeight
            * cam.aspect;


        // ==========================================
        // Camera 전체 화면이
        // Wall Bounds 안에 있도록 Center 제한
        // ==========================================

        float minX =
            worldMin.x
            + cameraHalfWidth
            + boundsPadding;


        float maxX =
            worldMax.x
            - cameraHalfWidth
            - boundsPadding;


        float minY =
            worldMin.y
            + cameraHalfHeight
            + boundsPadding;


        float maxY =
            worldMax.y
            - cameraHalfHeight
            - boundsPadding;


        // ==========================================
        // 맵이 Camera보다 작은 경우
        // ==========================================

        if (minX > maxX)
        {
            targetPosition.x =
                (
                    worldMin.x
                    + worldMax.x
                )
                * 0.5f;
        }
        else
        {
            targetPosition.x =
                Mathf.Clamp(
                    targetPosition.x,
                    minX,
                    maxX
                );
        }


        if (minY > maxY)
        {
            targetPosition.y =
                (
                    worldMin.y
                    + worldMax.y
                )
                * 0.5f;
        }
        else
        {
            targetPosition.y =
                Mathf.Clamp(
                    targetPosition.y,
                    minY,
                    maxY
                );
        }


        targetPosition.z =
            cameraZ;


        return targetPosition;
    }
    public void BeginCinematicMode()
    {
        cinematicMode = true;

        // 기존 SmoothDamp 속도가 남아있지 않게 초기화
        velocity = Vector3.zero;
    }


    public void EndCinematicMode()
    {
        cinematicMode = false;

        velocity = Vector3.zero;

        // 현재 카메라 위치에서 자연스럽게 추적 재개
        followPosition = transform.position;
    }


    public IEnumerator MoveToPlayerRealtime(
    float duration
)
    {
        if (player == null)
            yield break;

        cinematicMode = true;


        // ==========================================
        // 시작 위치
        // ==========================================

        Vector3 startPosition =
            transform.position;


        // ==========================================
        // 일반 CameraFollow와 동일한 최종 위치
        //
        // Player 중심뿐만 아니라
        // 현재 Mouse Look Ahead까지 포함
        // ==========================================

        Vector3 lookAheadOffset =
            CalculateLookAhead();


        Vector3 targetPosition =
            player.position
            + lookAheadOffset;


        targetPosition.z =
            cameraZ;


        targetPosition =
            ClampToMapBounds(
                targetPosition
            );


        // ==========================================
        // Camera 이동
        // ==========================================

        float timer = 0f;

        float safeDuration =
            Mathf.Max(
                duration,
                0.01f
            );


        while (timer < safeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / safeDuration
                );


            // SmoothStep
            float eased =
                t * t
                * (3f - 2f * t);


            Vector3 position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    eased
                );


            position =
                ClampToMapBounds(
                    position
                );


            transform.position =
                position;


            yield return null;
        }


        // ==========================================
        // 최종 위치 확정
        // ==========================================

        transform.position =
            targetPosition;


        followPosition =
            targetPosition;


        velocity =
            Vector3.zero;
    }
}