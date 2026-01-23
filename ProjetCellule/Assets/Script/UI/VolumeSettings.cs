using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VolumeSettings : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private Slider volumeSlider;

    [Header("Son de test")]
    [SerializeField] private AudioSource testAudioSource;

    void Start()
    {
        // Charger volume sauvegardé
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;
        volumeSlider.value = savedVolume;

        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    // Appelé quand on relâche le slider
    public void OnPointerUp(PointerEventData eventData)
    {
        if (testAudioSource != null)
        {
            testAudioSource.Play();
        }
    }
}
