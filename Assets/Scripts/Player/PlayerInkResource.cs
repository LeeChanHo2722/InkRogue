using UnityEngine;

public class PlayerInkResource : MonoBehaviour
{
    [Header("Ink")]
    public float maxInk = 100f;

    [SerializeField]
    private float currentInk;

    public float MaxInk
    {
        get
        {
            return maxInk;
        }
    }

    public float CurrentInk
    {
        get
        {
            return currentInk;
        }
    }

    public float CurrentInkPercent
    {
        get
        {
            if (maxInk <= 0f)
                return 0f;

            return currentInk / maxInk;
        }
    }

    public bool IsEmpty
    {
        get
        {
            return currentInk <= 0.001f;
        }
    }

    private void Awake()
    {
        // 게임 시작 시 Ink 100%
        currentInk = maxInk;
    }


    // ==================================================
    // Ink 확인
    // SplashBomb 같은 고정 비용용
    // ==================================================

    public bool HasInk(float amount)
    {
        return currentInk >= amount;
    }


    // ==================================================
    // 고정 Ink 소비
    // 나중에 SplashBomb에서 사용
    // ==================================================

    public bool TrySpendInk(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentInk < amount)
            return false;

        currentInk -= amount;

        currentInk =
            Mathf.Clamp(
                currentInk,
                0f,
                maxInk
            );

        return true;
    }


    // ==================================================
    // 연속 Ink 소비
    // 총처럼 초당 비용이 있는 기능용
    // ==================================================

    public float SpendInk(float amount)
    {
        if (amount <= 0f)
            return 0f;

        float actualSpent =
            Mathf.Min(
                amount,
                currentInk
            );

        currentInk -=
            actualSpent;

        currentInk =
            Mathf.Clamp(
                currentInk,
                0f,
                maxInk
            );

        return actualSpent;
    }


    // ==================================================
    // Ink 회복
    // 다음 단계의 잠수에서 사용
    // ==================================================

    public void RecoverInk(float amount)
    {
        if (amount <= 0f)
            return;

        currentInk += amount;

        currentInk =
            Mathf.Clamp(
                currentInk,
                0f,
                maxInk
            );
    }


    public void FillInk()
    {
        currentInk =
            maxInk;
    }
}