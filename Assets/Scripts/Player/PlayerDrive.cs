using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDive : MonoBehaviour
{
    [Header("Dive")]
    [Tooltip("발밑 Ink 판정 범위")]
    public float inkSampleRadius = 0.22f;


    [Header("Ink Recovery")]
    [Range(0f, 1f)]
    [Tooltip("실제 잠수 중 최대 Ink 기준 초당 회복 비율")]
    public float inkRecoveryPercentPerSecond = 0.25f;


    [Header("Shield Recovery")]
    [Range(0f, 1f)]
    [Tooltip("실제 잠수 중 최대 Shield 기준 초당 회복 비율")]
    public float shieldRecoveryPercentPerSecond = 0.50f;

    [Tooltip("내 Ink에 잠긴 뒤 Shield 회복까지 필요한 시간")]
    public float shieldRecoveryDelay = 1f;


    // Shift로 변신한 상태
    [SerializeField]
    private bool isSwimForm = false;


    // 실제 Player Ink 내부에 잠긴 상태
    [SerializeField]
    private bool isDiving = false;


    private float diveStartedTime;


    // ==================================================
    // Public State
    // ==================================================

    public bool IsSwimForm
    {
        get
        {
            return isSwimForm;
        }
    }


    public bool IsDiving
    {
        get
        {
            return isDiving;
        }
    }


    private PlayerInkResource inkResource;
    private PlayerShield playerShield;
    private PlayerSubWeapon subWeapon;


    private void Awake()
    {
        inkResource =
            GetComponent<PlayerInkResource>();


        playerShield =
            GetComponent<PlayerShield>();


        subWeapon =
            GetComponentInChildren<PlayerSubWeapon>();
    }


    private void Update()
    {
        if (Keyboard.current == null)
        {
            ExitSwimForm();
            return;
        }


        bool wantsSwimForm =
            Keyboard.current
                .shiftKey
                .isPressed;


        // ==================================================
        // Shift 해제
        // ==================================================

        if (!wantsSwimForm)
        {
            ExitSwimForm();
            return;
        }


        // ==================================================
        // Bomb 차징 중에는 폼 전환 불가
        // ==================================================

        if (subWeapon != null &&
            subWeapon.IsCharging)
        {
            ExitSwimForm();
            return;
        }


        // 일단 Shift가 눌렸으므로
        // Swim Form 진입
        EnterSwimForm();


        // InkMap을 읽을 수 없다면
        // 외형만 Swim Form 유지
        if (InkMap.Instance == null)
        {
            ExitDive();
            return;
        }


        InkTeam currentInk =
            InkMap.Instance
                .GetDominantInkTeam(
                    transform.position,
                    inkSampleRadius
                );


        // ==================================================
        // Enemy Ink
        //
        // 기존 규칙:
        // 상대 Ink에서는 강제 인간 폼
        // ==================================================

        if (currentInk == InkTeam.Enemy)
        {
            ExitSwimForm();
            return;
        }


        // ==================================================
        // Neutral
        //
        // Swim Form은 유지하지만
        // 실제 잠수 보너스는 없음
        // ==================================================

        if (currentInk != InkTeam.Player)
        {
            ExitDive();
            return;
        }


        // ==================================================
        // Player Ink
        //
        // 진짜 잠수 상태
        // ==================================================

        EnterDive();


        RecoverInk();

        RecoverShield();
    }


    // ==================================================
    // Ink Recovery
    // ==================================================

    private void RecoverInk()
    {
        if (inkResource == null)
            return;


        float recoverAmount =
            inkResource.MaxInk
            * inkRecoveryPercentPerSecond
            * Time.deltaTime;


        inkResource.RecoverInk(
            recoverAmount
        );
    }


    // ==================================================
    // Shield Recovery
    // ==================================================

    private void RecoverShield()
    {
        if (playerShield == null)
            return;


        // Emergency에서는 회복 불가
        if (playerShield.IsEmergency)
            return;


        // ------------------------------------------
        // 실제 Player Ink에 들어온 뒤
        // 1초가 지나야 함
        // ------------------------------------------

        float timeSinceDiveStarted =
            Time.time
            - diveStartedTime;


        if (timeSinceDiveStarted <
            shieldRecoveryDelay)
        {
            return;
        }


        // ------------------------------------------
        // 마지막 Hit 이후에도
        // 1초가 지나야 함
        // ------------------------------------------

        if (playerShield.TimeSinceLastHit <
            shieldRecoveryDelay)
        {
            return;
        }


        float recoverAmount =
            playerShield.MaxShield
            * shieldRecoveryPercentPerSecond
            * Time.deltaTime;


        playerShield.RecoverShield(
            recoverAmount
        );
    }


    // ==================================================
    // Form State
    // ==================================================

    private void EnterSwimForm()
    {
        isSwimForm =
            true;
    }


    private void ExitSwimForm()
    {
        isSwimForm =
            false;


        ExitDive();
    }


    // ==================================================
    // Actual Dive State
    // ==================================================

    private void EnterDive()
    {
        // 이미 잠수 중이면
        // 시작 시간을 다시 초기화하지 않음
        if (isDiving)
            return;


        isDiving =
            true;


        // 실제로 Player Ink 안으로
        // 진입한 순간
        diveStartedTime =
            Time.time;
    }


    private void ExitDive()
    {
        isDiving =
            false;
    }
}