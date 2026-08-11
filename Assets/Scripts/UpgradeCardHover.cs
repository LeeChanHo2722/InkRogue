using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeCardHover :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    // ==================================================
    // Hover
    // ==================================================

    [Header("Hover")]

    public float hoverScale = 1.07f;

    public float hoverLift = 8f;

    public float smoothSpeed = 12f;


    // ==================================================
    // Press
    // ==================================================

    [Header("Press")]

    public float pressScale = 0.97f;


    // ==================================================
    // Runtime
    // ==================================================

    private RectTransform rectTransform;

    private Vector3 originalScale;

    private Vector2 originalPosition;

    private bool isHovering = false;

    private bool isPressed = false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();


        originalScale =
            rectTransform.localScale;


        originalPosition =
            rectTransform.anchoredPosition;
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (rectTransform == null)
            return;


        float targetScale =
            1f;


        if (isPressed)
        {
            targetScale =
                pressScale;
        }
        else if (isHovering)
        {
            targetScale =
                hoverScale;
        }


        Vector3 desiredScale =
            originalScale
            * targetScale;


        Vector2 desiredPosition =
            originalPosition;


        if (isHovering &&
            !isPressed)
        {
            desiredPosition.y +=
                hoverLift;
        }


        // Upgrade 화면은
        // Time.timeScale = 0 이므로
        // unscaledDeltaTime 사용
        float smoothT =
            1f -
            Mathf.Exp(
                -smoothSpeed
                * Time.unscaledDeltaTime
            );


        rectTransform.localScale =
            Vector3.Lerp(
                rectTransform.localScale,
                desiredScale,
                smoothT
            );


        rectTransform.anchoredPosition =
            Vector2.Lerp(
                rectTransform.anchoredPosition,
                desiredPosition,
                smoothT
            );
    }


    // ==================================================
    // Pointer
    // ==================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isHovering =
            true;
    }


    public void OnPointerExit(
        PointerEventData eventData)
    {
        isHovering =
            false;


        isPressed =
            false;
    }


    public void OnPointerDown(
        PointerEventData eventData)
    {
        isPressed =
            true;
    }


    public void OnPointerUp(
        PointerEventData eventData)
    {
        isPressed =
            false;
    }


    // ==================================================
    // Reset
    // ==================================================

    public void ResetImmediate()
    {
        isHovering =
            false;


        isPressed =
            false;


        if (rectTransform == null)
            return;


        rectTransform.localScale =
            originalScale;


        rectTransform.anchoredPosition =
            originalPosition;
    }


    // 선택 연출 때문에 이 컴포넌트가 꺼졌을 때
    // Hover 상태가 다음 Floor까지 남는 것을 방지
    private void OnDisable()
    {
        ResetImmediate();
    }
}