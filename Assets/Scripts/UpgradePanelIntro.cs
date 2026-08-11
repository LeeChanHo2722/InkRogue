using System.Collections;
using UnityEngine;

public class UpgradePanelIntro : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public RectTransform title;

    public RectTransform powerCard;

    public RectTransform rapidCard;

    public RectTransform speedCard;


    // ==================================================
    // Title
    // ==================================================

    [Header("Title")]

    public float titleDuration = 0.18f;

    public float titleStartScale = 1.18f;


    // ==================================================
    // Cards
    // ==================================================

    [Header("Cards")]

    [Tooltip("카드가 아래에서 올라오는 거리")]
    public float cardStartYOffset = 45f;

    [Tooltip("카드 등장 시작 크기")]
    public float cardStartScale = 0.82f;

    [Tooltip("각 카드의 등장 시간")]
    public float cardDuration = 0.28f;

    [Tooltip("POWER → RAPID → SPEED 사이 시간차")]
    public float cardStagger = 0.09f;


    // ==================================================
    // Runtime
    // ==================================================

    private UIState titleState;

    private UIState powerState;

    private UIState rapidState;

    private UIState speedState;

    private bool initialized = false;


    // ==================================================
    // State
    // ==================================================

    private class UIState
    {
        public RectTransform rectTransform;

        public CanvasGroup canvasGroup;

        public UpgradeCardHover hover;

        public Vector3 originalScale;

        public Vector2 originalPosition;
    }


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        Initialize();
    }


    // ==================================================
    // Initialize
    // ==================================================

    private void Initialize()
    {
        if (initialized)
            return;


        titleState =
            CreateState(
                title
            );


        powerState =
            CreateState(
                powerCard
            );


        rapidState =
            CreateState(
                rapidCard
            );


        speedState =
            CreateState(
                speedCard
            );


        initialized =
            true;
    }


    private UIState CreateState(
        RectTransform target)
    {
        if (target == null)
            return null;


        UIState state =
            new UIState();


        state.rectTransform =
            target;


        state.originalScale =
            target.localScale;


        state.originalPosition =
            target.anchoredPosition;


        CanvasGroup group =
            target.GetComponent<CanvasGroup>();


        if (group == null)
        {
            group =
                target.gameObject
                    .AddComponent<CanvasGroup>();
        }


        state.canvasGroup =
            group;


        state.hover =
            target.GetComponent<UpgradeCardHover>();


        return state;
    }


    // ==================================================
    // Prepare
    // ==================================================

    public void PrepareHidden()
    {
        Initialize();


        // ==========================================
        // Title
        // ==========================================

        if (titleState != null)
        {
            titleState.canvasGroup.alpha =
                0f;


            titleState.rectTransform.localScale =
                titleState.originalScale
                * titleStartScale;
        }


        // ==========================================
        // Cards
        // ==========================================

        PrepareCard(
            powerState
        );


        PrepareCard(
            rapidState
        );


        PrepareCard(
            speedState
        );
    }


    private void PrepareCard(
        UIState state)
    {
        if (state == null)
            return;


        state.canvasGroup.alpha =
            0f;


        state.canvasGroup.interactable =
            false;


        state.canvasGroup.blocksRaycasts =
            false;


        state.rectTransform.localScale =
            state.originalScale
            * cardStartScale;


        state.rectTransform.anchoredPosition =
            state.originalPosition
            + Vector2.down
            * cardStartYOffset;


        // 등장 연출 중 Hover가
        // 위치/크기를 덮어쓰지 못하게 함
        if (state.hover != null)
        {
            state.hover.enabled =
                false;
        }
    }


    // ==================================================
    // Play
    // ==================================================

    public IEnumerator PlayIntro()
    {
        Initialize();


        // ==========================================
        // Header 등장
        // ==========================================

        yield return StartCoroutine(
            AnimateTitle()
        );


        // ==========================================
        // Cards 순차 등장
        // ==========================================

        yield return StartCoroutine(
            AnimateCards()
        );


        // ==========================================
        // 카드 조작 활성화
        // ==========================================

        EnableCard(
            powerState
        );


        EnableCard(
            rapidState
        );


        EnableCard(
            speedState
        );
    }


    // ==================================================
    // Title Animation
    // ==================================================

    private IEnumerator AnimateTitle()
    {
        if (titleState == null)
            yield break;


        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                titleDuration,
                0.01f
            );


        while (timer <
               safeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer
                    / safeDuration
                );


            float eased =
                EaseOutCubic(
                    t
                );


            titleState.canvasGroup.alpha =
                eased;


            float scale =
                Mathf.Lerp(
                    titleStartScale,
                    1f,
                    eased
                );


            titleState.rectTransform.localScale =
                titleState.originalScale
                * scale;


            yield return null;
        }


        titleState.canvasGroup.alpha =
            1f;


        titleState.rectTransform.localScale =
            titleState.originalScale;
    }


    // ==================================================
    // Cards Animation
    // ==================================================

    private IEnumerator AnimateCards()
    {
        float totalDuration =
            cardDuration
            + cardStagger * 2f;


        float timer =
            0f;


        while (timer <
               totalDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            AnimateCard(
                powerState,
                timer,
                0f
            );


            AnimateCard(
                rapidState,
                timer,
                cardStagger
            );


            AnimateCard(
                speedState,
                timer,
                cardStagger * 2f
            );


            yield return null;
        }


        SetCardFinal(
            powerState
        );


        SetCardFinal(
            rapidState
        );


        SetCardFinal(
            speedState
        );
    }


    private void AnimateCard(
        UIState state,
        float globalTime,
        float delay)
    {
        if (state == null)
            return;


        float localTime =
            globalTime - delay;


        if (localTime < 0f)
            return;


        float safeDuration =
            Mathf.Max(
                cardDuration,
                0.01f
            );


        float t =
            Mathf.Clamp01(
                localTime
                / safeDuration
            );


        float eased =
            EaseOutBack(
                t
            );


        // ==========================================
        // Alpha
        // ==========================================

        state.canvasGroup.alpha =
            Mathf.Clamp01(
                t * 1.5f
            );


        // ==========================================
        // Position
        // ==========================================

        Vector2 startPosition =
            state.originalPosition
            + Vector2.down
            * cardStartYOffset;


        state.rectTransform.anchoredPosition =
            Vector2.LerpUnclamped(
                startPosition,
                state.originalPosition,
                eased
            );


        // ==========================================
        // Scale
        // ==========================================

        Vector3 startScale =
            state.originalScale
            * cardStartScale;


        state.rectTransform.localScale =
            Vector3.LerpUnclamped(
                startScale,
                state.originalScale,
                eased
            );
    }


    // ==================================================
    // Final State
    // ==================================================

    private void SetCardFinal(
        UIState state)
    {
        if (state == null)
            return;


        state.canvasGroup.alpha =
            1f;


        state.rectTransform.localScale =
            state.originalScale;


        state.rectTransform.anchoredPosition =
            state.originalPosition;
    }


    private void EnableCard(
        UIState state)
    {
        if (state == null)
            return;


        state.canvasGroup.alpha =
            1f;


        state.canvasGroup.interactable =
            true;


        state.canvasGroup.blocksRaycasts =
            true;


        state.rectTransform.localScale =
            state.originalScale;


        state.rectTransform.anchoredPosition =
            state.originalPosition;


        if (state.hover != null)
        {
            state.hover.enabled =
                true;


            state.hover
                .ResetImmediate();
        }
    }


    // ==================================================
    // Ease
    // ==================================================

    private float EaseOutCubic(
        float t)
    {
        float inverse =
            1f - t;


        return
            1f
            - inverse
            * inverse
            * inverse;
    }


    private float EaseOutBack(
        float t)
    {
        const float c1 =
            1.70158f;


        const float c3 =
            c1 + 1f;


        float x =
            t - 1f;


        return
            1f
            + c3
            * x
            * x
            * x
            + c1
            * x
            * x;
    }
}