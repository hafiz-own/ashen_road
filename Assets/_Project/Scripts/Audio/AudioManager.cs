using System.Collections;
using System.Collections.Generic;
using AshenRoad.Core;
using AshenRoad.Data;
using UnityEngine;

namespace AshenRoad.Audio
{
    /// <summary>
    /// Singleton that plays all music and sound from one place. It listens to GameEvents (Observer) and turns
    /// game moments into sounds, so gameplay code never touches an AudioSource.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AudioLibrary library;
        [SerializeField] int sfxVoices = 12;
        [SerializeField] float musicFade = 0.8f;

        AudioSource[] musicSources;
        int activeMusic;
        readonly List<AudioSource> voices = new List<AudioSource>();
        int nextVoice;
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        string currentMusicId;
        float musicVolume = 0.7f;
        float sfxVolume = 0.8f;
        float duck = 1f;
        Coroutine fade;

        public string CurrentMusic => currentMusicId;
        public string LastSound { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            musicSources = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                musicSources[i] = gameObject.AddComponent<AudioSource>();
                musicSources[i].playOnAwake = false;
                musicSources[i].loop = true;
                musicSources[i].volume = 0f;
            }
            for (int i = 0; i < sfxVoices; i++)
            {
                AudioSource s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                voices.Add(s);
            }
        }

        void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.Save != null)
                SetVolumes(GameManager.Instance.Save.MusicVolume, GameManager.Instance.Save.SfxVolume);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.SoundRequested += PlaySfx;
            GameEvents.PlayerActed += OnPlayerActed;
            GameEvents.HitLanded += OnHitLanded;
            GameEvents.PlayerDamaged += OnPlayerDamaged;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.CheckpointActivated += OnCheckpoint;
            GameEvents.HeartPickedUp += OnHeart;
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.BossIntroStarted += OnBossIntro;
            GameEvents.BossDefeated += OnBossDefeated;
        }

        void OnDisable()
        {
            GameEvents.SoundRequested -= PlaySfx;
            GameEvents.PlayerActed -= OnPlayerActed;
            GameEvents.HitLanded -= OnHitLanded;
            GameEvents.PlayerDamaged -= OnPlayerDamaged;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.CheckpointActivated -= OnCheckpoint;
            GameEvents.HeartPickedUp -= OnHeart;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.BossIntroStarted -= OnBossIntro;
            GameEvents.BossDefeated -= OnBossDefeated;
        }

        // ------------------------------------------------------------ public API

        public void SetVolumes(float music, float sfx)
        {
            musicVolume = Mathf.Clamp01(music);
            sfxVolume = Mathf.Clamp01(sfx);
            ApplyMusicVolume();
        }

        public void PlaySfx(string id) => PlaySfx(id, 1f);

        public void PlaySfx(string id, float volumeScale)
        {
            if (library == null || string.IsNullOrEmpty(id)) return;
            if (id == "ending_music")
            {
                PlayMusic("ending");
                return;
            }
            AudioLibrary.Sound sound = library.FindSound(id);
            if (sound == null || sound.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < sound.minInterval) return;
            lastPlayed[id] = now;

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Count;
            voice.clip = sound.clips[Random.Range(0, sound.clips.Length)];
            voice.pitch = 1f + Random.Range(-sound.pitchVariance, sound.pitchVariance);
            voice.volume = sound.volume * sfxVolume * volumeScale;
            voice.Play();
            LastSound = id;
        }

        public void PlayMusic(string id)
        {
            if (library == null || id == currentMusicId) return;
            AudioLibrary.Music music = library.FindMusic(id);
            if (music == null || music.clip == null) return;
            currentMusicId = id;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Crossfade(music));
        }

        public void StopMusic()
        {
            currentMusicId = null;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeOutAll());
        }

        // ------------------------------------------------------------ music

        IEnumerator Crossfade(AudioLibrary.Music music)
        {
            AudioSource from = musicSources[activeMusic];
            activeMusic = 1 - activeMusic;
            AudioSource to = musicSources[activeMusic];
            to.clip = music.clip;
            to.loop = music.loop;
            to.volume = 0f;
            to.Play();
            float target = music.volume;
            float startFrom = from.volume;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / musicFade;
                to.volume = Mathf.Lerp(0f, target * musicVolume * duck, t);
                from.volume = Mathf.Lerp(startFrom, 0f, t);
                yield return null;
            }
            from.Stop();
            fade = null;
        }

        IEnumerator FadeOutAll()
        {
            float t = 0f;
            float a = musicSources[0].volume, b = musicSources[1].volume;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / musicFade;
                musicSources[0].volume = Mathf.Lerp(a, 0f, t);
                musicSources[1].volume = Mathf.Lerp(b, 0f, t);
                yield return null;
            }
            musicSources[0].Stop();
            musicSources[1].Stop();
            fade = null;
        }

        void ApplyMusicVolume()
        {
            if (fade != null || musicSources == null) return;
            AudioLibrary.Music m = currentMusicId != null && library != null ? library.FindMusic(currentMusicId) : null;
            if (m != null) musicSources[activeMusic].volume = m.volume * musicVolume * duck;
        }

        // ------------------------------------------------------------ events

        void OnGameStateChanged(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.MainMenu:
                    duck = 1f;
                    PlayMusic("title");
                    break;
                case GameState.Playing:
                    duck = 1f;
                    if (from == GameState.Loading)
                    {
                        LevelData level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : null;
                        currentMusicId = null;
                        PlayMusic(level != null ? level.musicId : "level_a");
                    }
                    ApplyMusicVolume();
                    break;
                case GameState.Paused:
                    duck = 0.35f;
                    ApplyMusicVolume();
                    break;
                case GameState.GameOver:
                    StopMusic();
                    PlaySfx("game_over");
                    break;
                case GameState.LevelComplete:
                    StopMusic();
                    PlaySfx("level_complete");
                    break;
                case GameState.GameFinished:
                    duck = 1f;
                    PlayMusic("ending");
                    break;
            }
        }

        void OnPlayerActed(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Jump: PlaySfx("jump"); break;
                case PlayerAction.Land: PlaySfx("land"); break;
                case PlayerAction.Roll: PlaySfx("roll"); break;
                case PlayerAction.Swing: PlaySfx("swing"); break;
                case PlayerAction.Footstep: PlaySfx("footstep", 0.6f); break;
            }
        }

        void OnHitLanded(Vector2 point, bool blocked) => PlaySfx(blocked ? "clang" : "hit");

        void OnPlayerDamaged() => PlaySfx("hurt");

        void OnPlayerDied(DeathCause cause) => PlaySfx("player_death");

        void OnCheckpoint(int index) => PlaySfx("checkpoint");

        void OnHeart() => PlaySfx("heart");

        void OnBossIntro() => PlayMusic("boss");

        void OnBossDefeated() => StopMusic();
    }
}
