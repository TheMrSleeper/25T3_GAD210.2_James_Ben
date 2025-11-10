using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject howToModal;

    // Called by Play button
    public void OnPlayClicked()
    {
        // Placeholder
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    // Called by How To Play button
    public void OnHowToClicked()
    {
        if (howToModal != null) howToModal.SetActive(true);
    }

    // Called by Close button inside the modal
    public void OnCloseHowToClicked()
    {
        if (howToModal != null) howToModal.SetActive(false);
    }

    // Called by Quit button
    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ESC closes modal
    private void Update()
    {
        if (howToModal != null && howToModal.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            howToModal.SetActive(false);
        }
    }
}
