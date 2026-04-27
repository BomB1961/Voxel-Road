using UnityEngine;

namespace VoxelRoad.Game
{
    /// <summary>카메라 Z를 따라가는 추격 벽. 시각 + OutOfBounds 사망 트리거 통합.
    /// 자동 전진 카메라(CrossyRoadCameraExtension)가 시간 기반으로 카메라 Z를 밀어내고,
    /// 플레이어가 카메라 Z를 넘어 뒤로 떨어지면 사망.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class ChasingWall : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _yPosition = 0.5f;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_gameManager == null) { Debug.LogError("[ChasingWall] _gameManager 미지정", this); enabled = false; return; }
            if (_playerController == null) { Debug.LogError("[ChasingWall] _playerController 미지정", this); enabled = false; return; }
            if (_camera == null) { Debug.LogError("[ChasingWall] Camera.main 없음", this); enabled = false; return; }
        }

        private void LateUpdate()
        {
            float camZ = _camera.transform.position.z;
            var p = transform.position;
            transform.position = new Vector3(p.x, _yPosition, camZ);

            if (!_gameManager.IsAlive) return;
            if (_playerController.transform.position.z < camZ)
                _gameManager.KillPlayer(DeathReason.OutOfBounds);
        }
    }
}
