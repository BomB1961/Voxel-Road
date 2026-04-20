// Assets/Scripts/Camera/PlayerJumpShakeTrigger.cs
// 플레이어 착지 시 CameraShakeController 트리거. UnityEvent로 외부 훅 노출.
using UnityEngine;
using UnityEngine.Events;

namespace VoxelRoad.CameraSystem
{
    public sealed class PlayerJumpShakeTrigger : MonoBehaviour
    {
        [Header("Shake")]
        [SerializeField] private float _intensity = 0.4f;
        [SerializeField] private float _duration = 0.15f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onLanded;

        public UnityEvent OnLanded => _onLanded;

        /// <summary>PlayerController의 착지 타이밍 또는 외부에서 호출.</summary>
        public void OnPlayerLanded()
        {
            _onLanded?.Invoke();
            if (CameraShakeController.Instance != null)
                CameraShakeController.Instance.Shake(_intensity, _duration);
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnPlayerLanded();
        }
    }
}
