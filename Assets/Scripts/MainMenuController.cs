using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelHowTo;

    [Header("Buttons")]
    [SerializeField] private GameObject btnPlayDefault;
    [SerializeField] private GameObject btnHowToCloseDefault;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
            AudioManager.Instance.StopGameAmbience();
        }

        // Ensure menu starts in a predictable state
        if (panelHowTo != null) panelHowTo.SetActive(false);

        // Set default selected for keyboard/controller
        if (btnPlayDefault != null)
            EventSystem.current?.SetSelectedGameObject(btnPlayDefault);
    }

    public void OnPlay()
    {
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    public void OnHowToOpen()
    {
        if (panelHowTo == null) return;
        panelHowTo.SetActive(true);

        if (btnHowToCloseDefault != null)
            EventSystem.current?.SetSelectedGameObject(btnHowToCloseDefault);
    }

    public void OnHowToClose()
    {
        if (panelHowTo == null) return;
        panelHowTo.SetActive(false);

        if (btnPlayDefault != null)
            EventSystem.current?.SetSelectedGameObject(btnPlayDefault);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
