using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoxelRoad.Game
{
    /// <summary>게임 상태 전역 관리. 사망 이벤트 + 씬 리로드.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static event Action OnPlayerDied;
        public static bool IsAlive { get; private set; } = true;

        [SerializeField] private KeyCode _restartKey = KeyCode.R;

        private void Awake()
        {
            IsAlive = true;
        }

        private void Update()
        {
            if (!IsAlive && UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public static void KillPlayer(string reason)
        {
            if (!IsAlive) return;
            IsAlive = false;
            Debug.Log($"[GameManager] Player died: {reason}");
            OnPlayerDied?.Invoke();
        }
    }
}
