using System.Collections;
using TMPro;          // TextMeshPro 사용을 위해 추가
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 슬라이더, 토글 사용을 위해 추가

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject metaUpgradePanel;

    [Header("Scene")]
    [SerializeField] private string inGameSceneName = "Main";

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Options UI")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Toggle fullscreenToggle;

    [Header("Value Texts")]
    public TMP_Text masterValueText;
    public TMP_Text bgmValueText;
    public TMP_Text sfxValueText;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel.activeSelf)
            {
                OnSettingsBack();
            }
            else if (metaUpgradePanel.activeSelf)
            {
                OnMetaUpgradeBack();
            }
        }
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Lobby_BGM");
        }

        if (fadeCanvasGroup != null)
        {
            StartCoroutine(Fade(0f));
        }

        // ==========================================
        // 저장된 세팅 값 불러오기 및 UI 연동
        // ==========================================
        if (masterSlider != null)
        {
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
            masterSlider.onValueChanged.AddListener(v => UpdateValueText(masterValueText, v));
            UpdateValueText(masterValueText, masterSlider.value);
            SetMasterVolume(masterSlider.value);
        }

        if (bgmSlider != null)
        {
            bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            bgmSlider.onValueChanged.AddListener(v => UpdateValueText(bgmValueText, v));
            UpdateValueText(bgmValueText, bgmSlider.value);
            SetBGMVolume(bgmSlider.value);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(v => UpdateValueText(sfxValueText, v));
            UpdateValueText(sfxValueText, sfxSlider.value);
            SetSFXVolume(sfxSlider.value);
        }

        if (fullscreenToggle != null)
        {
            bool isFull = PlayerPrefs.GetInt("IsFullscreen", 1) == 1;
            fullscreenToggle.isOn = isFull;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            SetFullscreen(isFull);
        }
    }

    // ==========================================
    // 옵션 제어 및 영구 저장 로직
    // ==========================================
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
            Debug.Log("전체화면 (1920x1080) 적용");
        }
        else
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Debug.Log("창모드 (1280x720) 적용");
        }
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
    }

    private void UpdateValueText(TMP_Text text, float value)
    {
        if (text != null) text.text = Mathf.RoundToInt(value * 100).ToString();
    }

    // ==========================================
    // 씬 이동 및 패널 제어
    // ==========================================
    public void OnStartButton()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(Fade(1f));
        }

        SceneManager.LoadScene(inGameSceneName);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void OnSettingsButton()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnSettingsBack()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnMetaUpgradeButton()
    {
        mainPanel.SetActive(false);
        metaUpgradePanel.SetActive(true);
    }

    public void OnMetaUpgradeBack()
    {
        metaUpgradePanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // 에디터에서는 게임 종료 대신 플레이 모드 끄기
#else
        Application.Quit();
#endif
    }
}