using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance
    {
        get;
        private set;
    }


    // ==================================================
    // Audio Sources
    // ==================================================

    [Header("Audio Sources")]

    [Tooltip("BGM 전용 AudioSource - 반드시 하나만 사용")]
    public AudioSource bgmSource;

    [Tooltip("효과음 전용 AudioSource")]
    public AudioSource sfxSource;


    // ==================================================
    // BGM
    // ==================================================

    [Header("BGM")]

    public AudioClip gameplayBGM;

    public AudioClip bossBGM;

    public AudioClip endingBGM;


    // ==================================================
    // Player SFX
    // ==================================================

    [Header("Player SFX")]

    public AudioClip shootSFX;

    public AudioClip splashBombSFX;

    public AudioClip emergencySFX;

    public AudioClip playerHitSFX;

    // ==================================================
    // Enemy SFX
    // ==================================================

    [Header("Enemy SFX")]

    public AudioClip enemyDeathSFX;


    // ==================================================
    // Boss SFX
    // ==================================================

    [Header("Boss SFX")]

    public AudioClip bossPhaseSFX;

    public AudioClip bossDeathSFX;


    // ==================================================
    // UI SFX
    // ==================================================

    [Header("UI SFX")]

    public AudioClip cardSelectSFX;


    // ==================================================
    // Volume
    // ==================================================

    [Header("Volume")]

    [Range(0f, 1f)]
    public float bgmVolume = 0.45f;

    [Range(0f, 1f)]
    public float sfxVolume = 0.75f;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        // Manager 중복 방지
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;


        // ==========================================
        // BGM Source
        // ==========================================

        if (bgmSource != null)
        {
            // 혹시 Inspector에서
            // Play On Awake로 재생된 것도 즉시 정지
            bgmSource.Stop();


            bgmSource.clip =
                null;


            bgmSource.loop =
                true;


            bgmSource.playOnAwake =
                false;


            bgmSource.spatialBlend =
                0f;


            bgmSource.volume =
                bgmVolume;
        }


        // ==========================================
        // SFX Source
        // ==========================================

        if (sfxSource != null)
        {
            sfxSource.loop =
                false;


            sfxSource.playOnAwake =
                false;


            sfxSource.spatialBlend =
                0f;


            sfxSource.volume =
                sfxVolume;
        }
    }


    // ==================================================
    // Start
    // ==================================================

    private void Start()
    {
        PlayGameplayBGM();
    }


    // ==================================================
    // BGM
    // ==================================================

    public void PlayGameplayBGM()
    {
        PlayBGM(
            gameplayBGM
        );
    }


    public void PlayBossBGM()
    {
        PlayBGM(
            bossBGM
        );
    }


    public void PlayEndingBGM()
    {
        PlayBGM(
            endingBGM
        );
    }


    private void PlayBGM(
        AudioClip newClip)
    {
        if (bgmSource == null ||
            newClip == null)
        {
            return;
        }


        // 이미 같은 곡이면 재시작하지 않음
        if (bgmSource.clip == newClip &&
            bgmSource.isPlaying)
        {
            return;
        }


        // ==========================================
        // 핵심
        //
        // 기존 BGM을 무조건 완전히 중지
        // ==========================================

        bgmSource.Stop();


        bgmSource.clip =
            null;


        // 새 BGM 장착
        bgmSource.clip =
            newClip;


        bgmSource.loop =
            true;


        bgmSource.volume =
            bgmVolume;


        bgmSource.Play();


        Debug.Log(
            "BGM SWITCH → "
            + newClip.name
        );
    }


    public void StopBGM()
    {
        if (bgmSource == null)
            return;


        bgmSource.Stop();


        bgmSource.clip =
            null;
    }


    // ==================================================
    // General SFX
    // ==================================================

    public void PlaySFX(
        AudioClip clip,
        float volumeMultiplier = 1f)
    {
        if (clip == null ||
            sfxSource == null)
        {
            return;
        }


        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(
                volumeMultiplier
            )
        );
    }

    public void PlayEmergency()
    {
        PlaySFX(
            emergencySFX,
            1f
        );
    }

    // ==================================================
    // Player Hit
    // ==================================================

    public void PlayPlayerHit()
    {
        PlaySFX(
            playerHitSFX,
            0.85f
        );
    }

    // ==================================================
    // Player
    // ==================================================

    public void PlayShoot()
    {
        PlaySFX(
            shootSFX,
            0.45f
        );
    }


    public void PlaySplashBomb()
    {
        PlaySFX(
            splashBombSFX,
            1f
        );
    }


    // ==================================================
    // Enemy
    // ==================================================

    public void PlayEnemyDeath()
    {
        PlaySFX(
            enemyDeathSFX,
            0.65f
        );
    }


    // ==================================================
    // Boss
    // ==================================================

    public void PlayBossPhase()
    {
        PlaySFX(
            bossPhaseSFX,
            1f
        );
    }


    public void PlayBossDeath()
    {
        PlaySFX(
            bossDeathSFX,
            1f
        );
    }


    // ==================================================
    // UI
    // ==================================================

    public void PlayCardSelect()
    {
        PlaySFX(
            cardSelectSFX,
            0.85f
        );
    }
}