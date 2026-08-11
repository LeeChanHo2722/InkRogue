using UnityEngine;

public class PlayerDamageAudio : MonoBehaviour
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
            playerShield.ShieldHit +=
                OnPlayerHit;
        }
    }


    // ==================================================
    // Disable
    // ==================================================

    private void OnDisable()
    {
        if (playerShield != null)
        {
            playerShield.ShieldHit -=
                OnPlayerHit;
        }
    }


    // ==================================================
    // Hit
    // ==================================================

    private void OnPlayerHit()
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayPlayerHit();
        }
    }
}