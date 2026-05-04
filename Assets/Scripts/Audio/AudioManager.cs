using System.Collections;
using UnityEngine;
using VoxelRoad.Game;

namespace VoxelRoad.Audio
{
    /// <summary>BGM 루프 + 점프·사망 SFX. SerializeField로 의존성 주입.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private SoundConfigSO _config;
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private GameManager _gameManager;

        private Coroutine _fadeRoutine;
        private float _bgmBaseVolume;

        private void Awake()
        {
            if (_config == null)        { Debug.LogError("[AudioManager] _config 미할당");        enabled = false; return; }
            if (_bgmSource == null)     { Debug.LogError("[AudioManager] _bgmSource 미할당");     enabled = false; return; }
            if (_sfxSource == null)     { Debug.LogError("[AudioManager] _sfxSource 미할당");     enabled = false; return; }
            if (_inputReader == null)   { Debug.LogError("[AudioManager] _inputReader 미할당");   enabled = false; return; }
            if (_gameManager == null)   { Debug.LogError("[AudioManager] _gameManager 미할당");   enabled = false; return; }
        }

        private void OnEnable()
        {
            if (_inputReader != null) _inputReader.OnMoveInput += HandleMoveInput;
            if (_gameManager != null) _gameManager.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (_inputReader != null) _inputReader.OnMoveInput -= HandleMoveInput;
            if (_gameManager != null) _gameManager.OnPlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            PlayBgm();
        }

        private void PlayBgm()
        {
            if (_config.Bgm == null) return;
            _bgmBaseVolume = _config.BgmVolume;
            _bgmSource.clip = _config.Bgm;
            _bgmSource.loop = true;
            _bgmSource.volume = _bgmBaseVolume;
            _bgmSource.Play();
        }

        private void HandleMoveInput(MoveDirection _)
        {
            if (_config.Jump == null) return;
            if (_gameManager != null && !_gameManager.IsAlive) return;
            float jitter = _config.JumpPitchJitter;
            _sfxSource.pitch = jitter > 0f ? 1f + Random.Range(-jitter, jitter) : 1f;
            _sfxSource.PlayOneShot(_config.Jump, _config.JumpVolume);
        }

        private void HandlePlayerDied(DeathReason reason)
        {
            var clip = _config.GetDeathClip(reason);
            if (clip != null)
            {
                _sfxSource.pitch = 1f;
                _sfxSource.PlayOneShot(clip, _config.DeathVolume);
            }
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutBgm(_config.BgmFadeOutSeconds));
        }

        private IEnumerator FadeOutBgm(float seconds)
        {
            if (!_bgmSource.isPlaying) yield break;
            float startVol = _bgmSource.volume;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, t / seconds);
                yield return null;
            }
            _bgmSource.Stop();
            _bgmSource.volume = _bgmBaseVolume;
        }
    }
}
