using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoxelRoad.Game
{
    /// <summary>게임 상태 관리(인스턴스). 사망 이벤트 + R키 씬 리로드.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>플레이어 사망 이벤트. 구독자는 SerializeField로 GameManager 참조 주입 후 +=.</summary>
        public event Action<DeathReason> OnPlayerDied;

        public bool IsAlive { get; private set; } = true;

        private void Awake()
        {
            IsAlive = true;
        }

        private void Update()
        {
            if (IsAlive) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void KillPlayer(DeathReason reason)
        {
            if (!IsAlive) return;
            IsAlive = false;
#if UNITY_EDITOR
            Debug.Log($"[GameManager] Player died: {reason}");
#endif
            OnPlayerDied?.Invoke(reason);
        }
    }
}
