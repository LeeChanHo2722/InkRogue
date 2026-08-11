using UnityEngine;
using UnityEngine.UI;

public class UIButtonSelectSFX : MonoBehaviour
{
    private Button button;


    private void Awake()
    {
        button =
            GetComponent<Button>();


        if (button != null)
        {
            button.onClick.AddListener(
                PlaySelectSound
            );
        }
    }


    private void PlaySelectSound()
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .PlayCardSelect();
        }
    }


    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                PlaySelectSound
            );
        }
    }
}