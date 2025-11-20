using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject modeSelectPanel; // for future

    [Header("Settings UI")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public TMP_Dropdown qualityDropdown;

    private void Start()
    {
        // Make sure main menu is visible and others are hidden
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);

        // Setup volume slider
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            volumeSlider.value = AudioListener.volume;
        }

        // Setup quality dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            var names = QualitySettings.names;
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(names));
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        // Sensitivity slider: we’ll just store it for now
        if (sensitivitySlider != null)
        {
            // load from PlayerPrefs or default 1
            float stored = PlayerPrefs.GetFloat("MouseSensitivityMultiplier", 1f);
            sensitivitySlider.value = stored;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
    }

    // Called by the "Settings" button
    public void OnSettingsButton()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // Called by the "Back" button in settings
    public void OnBackFromSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // Called by "Quit" button
    public void OnQuitButton()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- HANDLERS ---

    private void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
    }

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    private void OnSensitivityChanged(float v)
    {
        // Store in PlayerPrefs for now; we can hook this into FpsPlayerController later
        PlayerPrefs.SetFloat("MouseSensitivityMultiplier", v);
    }

    // For later game mode screen:
    public void OnPlayButton()
    {
        // Example: show game mode selection panel instead of host/join
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (modeSelectPanel != null) modeSelectPanel.SetActive(true);
    }

    public void OnBackFromModeSelect()
    {
        if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}
