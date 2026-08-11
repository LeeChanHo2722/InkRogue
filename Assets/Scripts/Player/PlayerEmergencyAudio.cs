using UnityEngine;

public class PlayerEmergencyAudio : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    [Header("References")]

    public PlayerShield playerShield;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        if (playerShield == null)
        {
            playerShield =
                GetComponent<PlayerShield>();
        }


        if (playerShield == null)
        {
            playerShield =
                GetComponentInParent<PlayerShield>();
        }
    }


    // ==================================================
    // Enable
    // ==================================================

    private void OnEnable()
    {
        if (playerShield != null)
        {
            playerShield.ShieldBroken +=
                OnShieldBroken;
        }
    }


    // ==================================================
    // Disable
    // ==================================================

    private void OnDisable()
    {
        if (playerShield != null)
        {
            playerShield.ShieldBroken -=
                OnShieldBroken;
        }
    }


    // ==================================================
    // Emergency
    // ==================================================

    private void OnShieldBroken()
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayEmergency();
        }
    }
}