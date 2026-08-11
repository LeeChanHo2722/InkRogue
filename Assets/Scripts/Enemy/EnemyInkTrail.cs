using UnityEngine;

public class EnemyInkTrail : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]
    public EnemyMovement movement;


    // ==================================================
    // Ink Trail
    // ==================================================

    [Header("Enemy Ink Trail")]

    public float trailRadius = 0.42f;

    public float paintSpacing = 0.24f;

    public int splatCount = 5;


    // ==================================================
    // Player Ink Hit Reaction
    // ==================================================

    [Header("Player Ink Hit Reaction")]

    public float suppressDuration = 1f;

    [Range(0.1f, 1f)]
    public float hitSlowMultiplier = 0.65f;


    // ==================================================
    // Runtime
    // ==================================================

    private Vector2 lastPaintPosition;

    private float suppressTimer = 0f;


    public bool IsSuppressed =>
        suppressTimer > 0f;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (movement == null)
        {
            movement =
                GetComponent<EnemyMovement>();
        }


        lastPaintPosition =
            transform.position;
    }


    // ==================================================
    // On Enable
    // ==================================================

    private void OnEnable()
    {
        lastPaintPosition =
            transform.position;
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (suppressTimer > 0f)
        {
            suppressTimer -=
                Time.deltaTime;


            // Suppression 중 이동한 경로를
            // 나중에 몰아서 칠하지 않게 함
            lastPaintPosition =
                transform.position;


            if (suppressTimer <= 0f)
            {
                EndSuppression();
            }


            return;
        }


        PaintMovementTrail();
    }


    // ==================================================
    // Player Hit
    // ==================================================

    public void OnHitByPlayerInk()
    {
        suppressTimer =
            suppressDuration;


        lastPaintPosition =
            transform.position;


        if (movement != null)
        {
            movement
                .SetSuppressionSpeedMultiplier(
                    hitSlowMultiplier
                );
        }
    }


    // ==================================================
    // End Suppression
    // ==================================================

    private void EndSuppression()
    {
        suppressTimer =
            0f;


        if (movement != null)
        {
            movement
                .SetSuppressionSpeedMultiplier(
                    1f
                );
        }


        lastPaintPosition =
            transform.position;
    }


    // ==================================================
    // Paint
    // ==================================================

    private void PaintMovementTrail()
    {
        if (InkMap.Instance == null)
            return;


        Vector2 currentPosition =
            transform.position;


        Vector2 difference =
            currentPosition
            - lastPaintPosition;


        float distance =
            difference.magnitude;


        if (distance <
            paintSpacing)
        {
            return;
        }


        Vector2 direction =
            difference.normalized;


        int paintCount =
            Mathf.FloorToInt(
                distance
                / paintSpacing
            );


        for (int i = 1;
             i <= paintCount;
             i++)
        {
            Vector2 paintPosition =
                lastPaintPosition
                + direction
                * paintSpacing
                * i;


            InkMap.Instance.PaintExplosion(
                paintPosition,
                trailRadius,
                InkTeam.Enemy,
                splatCount
            );
        }


        lastPaintPosition +=
            direction
            * paintSpacing
            * paintCount;
    }


    // ==================================================
    // Safety
    // ==================================================

    private void OnDisable()
    {
        suppressTimer =
            0f;


        if (movement != null)
        {
            movement
                .SetSuppressionSpeedMultiplier(
                    1f
                );
        }
    }
}