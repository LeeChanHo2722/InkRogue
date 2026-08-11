using UnityEngine;

public class PlayerShotInkStart : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    [Tooltip(
        "잉크가 시작될 위치. "
        + "비워두면 Player 자신의 위치를 사용합니다."
    )]
    public Transform feetOrigin;


    // ==================================================
    // Ink
    // ==================================================

    [Header("Shot Start Ink")]

    [Tooltip("Player 발밑에 찍히는 Ink 크기")]
    public float startInkRadius = 0.30f;


    [Tooltip(
        "발밑에서 FirePoint까지 "
        + "몇 유닛 간격으로 Ink를 찍을지"
    )]
    public float paintSpacing = 0.10f;


    [Tooltip(
        "각 지점의 Ink 불규칙성"
    )]
    public int splatCount = 3;


    [Tooltip(
        "FirePoint보다 살짝 앞까지 "
        + "추가로 칠할 거리"
    )]
    public float extraForwardDistance = 0.15f;


    // ==================================================
    // Public
    // ==================================================

    public void PaintShotStart(
        Vector2 firePosition)
    {
        if (InkMap.Instance == null)
            return;


        Vector2 startPosition;


        if (feetOrigin != null)
        {
            startPosition =
                feetOrigin.position;
        }
        else
        {
            startPosition =
                transform.position;
        }


        // ==========================================
        // Player → FirePoint
        // ==========================================

        Vector2 difference =
            firePosition
            - startPosition;


        float distance =
            difference.magnitude;


        Vector2 direction;


        if (distance >
            0.001f)
        {
            direction =
                difference.normalized;
        }
        else
        {
            direction =
                Vector2.right;
        }


        // ==========================================
        // FirePoint보다 살짝 앞까지 연장
        //
        // 기존 Bullet Ink와 자연스럽게
        // 연결되도록 함
        // ==========================================

        Vector2 endPosition =
            firePosition
            + direction
            * extraForwardDistance;


        float totalDistance =
            Vector2.Distance(
                startPosition,
                endPosition
            );


        float safeSpacing =
            Mathf.Max(
                paintSpacing,
                0.02f
            );


        int paintCount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    totalDistance
                    / safeSpacing
                )
            );


        // ==========================================
        // 발밑부터 총구까지 연속 Paint
        // ==========================================

        for (int i = 0;
             i <= paintCount;
             i++)
        {
            float t =
                (float)i
                / paintCount;


            Vector2 paintPosition =
                Vector2.Lerp(
                    startPosition,
                    endPosition,
                    t
                );


            // 발밑은 조금 넓고
            // 총구로 갈수록 살짝 좁아짐
            float radius =
                Mathf.Lerp(
                    startInkRadius,
                    startInkRadius * 0.70f,
                    t
                );


            InkMap.Instance.PaintExplosion(
                paintPosition,
                radius,
                InkTeam.Player,
                splatCount
            );
        }
    }
}