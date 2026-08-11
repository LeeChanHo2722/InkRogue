using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    // ==================================================
    // References
    // ==================================================

    public GameObject bulletPrefab;

    public Transform firePoint;


    // ==================================================
    // Weapon
    // ==================================================

    [Header("Weapon")]

    public float fireRate = 6f;

    public int bulletDamage = 1;


    // ==================================================
    // Ink Cost
    // ==================================================

    [Header("Ink Cost")]

    [Range(0f, 1f)]

    [Tooltip("최대 Ink 기준 초당 소비 비율")]

    public float inkUsePerSecondPercent =
        0.10f;


    // ==================================================
    // Spray
    // ==================================================

    [Header("Spray")]

    public float spreadIncreasePerShot =
        2.5f;

    public float maxSpreadAngle =
        8f;


    // ==================================================
    // State
    // ==================================================

    public bool IsFiring
    {
        get;
        private set;
    }


    private float nextFireTime;

    private int continuousShotCount =
        0;


    // ==================================================
    // Player References
    // ==================================================

    private PlayerInkResource inkResource;

    private PlayerDive playerDive;

    private PlayerSubWeapon subWeapon;

    private PlayerShotInkStart shotInkStart;


    // ==================================================
    // Awake
    // ==================================================

    private void Awake()
    {
        shotInkStart =
            GetComponentInParent<
                PlayerShotInkStart
            >();


        inkResource =
            GetComponentInParent<
                PlayerInkResource
            >();


        playerDive =
            GetComponentInParent<
                PlayerDive
            >();


        subWeapon =
            GetComponent<
                PlayerSubWeapon
            >();
    }


    // ==================================================
    // Update
    // ==================================================

    private void Update()
    {
        if (Mouse.current == null)
        {
            StopFiring();

            return;
        }


        // ==========================================
        // 잠수 중에는 총 발사 불가
        // ==========================================

        if (playerDive != null &&
            playerDive.IsSwimForm)
        {
            StopFiring();

            return;
        }


        // ==========================================
        // Bomb 차징 중에도 총 발사 불가
        // ==========================================

        if (subWeapon != null &&
            subWeapon.IsCharging)
        {
            StopFiring();

            return;
        }


        bool wantsToFire =
            Mouse.current
                .leftButton
                .isPressed;


        if (!wantsToFire)
        {
            StopFiring();

            return;
        }


        if (inkResource == null)
        {
            StopFiring();

            return;
        }


        if (inkResource.IsEmpty)
        {
            StopFiring();

            return;
        }


        // ==========================================
        // Ink 초당 10% 소비
        // ==========================================

        float inkCostThisFrame =
            inkResource.MaxInk
            * inkUsePerSecondPercent
            * Time.deltaTime;


        float actualSpent =
            inkResource.SpendInk(
                inkCostThisFrame
            );


        if (actualSpent <= 0f)
        {
            StopFiring();

            return;
        }


        IsFiring =
            true;


        // ==========================================
        // Bullet 발사
        // ==========================================

        if (Time.time >=
            nextFireTime)
        {
            Shoot();


            nextFireTime =
                Time.time
                + 1f / fireRate;
        }
    }


    // ==================================================
    // Stop Firing
    // ==================================================

    private void StopFiring()
    {
        IsFiring =
            false;


        continuousShotCount =
            0;
    }


    // ==================================================
    // Shoot
    // ==================================================

    private void Shoot()
    {
        float spreadAngle =
            0f;


        // ==========================================
        // 첫 탄은 정확
        // ==========================================

        if (continuousShotCount > 0)
        {
            float currentMaxSpread =
                Mathf.Min(
                    continuousShotCount
                    * spreadIncreasePerShot,
                    maxSpreadAngle
                );


            spreadAngle =
                Random.Range(
                    -currentMaxSpread,
                    currentMaxSpread
                );
        }


        Quaternion bulletRotation =
            firePoint.rotation
            * Quaternion.Euler(
                0f,
                0f,
                spreadAngle
            );


        // ==========================================
        // Bullet 생성
        // ==========================================

        GameObject bulletObject =
            Instantiate(
                bulletPrefab,
                firePoint.position,
                bulletRotation
            );


        // ==========================================
        // Shoot SFX
        //
        // 실제 Bullet이 생성된 순간에만 재생
        // ==========================================

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayShoot();
        }


        // ==========================================
        // Player 발밑 → 총구까지 Ink
        // ==========================================

        shotInkStart?.PaintShotStart(
            firePoint.position
        );


        // ==========================================
        // Damage
        // ==========================================

        Bullet bullet =
            bulletObject
                .GetComponent<
                    Bullet
                >();


        if (bullet != null)
        {
            bullet.damage =
                bulletDamage;
        }


        continuousShotCount++;
    }
}