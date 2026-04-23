// Assets/Scripts/Camera/PlayerJumpShakeTrigger.cs
// 플레이어 착지 시 CameraShakeController 트리거. C# event 로 외부 훅 노출.
using System;
using UnityEngine;

namespace VoxelRoad.CameraSystem
{
    public sealed class PlayerJumpShakeTrigger : MonoBehaviour
    {
        [Header("Shake")]
        [SerializeField] private float _intensity = 0.4f;
        [SerializeField] private float _duration = 0.15f;

        [Header("Dependencies")]
        [SerializeField] private CameraShakeController _shaker;

        /// <summary>착지 이벤트. UI·SFX 등 외부 구독자용.</summary>
        public event Action OnLanded;

        private void Awake()
        {
            if (_shaker == null)
                Debug.LogWarning("[PlayerJumpShakeTrigger] _shaker 미지정 — 흔들림 비활성");
        }

        /// <summary>PlayerController의 착지 타이밍 또는 외부에서 호출.</summary>
        public void OnPlayerLanded()
        {
            OnLanded?.Invoke();
            if (_shaker != null)
                _shaker.Shake(_intensity, _duration);
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnPlayerLanded();
        }
    }
}
