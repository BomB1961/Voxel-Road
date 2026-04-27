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
        [Tooltip("사망 임계값 보정. 0이면 플레이어 중심이 화면 하단 도달 시 사망. 양수로 올리면 더 일찍 사망.\n시작 위치(player.z=0)에서 visibleBottomZ까지 거리(약 0.22)보다 큰 값을 주면 시작 즉시 사망하니 주의.")]
        [SerializeField] private float _killOffsetZ = 0f;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_gameManager == null) { Debug.LogError("[ChasingWall] _gameManager 미지정", this); enabled = false; return; }
            if (_playerController == null) { Debug.LogError("[ChasingWall] _playerController 미지정", this); enabled = false; return; }
            if (_camera == null) { Debug.LogError("[ChasingWall] Camera.main 없음", this); enabled = false; return; }
        }

        private void LateUpdate()
        {
            // 사망 후엔 벽 위치 동결 — 잔여 카메라 lerp이 있어도 벽은 그대로.
            if (!_gameManager.IsAlive) return;

            // 화면 하단 가장자리가 지면(Y=0)과 만나는 Z를 매 프레임 계산.
            // 카메라가 직각 투영(orthographic)이므로 frustum 하단 평면은 camPos − up*orthoSize 점을
            // 지나며 카메라 forward 방향으로 뻗는다. 그 ray가 Y=0 평면과 만나는 Z 좌표가 화면 하단.
            // 이렇게 하면 카메라 Y/pitch/orthoSize 변경에도 자동 추종.
            Vector3 camPos = _camera.transform.position;
            Vector3 camFwd = _camera.transform.forward;
            Vector3 camUp = _camera.transform.up;
            Vector3 bottomEdgeOrigin = camPos - camUp * _camera.orthographicSize;
            // 카메라가 평행/위쪽을 보면 ray가 지면에 닿지 않음 — 그 경우 사망 판정 생략.
            if (camFwd.y >= -0.001f) return;
            float t = -bottomEdgeOrigin.y / camFwd.y;
            float visibleBottomZ = bottomEdgeOrigin.z + t * camFwd.z;

            var p = transform.position;
            transform.position = new Vector3(p.x, _yPosition, visibleBottomZ);

            // 플레이어 앞면(카메라쪽 면)이 화면 하단을 넘는 순간 사망 — 시각적 접촉과 동시.
            if (_playerController.transform.position.z < visibleBottomZ + _killOffsetZ)
                _gameManager.KillPlayer(DeathReason.OutOfBounds);
        }
    }
}
