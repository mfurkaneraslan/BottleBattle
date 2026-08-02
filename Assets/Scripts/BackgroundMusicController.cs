using UnityEngine;

namespace BottleBattle
{
    /// <summary>
    /// Keeps one looping music source alive while the player moves between screens.
    /// </summary>
    public sealed class BackgroundMusicController : MonoBehaviour
    {
        private const string MusicResourcePath = "Audio/Bottle Swap Drift";
        private const string MusicVolumeKey = "BottleBattle.MusicVolume";
        private const string MusicMutedKey = "BottleBattle.MusicMuted";
        private const float DefaultVolume = 0.23f;

        private static BackgroundMusicController instance;

        private AudioSource musicSource;

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

            AudioClip music = Resources.Load<AudioClip>(MusicResourcePath);
            if (music == null)
            {
                Debug.LogWarning($"Background music was not found at Resources/{MusicResourcePath}.");
                return;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.ignoreListenerPause = true;
            ApplySavedSettings();
            musicSource.Play();
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
