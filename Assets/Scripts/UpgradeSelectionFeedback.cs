using System.Collections;
using UnityEngine;

public class UpgradeSelectionFeedback : MonoBehaviour
{
    // ==================================================
    // Cards
    // ==================================================

    [Header("Cards")]

    public RectTransform powerCard;

    public RectTransform rapidCard;

    public RectTransform speedCard;


    // ==================================================
    // Selected Card
    // ==================================================

    [Header("Selected Card")]

    [Tooltip("선택한 카드 확대 크기")]
    public float selectedScale = 1.14f;

    [Tooltip("선택한 카드가 위로 올라가는 거리")]
    public float selectedLift = 12f;


    // ==================================================
    // Other Cards
    // ==================================================

    [Header("Other Cards")]

    [Tooltip("선택되지 않은 카드 크기")]
    public float unselectedScale = 0.94f;

    [Range(0f, 1f)]
    [Tooltip("선택되지 않은 카드 투명도")]
    public float unselectedAlpha = 0.28f;


    // ==================================================
    // Timing
    // ==================================================

    [Header("Timing")]

    [Tooltip("선택 연출이 진행되는 시간")]
    public float focusDuration = 0.18f;

    [Tooltip("선택 결과를 보여주는 시간")]
    public float holdDuration = 0.22f;


    // ==================================================
    // Runtime
    // ==================================================

    private CardState powerState;

    private CardState rapidState;

    private CardState speedState;

    private bool initialized = false;


    // ==================================================
    // Card State
    // ==================================================

    private class CardState
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


    private CardState CreateState(
        RectTransform card)
    {
        if (card == null)
            return null;


        CardState state =
            new CardState();


        state.rectTransform =
            card;


        state.originalScale =
            card.localScale;


        state.originalPosition =
            card.anchoredPosition;


        CanvasGroup canvasGroup =
            card.GetComponent<CanvasGroup>();


        // 카드에 CanvasGroup이 없어도
        // 자동으로 추가
        if (canvasGroup == null)
        {
            canvasGroup =
                card.gameObject
                    .AddComponent<CanvasGroup>();
        }


        state.canvasGroup =
            canvasGroup;


        state.hover =
            card.GetComponent<UpgradeCardHover>();


        return state;
    }


    // ==================================================
    // Reset
    // ==================================================

    public void ResetCardsImmediate()
    {
        Initialize();


        ResetState(
            powerState
        );


        ResetState(
            rapidState
        );


        ResetState(
            speedState
        );
    }


    private void ResetState(
        CardState state)
    {
        if (state == null)
            return;


        state.rectTransform.localScale =
            state.originalScale;


        state.rectTransform.anchoredPosition =
            state.originalPosition;


        if (state.canvasGroup != null)
        {
            state.canvasGroup.alpha =
                1f;


            state.canvasGroup.interactable =
                true;


            state.canvasGroup.blocksRaycasts =
                true;
        }


        if (state.hover != null)
        {
            state.hover.enabled =
                true;


            state.hover.ResetImmediate();
        }
    }


    // ==================================================
    // Play Selection
    // ==================================================

    public IEnumerator PlaySelection(
        UpgradeCardUI.UpgradeType selectedType)
    {
        Initialize();


        CardState selectedState =
            GetState(
                selectedType
            );


        if (selectedState == null)
            yield break;


        // ==========================================
        // 선택 이후에는 추가 클릭 방지
        // ==========================================

        LockCardsForAnimation();


        // ==========================================
        // Focus Animation
        // ==========================================

        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                focusDuration,
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


            AnimateState(
                powerState,
                powerState == selectedState,
                eased
            );


            AnimateState(
                rapidState,
                rapidState == selectedState,
                eased
            );


            AnimateState(
                speedState,
                speedState == selectedState,
                eased
            );


            yield return null;
        }


        // ==========================================
        // 최종 상태 확정
        // ==========================================

        AnimateState(
            powerState,
            powerState == selectedState,
            1f
        );


        AnimateState(
            rapidState,
            rapidState == selectedState,
            1f
        );


        AnimateState(
            speedState,
            speedState == selectedState,
            1f
        );


        // ==========================================
        // 선택된 카드를 잠깐 감상
        // ==========================================

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                holdDuration
            );
        }
    }


    // ==================================================
    // Animate State
    // ==================================================

    private void AnimateState(
        CardState state,
        bool selected,
        float t)
    {
        if (state == null)
            return;


        if (selected)
        {
            // ======================================
            // 선택 카드
            // ======================================

            Vector3 targetScale =
                state.originalScale
                * selectedScale;


            Vector2 targetPosition =
                state.originalPosition
                + Vector2.up
                * selectedLift;


            state.rectTransform.localScale =
                Vector3.Lerp(
                    state.originalScale,
                    targetScale,
                    t
                );


            state.rectTransform.anchoredPosition =
                Vector2.Lerp(
                    state.originalPosition,
                    targetPosition,
                    t
                );


            if (state.canvasGroup != null)
            {
                state.canvasGroup.alpha =
                    1f;
            }
        }
        else
        {
            // ======================================
            // 선택 안 된 카드
            // ======================================

            Vector3 targetScale =
                state.originalScale
                * unselectedScale;


            state.rectTransform.localScale =
                Vector3.Lerp(
                    state.originalScale,
                    targetScale,
                    t
                );


            state.rectTransform.anchoredPosition =
                state.originalPosition;


            if (state.canvasGroup != null)
            {
                state.canvasGroup.alpha =
                    Mathf.Lerp(
                        1f,
                        unselectedAlpha,
                        t
                    );
            }
        }
    }


    // ==================================================
    // Lock
    // ==================================================

    private void LockCardsForAnimation()
    {
        LockState(
            powerState
        );


        LockState(
            rapidState
        );


        LockState(
            speedState
        );
    }


    private void LockState(
        CardState state)
    {
        if (state == null)
            return;


        // Hover가 선택 연출의 Scale을
        // 덮어쓰지 못하도록 중지
        if (state.hover != null)
        {
            state.hover.enabled =
                false;
        }


        if (state.canvasGroup != null)
        {
            state.canvasGroup.interactable =
                false;


            state.canvasGroup.blocksRaycasts =
                false;
        }
    }


    // ==================================================
    // Get State
    // ==================================================

    private CardState GetState(
        UpgradeCardUI.UpgradeType type)
    {
        switch (type)
        {
            case UpgradeCardUI.UpgradeType.Power:
                return powerState;

            case UpgradeCardUI.UpgradeType.Rapid:
                return rapidState;

            case UpgradeCardUI.UpgradeType.Speed:
                return speedState;
        }


        return null;
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
}