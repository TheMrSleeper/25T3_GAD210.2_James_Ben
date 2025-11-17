using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelHowTo;

    [Header("Buttons")]
    [SerializeField] private GameObject btnPlayDefault; // First selected
    [SerializeField] private GameObject btnHowToCloseDefault; // First selected when HowTo opens

    private void Start()
    {
        // Ensure menu starts in a predictable state
        if (panelHowTo != null) panelHowTo.SetActive(false);

        // Set default selected for keyboard/controller
        if (btnPlayDefault != null)
            EventSystem.current?.SetSelectedGameObject(btnPlayDefault);
    }

    // Called by Play button
    public void OnPlay()
    {
        // For now, go to a placeholder scene we’ll create next (e.g., "Lobby")
        // Change "Lobby" to your actual next scene name.
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    // Called by How To Play button
    public void OnHowToOpen()
    {
        if (panelHowTo == null) return;
        panelHowTo.SetActive(true);

        // Move selection to the Close button for accessibility
        if (btnHowToCloseDefault != null)
            EventSystem.current?.SetSelectedGameObject(btnHowToCloseDefault);
    }

    // Called by Close button on How To panel
    public void OnHowToClose()
    {
        if (panelHowTo == null) return;
        panelHowTo.SetActive(false);

        // Return selection to Play
        if (btnPlayDefault != null)
            EventSystem.current?.SetSelectedGameObject(btnPlayDefault);
    }

    // Called by Quit button
    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
