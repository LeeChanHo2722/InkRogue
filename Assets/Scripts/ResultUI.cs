using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public CanvasGroup rootCanvasGroup;

    public CanvasGroup contentCanvasGroup;

    public RectTransform contentRoot;


    // ==================================================
    // Texts
    // ==================================================

    [Header("Texts")]

    public TMP_Text titleText;

    public TMP_Text subText;

    public TMP_Text clearTimeText;

    public TMP_Text powerText;

    public TMP_Text rapidText;

    public TMP_Text speedText;


    // ==================================================
    // Button
    // ==================================================

    [Header("Button")]

    public Button restartButton;


    // ==================================================
    // Upgrade
    // ==================================================

    [Header("Upgrade")]

    public UpgradeManager upgradeManager;


    // ==================================================
    // Animation
    // ==================================================

    [Header("Open Animation")]

    public float appearDuration = 0.35f;

    public float startScale = 1.12f;

    public float settleScale = 1f;


    // ==================================================
    // Runtime
    // ==================================================

    private double runStartRealtime;

    private Vector3 originalContentScale;

    private bool resultPrepared = false;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        runStartRealtime =
            Time.realtimeSinceStartupAsDouble;


        // ==========================================
        // Auto References
        // ==========================================

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup =
                GetComponent<CanvasGroup>();
        }


        if (contentRoot != null &&
            contentCanvasGroup == null)
        {
            contentCanvasGroup =
                contentRoot
                    .GetComponent<CanvasGroup>();
        }


        if (contentRoot != null)
        {
            originalContentScale =
                contentRoot.localScale;
        }


        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(
                RestartGame
            );


            restartButton.onClick.AddListener(
                RestartGame
            );
        }


        HideImmediate();
    }


    // ==================================================
    // Prepare
    // ==================================================

    public void PrepareResult()
    {
        Debug.Log(
            "RESULT UI → PREPARE START"
        );


        resultPrepared =
            true;


        // ==========================================
        // 혹시 ResultPanel이 꺼져 있었다면
        // 강제로 활성화
        // ==========================================

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(
                true
            );
        }


        // ==========================================
        // 부모도 비활성 상태인지 검사
        // ==========================================

        Transform current =
            transform.parent;


        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(
                    true
                );
            }


            current =
                current.parent;
        }


        // ==========================================
        // CanvasGroup 확보
        // ==========================================

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup =
                GetComponent<CanvasGroup>();
        }


        if (contentRoot != null &&
            contentCanvasGroup == null)
        {
            contentCanvasGroup =
                contentRoot
                    .GetComponent<CanvasGroup>();
        }


        // ==========================================
        // Text 갱신
        // ==========================================

        UpdateTexts();


        // ==========================================
        // Root 표시
        // ==========================================

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha =
                1f;


            rootCanvasGroup.interactable =
                true;


            rootCanvasGroup.blocksRaycasts =
                true;
        }


        // ==========================================
        // Content는 Animation 전까지 숨김
        // ==========================================

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha =
                0f;


            contentCanvasGroup.interactable =
                false;


            contentCanvasGroup.blocksRaycasts =
                false;
        }


        if (contentRoot != null)
        {
            contentRoot.localScale =
                originalContentScale
                * startScale;
        }


        // ==========================================
        // Result 화면은 Gameplay 정지
        // ==========================================

        Time.timeScale =
            0f;


        Debug.Log(
            "RESULT UI → PREPARE COMPLETE"
            + " | Root Alpha: "
            + (
                rootCanvasGroup != null
                    ? rootCanvasGroup.alpha
                    : -1f
            )
        );
    }


    // ==================================================
    // Open Animation
    // ==================================================

    public IEnumerator PlayOpenAnimation()
    {
        Debug.Log(
            "RESULT UI → OPEN ANIMATION START"
        );


        if (!resultPrepared)
        {
            PrepareResult();
        }


        // ==========================================
        // Content Root가 없어도
        // ResultPanel 자체는 표시
        // ==========================================

        if (contentCanvasGroup == null ||
            contentRoot == null)
        {
            Debug.LogWarning(
                "ResultUI: ContentRoot 또는 "
                + "Content CanvasGroup이 없습니다."
            );


            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha =
                    1f;
            }


            yield break;
        }


        float timer =
            0f;


        float safeDuration =
            Mathf.Max(
                appearDuration,
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


            contentCanvasGroup.alpha =
                eased;


            float scale =
                Mathf.Lerp(
                    startScale,
                    settleScale,
                    eased
                );


            contentRoot.localScale =
                originalContentScale
                * scale;


            yield return null;
        }


        contentCanvasGroup.alpha =
            1f;


        contentCanvasGroup.interactable =
            true;


        contentCanvasGroup.blocksRaycasts =
            true;


        contentRoot.localScale =
            originalContentScale
            * settleScale;


        Debug.Log(
            "RESULT UI → OPEN ANIMATION COMPLETE"
        );
    }


    // ==================================================
    // Texts
    // ==================================================

    private void UpdateTexts()
    {
        if (titleText != null)
        {
            titleText.text =
                "MISSION COMPLETE";
        }


        if (subText != null)
        {
            subText.text =
                "INK CORE DEFEATED";
        }


        double elapsed =
            Time.realtimeSinceStartupAsDouble
            - runStartRealtime;


        if (clearTimeText != null)
        {
            clearTimeText.text =
                FormatTime(
                    elapsed
                );
        }


        int powerLevel =
            GetUpgradeLevel(
                "PowerLevel",
                "powerLevel"
            );


        int rapidLevel =
            GetUpgradeLevel(
                "RapidLevel",
                "rapidLevel"
            );


        int speedLevel =
            GetUpgradeLevel(
                "SpeedLevel",
                "speedLevel"
            );


        if (powerText != null)
        {
            powerText.text =
                "POWER   LV."
                + powerLevel;
        }


        if (rapidText != null)
        {
            rapidText.text =
                "RAPID   LV."
                + rapidLevel;
        }


        if (speedText != null)
        {
            speedText.text =
                "SPEED   LV."
                + speedLevel;
        }
    }


    // ==================================================
    // Upgrade Level
    // ==================================================

    private int GetUpgradeLevel(
        params string[] possibleNames)
    {
        if (upgradeManager == null)
            return 0;


        System.Type type =
            upgradeManager.GetType();


        const BindingFlags flags =
            BindingFlags.Instance
            |
            BindingFlags.Public
            |
            BindingFlags.NonPublic;


        foreach (
            string memberName
            in possibleNames)
        {
            PropertyInfo property =
                type.GetProperty(
                    memberName,
                    flags
                );


            if (property != null &&
                property.PropertyType ==
                typeof(int))
            {
                object value =
                    property.GetValue(
                        upgradeManager
                    );


                if (value is int result)
                {
                    return result;
                }
            }


            FieldInfo field =
                type.GetField(
                    memberName,
                    flags
                );


            if (field != null &&
                field.FieldType ==
                typeof(int))
            {
                object value =
                    field.GetValue(
                        upgradeManager
                    );


                if (value is int result)
                {
                    return result;
                }
            }
        }


        return 0;
    }


    // ==================================================
    // Format Time
    // ==================================================

    private string FormatTime(
        double seconds)
    {
        int minutes =
            Mathf.FloorToInt(
                (float)seconds
                / 60f
            );


        int wholeSeconds =
            Mathf.FloorToInt(
                (float)seconds
            )
            % 60;


        int centiseconds =
            Mathf.FloorToInt(
                (float)(
                    seconds
                    * 100.0
                )
            )
            % 100;


        return
            minutes.ToString("00")
            + ":"
            + wholeSeconds.ToString("00")
            + "."
            + centiseconds.ToString("00");
    }


    // ==================================================
    // Restart
    // ==================================================

    private void RestartGame()
    {
        Time.timeScale =
            1f;


        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }


    // ==================================================
    // Hide
    // ==================================================

    public void HideImmediate()
    {
        resultPrepared =
            false;


        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha =
                0f;


            rootCanvasGroup.interactable =
                false;


            rootCanvasGroup.blocksRaycasts =
                false;
        }


        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha =
                0f;


            contentCanvasGroup.interactable =
                false;


            contentCanvasGroup.blocksRaycasts =
                false;
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
}