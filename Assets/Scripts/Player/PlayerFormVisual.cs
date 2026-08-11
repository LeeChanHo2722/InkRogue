using UnityEngine;

public class PlayerFormVisual : MonoBehaviour
{
    [Header("References")]
    public PlayerDive playerDive;

    public GameObject humanVisual;

    public GameObject swimVisual;

    public SpriteRenderer gunRenderer;


    private bool lastSwimFormState;


    private void Awake()
    {
        if (playerDive == null)
        {
            playerDive =
                GetComponent<PlayerDive>();
        }
    }


    private void Start()
    {
        lastSwimFormState =
            playerDive == null
            ? true
            : !playerDive.IsSwimForm;


        UpdateVisualState();
    }


    private void Update()
    {
        if (playerDive == null)
            return;


        if (lastSwimFormState !=
            playerDive.IsSwimForm)
        {
            UpdateVisualState();
        }
    }


    private void UpdateVisualState()
    {
        if (playerDive == null)
            return;


        bool isSwimForm =
            playerDive.IsSwimForm;


        lastSwimFormState =
            isSwimForm;


        if (humanVisual != null)
        {
            humanVisual.SetActive(
                !isSwimForm
            );
        }


        if (swimVisual != null)
        {
            swimVisual.SetActive(
                isSwimForm
            );
        }


        if (gunRenderer != null)
        {
            gunRenderer.enabled =
                !isSwimForm;
        }
    }
}