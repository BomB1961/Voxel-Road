using UnityEngine;

namespace VoxelRoad.Game
{
    public sealed class EagleController : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private EagleConfigSO _config;

        private Camera _camera;
        private GridPosition _lastGridPos;
        private float _idleTimer;

        private void Awake()
        {
            _camera = Camera.main;

            if (_gameManager == null) { Debug.LogError("[EagleController] _gameManager is null", this); enabled = false; return; }
            if (_playerController == null) { Debug.LogError("[EagleController] _playerController is null", this); enabled = false; return; }
            if (_config == null) { Debug.LogError("[EagleController] _config is null", this); enabled = false; return; }
            if (_camera == null) { Debug.LogError("[EagleController] Camera.main not found", this); enabled = false; return; }
        }

        private void OnEnable()
        {
            if (_gameManager != null)
                _gameManager.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (_gameManager != null)
                _gameManager.OnPlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            _lastGridPos = _playerController.GridPos;
        }

        private void Update()
        {
            if (!_gameManager.IsAlive) return;

            CheckIdleTimeout();
            CheckCameraBottom();
        }

        private void CheckIdleTimeout()
        {
            GridPosition currentGrid = _playerController.GridPos;
            if (currentGrid != _lastGridPos)
            {
                _lastGridPos = currentGrid;
                _idleTimer = 0f;
                return;
            }

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _config.IdleTimeoutSeconds)
                _gameManager.KillPlayer(DeathReason.Eagle);
        }

        private void CheckCameraBottom()
        {
            float playerZ = _playerController.transform.position.z;
            float cameraMinZ = _camera.transform.position.z - _config.CameraTrailOffsetZ;
            if (playerZ < cameraMinZ)
                _gameManager.KillPlayer(DeathReason.OutOfBounds);
        }

        private void HandlePlayerDied(DeathReason _)
        {
            _idleTimer = 0f;
        }
    }
}
