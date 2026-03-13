using UnityEngine;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Tracks")]
    public AudioClip[] tracks;              // 曲をサイズ分だけ登録

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    public float crossFadeSeconds = 0.4f;

    const string KeyMute = "BGM_Mute";
    const string KeyVol = "BGM_Volume";
    const string KeyIdx = "BGM_Index";

    AudioSource a, b;
    bool activeIsA = true;
    int currentIndex = -1;
    bool muted;
    int adPauseDepth = 0; // 広告中の一時停止ネスト数

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { a, b }) { s.loop = true; s.playOnAwake = false; }

        // 保存値ロード
        muted = PlayerPrefs.GetInt(KeyMute, 0) == 1;
        masterVolume = PlayerPrefs.GetFloat(KeyVol, 0.8f);
        currentIndex = PlayerPrefs.GetInt(KeyIdx, -1);

        if (currentIndex >= 0 && currentIndex < tracks.Length && !muted)
            Play(currentIndex, 0f);
    }

    public void Play(int index, float fade = -1f)
    {
        if (index < 0 || index >= tracks.Length) return;
        if (index == currentIndex && GetActive().isPlaying) return;

        var from = GetActive();
        var to = GetInactive();

        to.clip = tracks[index];
        to.time = 0f;
        to.volume = 0f;
        to.Play();

        float dur = (fade < 0f) ? crossFadeSeconds : fade;
        StopAllCoroutines();
        StartCoroutine(CrossFade(from, to, dur));

        currentIndex = index;
        Save();
    }

    IEnumerator CrossFade(AudioSource from, AudioSource to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;              // ポーズ中でもフェード
            float k = (dur <= 0f) ? 1f : t / dur;
            float vol = muted ? 0f : masterVolume;
            to.volume = vol * k;
            if (from) from.volume = vol * (1f - k);
            yield return null;
        }
        if (from) { from.Stop(); from.clip = null; }
        to.volume = muted ? 0f : masterVolume;
        activeIsA = (to == a);
    }

    AudioSource GetActive() => activeIsA ? a : b;
    AudioSource GetInactive() => activeIsA ? b : a;

    public void SetMute(bool on)
    {
        muted = on;
        var s = GetActive();
        if (muted) s.Pause(); else s.UnPause();
        s.volume = muted ? 0f : masterVolume;
        Save();
    }

    public void SetVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        GetActive().volume = muted ? 0f : masterVolume;
        Save();
    }

    public int GetCurrentIndex() => currentIndex;
    public bool GetMute() => muted;
    public float GetVolume() => masterVolume;

    void Save()
    {
        PlayerPrefs.SetInt(KeyMute, muted ? 1 : 0);
        PlayerPrefs.SetFloat(KeyVol, masterVolume);
        PlayerPrefs.SetInt(KeyIdx, currentIndex);
        PlayerPrefs.Save();
    }

    // === 広告用一時停止 API ===
    public static void PauseForAd()
    {
        if (Instance == null) return;
        Instance.adPauseDepth++;
        Instance.a.Pause();
        Instance.b.Pause();
    }

    public static void ResumeAfterAd()
    {
        if (Instance == null) return;
        if (Instance.adPauseDepth > 0) Instance.adPauseDepth--;
        if (Instance.adPauseDepth > 0) return; // まだ他の広告で停止中

        if (!Instance.muted)
        {
            Instance.a.UnPause();
            Instance.b.UnPause();
        }
    }
}
