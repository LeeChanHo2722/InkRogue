using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSubWeapon : MonoBehaviour
{
    [Header("Bomb")]
    public GameObject splashBombPrefab;
    public Transform firePoint;

    [Header("Ink Cost")]
    [Range(0f, 1f)]
    [Tooltip("최대 Ink 기준 폭탄 1회 소비 비율")]
    public float bombInkCostPercent = 0.5f;

    [Header("Charge")]
    public float minRange = 2.5f;
    public float maxRange = 8f;

    [Tooltip("최대 사거리에 도달하는 데 걸리는 시간")]
    public float maxChargeTime = 1f;

    [Header("Cooldown")]
    public float cooldown = 2.5f;

    [Header("Trajectory Preview")]
    public LineRenderer rangeLine;
    public Transform landingIndicator;
    public LayerMask obstacleLayer;

    [Tooltip("SplashBomb Collider Radius와 동일하게 설정")]
    public float previewRadius = 0.18f;

    [Tooltip("작을수록 Preview가 정확하지만 계산량 증가")]
    public float simulationStep = 0.02f;

    [Header("Preview Appearance")]
    public float lineWidth = 0.08f;

    public Color previewColor =
        new Color(
            0.15f,
            0.75f,
            1f,
            0.9f
        );

    private bool isCharging = false;

    private float chargeStartTime;
    private float nextUseTime = 0f;

    private SplashBomb bombTemplate;
    private PlayerInkResource inkResource;
    private PlayerDive playerDive;

    public bool IsCharging
    {
        get
        {
            return isCharging;
        }
    }

    public float Charge01
    {
        get
        {
            return GetCharge01();
        }
    }

    private void Awake()
    {
        if (splashBombPrefab != null)
        {
            bombTemplate =
                splashBombPrefab
                    .GetComponent<SplashBomb>();
        }

        inkResource =
            GetComponentInParent<PlayerInkResource>();
        playerDive =
            GetComponentInParent<PlayerDive>();
    }

    private void Start()
    {
        SetupLineRenderer();

        HidePreview();
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        // 우클릭 시작
        if (Mouse.current
            .rightButton
            .wasPressedThisFrame)
        {
            BeginCharge();
        }

        // 우클릭 유지
        if (isCharging &&
            Mouse.current
                .rightButton
                .isPressed)
        {
            UpdatePreview();
        }

        // 우클릭 해제
        if (isCharging &&
            Mouse.current
                .rightButton
                .wasReleasedThisFrame)
        {
            ReleaseBomb();
        }
    }

    // ==================================================
    // Ink
    // ==================================================

    private float GetBombInkCost()
    {
        if (inkResource == null)
            return Mathf.Infinity;

        return
            inkResource.MaxInk
            * bombInkCostPercent;
    }

    // ==================================================
    // Charge
    // ==================================================

    private void BeginCharge()
    {
        if (playerDive != null && playerDive.IsSwimForm)
        {
            return;
        }
        // Cooldown 중
        if (Time.time < nextUseTime)
            return;

        if (bombTemplate == null ||
            firePoint == null ||
            inkResource == null)
        {
            return;
        }

        // Ink 50%가 없다면
        // 차징 자체가 시작되지 않음
        float inkCost =
            GetBombInkCost();

        if (!inkResource.HasInk(inkCost))
        {
            return;
        }

        isCharging = true;

        chargeStartTime =
            Time.time;

        ShowPreview();

        UpdatePreview();
    }

    private float GetCharge01()
    {
        if (!isCharging)
            return 0f;

        float safeChargeTime =
            Mathf.Max(
                maxChargeTime,
                0.01f
            );

        return Mathf.Clamp01(
            (
                Time.time
                - chargeStartTime
            )
            / safeChargeTime
        );
    }

    private float GetCurrentRange()
    {
        return Mathf.Lerp(
            minRange,
            maxRange,
            GetCharge01()
        );
    }

    // ==================================================
    // Preview
    // ==================================================

    private void UpdatePreview()
    {
        if (firePoint == null ||
            bombTemplate == null)
        {
            return;
        }

        Vector2 start =
            firePoint.position;

        Vector2 direction =
            firePoint.right;

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector2.right;
        }

        direction.Normalize();

        float targetRange =
            GetCurrentRange();

        List<Vector3> points =
            SimulateTrajectory(
                start,
                direction,
                targetRange
            );

        if (points.Count == 0)
            return;

        if (rangeLine != null)
        {
            rangeLine.positionCount =
                points.Count;

            rangeLine.SetPositions(
                points.ToArray()
            );
        }

        Vector3 landingPoint =
            points[
                points.Count - 1
            ];

        if (landingIndicator != null)
        {
            landingIndicator.position =
                new Vector3(
                    landingPoint.x,
                    landingPoint.y,
                    0f
                );
        }
    }

    // ==================================================
    // Preview Physics
    // ==================================================

    private List<Vector3> SimulateTrajectory(
        Vector2 start,
        Vector2 direction,
        float targetRange)
    {
        List<Vector3> points =
            new List<Vector3>();

        bombTemplate
            .CalculateLaunchVelocity(
                targetRange,
                out float horizontalSpeed,
                out float verticalVelocity
            );

        Vector2 position =
            start;

        Vector2 groundVelocity =
            direction.normalized
            * horizontalSpeed;

        float height =
            0.01f;

        points.Add(
            new Vector3(
                position.x,
                position.y,
                0f
            )
        );

        float safeStep =
            Mathf.Clamp(
                simulationStep,
                0.005f,
                0.05f
            );

        const int maxSteps = 300;

        for (int step = 0;
             step < maxSteps;
             step++)
        {
            verticalVelocity -=
                bombTemplate.gravity
                * safeStep;

            height +=
                verticalVelocity
                * safeStep;

            SimulateHorizontalMovement(
                ref position,
                ref groundVelocity,
                safeStep,
                points
            );

            points.Add(
                new Vector3(
                    position.x,
                    position.y,
                    0f
                )
            );

            if (height <= 0f &&
                verticalVelocity < 0f)
            {
                break;
            }
        }

        return points;
    }

    private void SimulateHorizontalMovement(
        ref Vector2 position,
        ref Vector2 velocity,
        float deltaTime,
        List<Vector3> points)
    {
        float remainingTime =
            deltaTime;

        const int maxBouncesPerStep = 4;

        for (int bounce = 0;
             bounce < maxBouncesPerStep;
             bounce++)
        {
            float speed =
                velocity.magnitude;

            if (speed <
                bombTemplate
                    .minimumGroundSpeed)
            {
                velocity =
                    Vector2.zero;

                return;
            }

            float moveDistance =
                speed
                * remainingTime;

            if (moveDistance <=
                0.0001f)
            {
                return;
            }

            Vector2 moveDirection =
                velocity / speed;

            RaycastHit2D hit =
                Physics2D.CircleCast(
                    position,
                    previewRadius,
                    moveDirection,
                    moveDistance,
                    obstacleLayer
                );

            // 충돌 없음
            if (hit.collider == null)
            {
                position +=
                    velocity
                    * remainingTime;

                return;
            }

            // 벽 충돌 위치
            float travelDistance =
                Mathf.Max(
                    0f,
                    hit.distance
                    - 0.005f
                );

            position +=
                moveDirection
                * travelDistance;

            points.Add(
                new Vector3(
                    position.x,
                    position.y,
                    0f
                )
            );

            float timeToHit =
                hit.distance
                / speed;

            remainingTime -=
                Mathf.Clamp(
                    timeToHit,
                    0f,
                    remainingTime
                );

            // 벽 반사
            velocity =
                Vector2.Reflect(
                    velocity,
                    hit.normal
                );

            velocity *=
                bombTemplate
                    .wallBounceRetention;

            position +=
                hit.normal
                * 0.01f;

            if (remainingTime <=
                0.0001f)
            {
                return;
            }
        }
    }

    // ==================================================
    // Throw
    // ==================================================

    private void ReleaseBomb()
    {
        if (!isCharging)
            return;

        // 아직 Charging 상태일 때
        // Range를 먼저 계산
        float targetRange =
            GetCurrentRange();

        isCharging =
            false;

        HidePreview();

        if (splashBombPrefab == null ||
            firePoint == null ||
            inkResource == null)
        {
            return;
        }

        // 실제 투척 직전 다시 Ink 검사
        //
        // 차징 도중 총을 쏴서 Ink가
        // 50% 미만이 되었을 수도 있기 때문
        float inkCost =
            GetBombInkCost();

        if (!inkResource.TrySpendInk(
                inkCost))
        {
            return;
        }

        Vector2 direction =
            firePoint.right;

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector2.right;
        }

        direction.Normalize();

        GameObject bombObject =
            Instantiate(
                splashBombPrefab,
                firePoint.position,
                Quaternion.identity
            );

        SplashBomb bomb =
            bombObject
                .GetComponent<SplashBomb>();

        if (bomb != null)
        {
            bomb.Launch(
                direction,
                targetRange
            );
        }

        nextUseTime =
            Time.time
            + cooldown;
    }

    // ==================================================
    // Line Renderer
    // ==================================================

    private void SetupLineRenderer()
    {
        if (rangeLine == null)
            return;

        rangeLine.useWorldSpace =
            true;

        rangeLine.startWidth =
            lineWidth;

        rangeLine.endWidth =
            lineWidth;

        rangeLine.startColor =
            previewColor;

        rangeLine.endColor =
            previewColor;

        rangeLine.sortingOrder =
            50;

        rangeLine.numCornerVertices =
            4;

        rangeLine.numCapVertices =
            4;

        if (rangeLine.sharedMaterial == null)
        {
            Shader spriteShader =
                Shader.Find(
                    "Sprites/Default"
                );

            if (spriteShader != null)
            {
                rangeLine.material =
                    new Material(
                        spriteShader
                    );
            }
        }
    }

    // ==================================================
    // Preview Visibility
    // ==================================================

    private void ShowPreview()
    {
        if (rangeLine != null)
        {
            rangeLine.enabled =
                true;
        }

        if (landingIndicator != null)
        {
            landingIndicator
                .gameObject
                .SetActive(true);
        }
    }

    private void HidePreview()
    {
        if (rangeLine != null)
        {
            rangeLine.enabled =
                false;
        }

        if (landingIndicator != null)
        {
            landingIndicator
                .gameObject
                .SetActive(false);
        }
    }
}