using UnityEngine;

namespace BottleBattle
{
    /// <summary>
    /// Keeps one looping music source and the shared game sound effects alive
    /// while the player moves between screens.
    /// </summary>
    public sealed class BackgroundMusicController : MonoBehaviour
    {
        private const string MusicResourcePath = "Audio/Bottle Swap Drift";
        private const string DropSoundResourcePath = "Audio/Bottle Drop";
        private const string MusicVolumeKey = "BottleBattle.MusicVolume";
        private const string MusicMutedKey = "BottleBattle.MusicMuted";
        private const float DefaultVolume = 0.23f;
        private const float DropSoundVolume = 0.7f;
        private const float DropSoundStartTime = 0.5f;
        private const float DropSoundDuration = 0.5f;

        private static BackgroundMusicController instance;

        private AudioSource musicSource;
        private AudioSource dropSoundSource;
        private AudioClip musicClip;
        private AudioClip dropSoundClip;
        private bool musicStarted;
        private bool dropSoundPending;
        private float dropSoundStopAt = -1f;

        public static float Volume => PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
        public static bool IsMuted => PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureMusicExists()
        {
            if (instance != null)
            {
                return;
            }

            var musicObject = new GameObject("Background Music");
            instance = musicObject.AddComponent<BackgroundMusicController>();
            DontDestroyOnLoad(musicObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // This IMGUI-only scene does not need a Camera, so it also has no
            // automatically-created AudioListener. AudioSources are silent without one.
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }

            musicClip = Resources.Load<AudioClip>(MusicResourcePath);
            if (musicClip == null)
            {
                Debug.LogWarning($"Background music was not found at Resources/{MusicResourcePath}.");
            }
            else
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.clip = musicClip;
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
                musicSource.ignoreListenerPause = true;
                ApplySavedSettings();
                musicClip.LoadAudioData();
            }

            dropSoundClip = Resources.Load<AudioClip>(DropSoundResourcePath);
            if (dropSoundClip == null)
            {
                Debug.LogWarning($"Drop sound was not found at Resources/{DropSoundResourcePath}.");
            }
            else
            {
                dropSoundSource = gameObject.AddComponent<AudioSource>();
                dropSoundSource.clip = dropSoundClip;
                dropSoundSource.loop = false;
                dropSoundSource.playOnAwake = false;
                dropSoundSource.spatialBlend = 0f;
                dropSoundSource.volume = DropSoundVolume;
                dropSoundClip.LoadAudioData();
            }
        }

        private void Update()
        {
            if (!musicStarted && musicSource != null && musicClip.loadState == AudioDataLoadState.Loaded)
            {
                musicSource.Play();
                musicStarted = true;
            }

            if (dropSoundPending && dropSoundClip != null && dropSoundClip.loadState == AudioDataLoadState.Loaded)
            {
                StartDropSound();
            }

            if (dropSoundStopAt >= 0f && Time.unscaledTime >= dropSoundStopAt)
            {
                dropSoundSource?.Stop();
                dropSoundStopAt = -1f;
            }
        }

        public static void PlayDropSound()
        {
            if (instance?.dropSoundSource == null)
            {
                return;
            }

            if (instance.dropSoundClip.loadState != AudioDataLoadState.Loaded)
            {
                instance.dropSoundPending = true;
                instance.dropSoundClip.LoadAudioData();
                return;
            }

            instance.StartDropSound();
        }

        private void StartDropSound()
        {
            dropSoundPending = false;
            dropSoundSource.Stop();
            dropSoundSource.time = DropSoundStartTime;
            dropSoundSource.Play();
            dropSoundStopAt = Time.unscaledTime + DropSoundDuration;
        }

        public static void SetVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, clampedVolume);
            PlayerPrefs.Save();

            if (instance?.musicSource != null)
            {
                instance.musicSource.volume = clampedVolume;
            }
        }

        public static void SetMuted(bool muted)
        {
            PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();

            if (instance?.musicSource != null)
            {
                instance.musicSource.mute = muted;
            }
        }

        private void ApplySavedSettings()
        {
            musicSource.volume = Volume;
            musicSource.mute = IsMuted;
        }
    }
}
