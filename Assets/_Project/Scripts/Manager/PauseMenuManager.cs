using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject optionsPanel;

    [Header("Options UI")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Toggle fullscreenToggle;

    [Header("Value Texts")]
    public TMP_Text masterValueText;
    public TMP_Text bgmValueText;
    public TMP_Text sfxValueText;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    public static bool isPaused = false;
    public static float previousTimeScale = 1f;

    //창우_[추가] 외부에서 일시정지 상태를 확인할 수 있도록 프로퍼티 추가
    public bool IsPaused => isPaused;

    private void Start()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);

        // =========================================================
        // [수정] PlayerPrefs에서 저장된 값을 불러와서 슬라이더/토글 위치를 맞춰줍니다.
        // =========================================================
        if (masterSlider != null)
        {
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
            masterSlider.onValueChanged.AddListener(v => UpdateValueText(masterValueText, v));
            UpdateValueText(masterValueText, masterSlider.value);
        }

        if (bgmSlider != null)
        {
            bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            bgmSlider.onValueChanged.AddListener(v => UpdateValueText(bgmValueText, v));
            UpdateValueText(bgmValueText, bgmSlider.value);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(v => UpdateValueText(sfxValueText, v));
            UpdateValueText(sfxValueText, sfxSlider.value);
        }

        if (fullscreenToggle != null)
        {
            bool isFull = PlayerPrefs.GetInt("IsFullscreen", 1) == 1;
            fullscreenToggle.isOn = isFull;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (optionsPanel.activeSelf)
            {
                CloseOptions();
            }
            else
            {
                TogglePause();
            }
        }
    }

    // =========================================================
    // 일시정지 및 씬 제어
    // =========================================================

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            previousTimeScale = Time.timeScale; 
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = previousTimeScale;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =========================================================
    // 패널 전환
    // =========================================================

    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    // =========================================================
    // 옵션 기능 연동 및 영구 저장 (PlayerPrefs)
    // =========================================================

    public void SetMasterVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
        PlayerPrefs.SetFloat("MasterVolume", value); 
    }

    public void SetBGMVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(value);
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value); 
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen)
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
    }

    private void UpdateValueText(TMP_Text text, float value)
    {
        if (text != null)
            text.text = Mathf.RoundToInt(value * 100).ToString();
    }
}