// Assets/Scripts/Camera/CrossyRoadCameraExtension.cs
// Crossy Road 카메라: 플레이어가 전진할 때만 Z를 따라가고 후퇴 시 카메라 Z 고정. 좌우는 댐핑.
using UnityEngine;
using Unity.Cinemachine;

namespace VoxelRoad.CameraSystem
{
    [SaveDuringPlay]
    public sealed class CrossyRoadCameraExtension : CinemachineExtension
    {
        [Header("Target")]
        [SerializeField] private Transform _player;

        [Header("Follow Offset (target 기준)")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 10f, -8f);

        [Tooltip("true면 플레이어 Y 무시하고 월드 고정 Y를 사용 (점프 아크가 카메라에 전이되지 않음)")]
        [SerializeField] private bool _useFixedY = true;
        [SerializeField] private float _baseGroundY = 0f;

        [Header("Follow Tuning")]
        [SerializeField] private float _forwardFollowSpeed = 5f;
        [SerializeField] private float _lateralDamping = 2f;

        private float _maxFollowedZ = float.NegativeInfinity;
        private bool _initialized;

        public Transform Player { get => _player; set => _player = value; }
        public Vector3 FollowOffset { get => _followOffset; set => _followOffset = value; }
        public float ForwardFollowSpeed { get => _forwardFollowSpeed; set => _forwardFollowSpeed = value; }
        public float LateralDamping { get => _lateralDamping; set => _lateralDamping = value; }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body) return;
            if (_player == null) return;

            Vector3 playerPos = _player.position;
            if (!_initialized || playerPos.z > _maxFollowedZ)
            {
                _maxFollowedZ = playerPos.z;
                _initialized = true;
            }

            Vector3 pos = state.RawPosition;
            float dt = Mathf.Max(0f, deltaTime);

            float targetX = playerPos.x + _followOffset.x;
            // 점프 아크(플레이어 Y)를 카메라에 반영하면 화면 기울어짐 발생 → 고정 Y 사용.
            float targetY = (_useFixedY ? _baseGroundY : playerPos.y) + _followOffset.y;
            float targetZ = _maxFollowedZ + _followOffset.z;

            float tX = dt > 0f ? Mathf.Clamp01(dt * _lateralDamping) : 1f;
            float tZ = dt > 0f ? Mathf.Clamp01(dt * _forwardFollowSpeed) : 1f;

            pos.x = Mathf.Lerp(pos.x, targetX, tX);
            pos.y = targetY;
            pos.z = Mathf.Lerp(pos.z, targetZ, tZ);

            state.RawPosition = pos;
        }
    }
}
