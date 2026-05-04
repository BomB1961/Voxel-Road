using UnityEngine;
using VoxelRoad.Game;

namespace VoxelRoad.Audio
{
    /// <summary>오디오 클립·볼륨 설정.</summary>
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "VoxelRoad/SoundConfig")]
    public sealed class SoundConfigSO : ScriptableObject
    {
        [Header("BGM")]
        [SerializeField] private AudioClip _bgm;
        [Range(0f, 1f)]
        [SerializeField] private float _bgmVolume = 0.5f;
        [Range(0.05f, 5f)]
        [SerializeField] private float _bgmFadeOutSeconds = 0.5f;

        [Header("Movement SFX (착지 트리거)")]
        [SerializeField] private AudioClip _jump;
        [Range(0f, 1f)]
        [SerializeField] private float _jumpVolume = 0.7f;
        [Tooltip("점프 피치 랜덤 폭. 0이면 항상 동일.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _jumpPitchJitter = 0.05f;

        [Header("Death SFX (DeathReason 별)")]
        [SerializeField] private AudioClip _deathVehicle;
        [SerializeField] private AudioClip _deathDrown;
        [SerializeField] private AudioClip _deathTrain;
        [SerializeField] private AudioClip _deathOutOfBounds;
        [SerializeField] private AudioClip _deathIdle;
        [Range(0f, 1f)]
        [SerializeField] private float _deathVolume = 0.85f;

        public AudioClip Bgm => _bgm;
        public float BgmVolume => _bgmVolume;
        public float BgmFadeOutSeconds => _bgmFadeOutSeconds;

        public AudioClip Jump => _jump;
        public float JumpVolume => _jumpVolume;
        public float JumpPitchJitter => _jumpPitchJitter;

        public float DeathVolume => _deathVolume;

        public AudioClip GetDeathClip(DeathReason reason)
        {
            switch (reason)
            {
                case DeathReason.Vehicle:     return _deathVehicle;
                case DeathReason.Drown:       return _deathDrown;
                case DeathReason.Train:       return _deathTrain;
                case DeathReason.OutOfBounds: return _deathOutOfBounds;
                case DeathReason.Idle:        return _deathIdle;
                default: return null;
            }
        }
    }
}
