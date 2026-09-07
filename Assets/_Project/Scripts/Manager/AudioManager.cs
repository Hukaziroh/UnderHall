using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer Settings")]
    [Tooltip("프로젝트에서 생성한 Audio Mixer를 넣어주세요")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    [Header("Audio Clips")]
    public Sound[] bgmList;
    public Sound[] sfxList;

    [Header("SFX Pool Settings")]
    [Tooltip("동시에 재생될 수 있는 최대 효과음 개수")]
    public int sfxSourceCount = 10;

    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    private AudioSource bgmSource;
    private AudioSource[] sfxSources;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudio();
    }

    private void InitializeAudio()
    {
        foreach (Sound bgm in bgmList)
        {
            bgmDictionary.Add(bgm.name, bgm.clip);
        }
        foreach (Sound sfx in sfxList)
        {
            sfxDictionary.Add(sfx.name, sfx.clip);
        }

        GameObject bgmObject = new GameObject("BGM_Source");
        bgmObject.transform.SetParent(transform);
        bgmSource = bgmObject.AddComponent<AudioSource>();
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        GameObject sfxRoot = new GameObject("SFX_Sources");
        sfxRoot.transform.SetParent(transform);
        sfxSources = new AudioSource[sfxSourceCount];
        for (int i = 0; i < sfxSourceCount; i++)
        {
            sfxSources[i] = sfxRoot.AddComponent<AudioSource>();
            sfxSources[i].outputAudioMixerGroup = sfxMixerGroup;
            sfxSources[i].playOnAwake = false;
        }
    }

    // =========================================================
    // 재생 관련 함수
    // =========================================================

    public void PlayBGM(string name)
    {
        if (bgmDictionary.TryGetValue(name, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying) return; 

            bgmSource.clip = clip;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] BGM을 찾을 수 없습니다: {name}");
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlaySFX(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioClip clip))
        {
            foreach (AudioSource source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    source.clip = clip;
                    source.Play();
                    return;
                }
            }
            sfxSources[0].PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX를 찾을 수 없습니다: {name}");
        }
    }

    public void PlaySFXLoop(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioClip clip))
        {
            // 현재 이미 같은 루프 소리가 나고 있다면 중복 재생 방지
            foreach (AudioSource source in sfxSources)
            {
                if (source.isPlaying && source.clip == clip) return;
            }

            // 비어있는 소스를 찾아 무한 루프로 재생
            foreach (AudioSource source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    source.clip = clip;
                    source.loop = true; // 루프 ON
                    source.Play();
                    return;
                }
            }
        }
    }

    public void StopSFXLoop(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioClip clip))
        {
            // 해당 클립을 재생 중인 소스를 찾아 정지시키고 루프를 끕니다.
            foreach (AudioSource source in sfxSources)
            {
                if (source.clip == clip)
                {
                    source.Stop();
                    source.loop = false; // 루프 OFF (풀 복원)
                }
            }
        }

    }
    // =========================================================
    // 볼륨 조절 함수 (UI 슬라이더와 연동)
    // 자연스러운 볼륨 변화를 위해 로그 스케일(Log10) 적용
    // =========================================================

    public void SetMasterVolume(float sliderValue)
    {
        float decibel = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat("MasterVolume", decibel);
    }

    public void SetBGMVolume(float sliderValue)
    {
        float decibel = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat("BGMVolume", decibel);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float decibel = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat("SFXVolume", decibel);
    }
}