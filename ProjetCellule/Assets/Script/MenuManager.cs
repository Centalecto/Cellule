using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Scene")]
    [SerializeField] private string gameSceneName;

    [Header("Audio")]
    [SerializeField] private AudioSource uiClickSound;

    void Start()
    {
        ShowMainMenu();
    }

    // ---------- BOUTONS ----------

    public void PlayGame()
    {
        
        PlayClick();
        
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("MenuManager : aucune scène de jeu définie !");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        
        PlayClick();
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        
        PlayClick();
        ShowMainMenu();
    }

    public void QuitGame()
    {
        PlayClick();
        Application.Quit();
    }

    // ---------- UTILS ----------

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    private void PlayClick()
    {
        if (uiClickSound != null) 
        {

            uiClickSound.PlayOneShot(uiClickSound.clip);
        }
            
    }
}