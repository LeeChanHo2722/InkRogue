using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDive : MonoBehaviour
{
    // ==================================================
    // Dive
    // ==================================================

    [Header("Dive")]

    [Tooltip("발밑 Ink 판정 범위")]
    public float inkSampleRadius = 0.22f;


    // ==================================================
    // Ink Recovery
    // ==================================================

    [Header("Ink Recovery")]

    [Range(0f, 1f)]
    [Tooltip("실제 잠수 중 최대 Ink 기준 초당 회복 비율")]
    public float inkRecoveryPercentPerSecond =
        0.25f;


    // ==================================================
    // Shield Recovery
    // ==================================================

    [Header("Shield Recovery")]

    [Range(0f, 1f)]
    [Tooltip("실제 잠수 중 최대 Shield 기준 초당 회복 비율")]
    public float shieldRecoveryPercentPerSecond =
        0.50f;


    [Tooltip("내 Ink에 잠긴 뒤 Shield 회복까지 필요한 시간")]
    public float shieldRecoveryDelay =
        1f;


    // ==================================================
    // State
    // ==================================================

    [SerializeField]
    private bool isSwimForm =
        false;


    [SerializeField]
    private bool isDiving =
        false;


    private float diveStartedTime;


    // ==================================================
    // References
    // ==================================================

    private PlayerInkResource inkResource;

    private PlayerShield playerShield;

    private SplashBombWeaponBehaviour
        splashBombWeaponBehaviour;


    // ==================================================
    // Public State
    // ==================================================

    public bool IsSwimForm =>
        isSwimForm;


    public bool IsDiving =>
        isDiving;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        inkResource =
            GetComponent<
                PlayerInkResource
            >();


        playerShield =
            GetComponent<
                PlayerShield
            >();


        splashBombWeaponBehaviour =
            GetComponentInChildren<
                SplashBombWeaponBehaviour
            >(
                true
            );
    }


    // ==================================================
    // Update
    // ==================================================

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
        // SplashBomb 차징 중
        //
        // 잠수는 캐릭터 전체 형태 변화이므로
        // 어느 손에서든 Bomb 차지 중이면 진입 불가.
        // ==================================================

        if (splashBombWeaponBehaviour != null &&
            splashBombWeaponBehaviour.IsCharging)
        {
            ExitSwimForm();

            return;
        }


        EnterSwimForm();


        // ==================================================
        // InkMap 없음
        // ==================================================

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
        // ==================================================

        if (currentInk ==
            InkTeam.Enemy)
        {
            ExitSwimForm();

            return;
        }


        // ==================================================
        // Neutral
        // ==================================================

        if (currentInk !=
            InkTeam.Player)
        {
            ExitDive();

            return;
        }


        // ==================================================
        // Player Ink
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
        {
            return;
        }


        float recoverAmount =
            inkResource.MaxInk
            *
            inkRecoveryPercentPerSecond
            *
            Time.deltaTime;


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
        {
            return;
        }


        if (playerShield.IsEmergency)
        {
            return;
        }


        float timeSinceDiveStarted =
            Time.time
            -
            diveStartedTime;


        if (timeSinceDiveStarted <
            shieldRecoveryDelay)
        {
            return;
        }


        if (playerShield.TimeSinceLastHit <
            shieldRecoveryDelay)
        {
            return;
        }


        float recoverAmount =
            playerShield.MaxShield
            *
            shieldRecoveryPercentPerSecond
            *
            Time.deltaTime;


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
    // Dive State
    // ==================================================

    private void EnterDive()
    {
        if (isDiving)
        {
            return;
        }


        isDiving =
            true;


        diveStartedTime =
            Time.time;
    }


    private void ExitDive()
    {
        isDiving =
            false;
    }
}