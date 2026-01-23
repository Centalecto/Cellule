using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private AudioSource uiClickSound;


    public static bool IsPaused { get; private set; }

    void Start()
    {
        IsPaused = false;
        pauseCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // ---------- BOUTONS UI ----------

    public void TogglePause()
    {
        PlayClick();

        IsPaused = !IsPaused;
        pauseCanvas.SetActive(IsPaused);
        Time.timeScale = IsPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        PlayClick();
        IsPaused = false;
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        PlayClick();
        Time.timeScale = 1f;
        Application.Quit();
    }

    // ---------- UTILS ----------

    void PlayClick()
    {
        if (uiClickSound != null)
            uiClickSound.Play();
    }
}