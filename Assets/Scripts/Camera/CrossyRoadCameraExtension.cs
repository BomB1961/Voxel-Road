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
        [SerializeField] private float _forwardFollowSpeed = 1.5f;
        [SerializeField] private float _lateralDamping = 1.5f;

        [Tooltip("시작 지점 기준 이 거리(레인 수) 이내에서는 카메라 Z 고정. 이후부터만 추적 시작.")]
        [SerializeField] private float _forwardDeadzoneLanes = 4f;

        [Header("Map Boundary Clamp")]
        [Tooltip("맵 X 절반 크기(LaneSpanX / 2). WorldConfig의 LaneSpanX와 일치시킬 것.")]
        [SerializeField] private float _mapHalfSpan = 25f;
        [Tooltip("직교 카메라 가시 X 반폭 = OrthographicSize × Aspect. Inspector에서 직접 측정해 입력.")]
        [SerializeField] private float _visibleHalfWidth = 5.73f;

        /// <summary>카메라가 더 이상 따라가지 않는 X 한계. PlayerController 경계 동기화에 사용.</summary>
        public static float MapXLimit { get; private set; } = 19f;

        private float _startPlayerZ;
        private bool _initialized;

        public Transform Player { get => _player; set => _player = value; }
        public Vector3 FollowOffset { get => _followOffset; set => _followOffset = value; }
        public float ForwardFollowSpeed { get => _forwardFollowSpeed; set => _forwardFollowSpeed = value; }
        public float LateralDamping { get => _lateralDamping; set => _lateralDamping = value; }
        public float ForwardDeadzoneLanes { get => _forwardDeadzoneLanes; set => _forwardDeadzoneLanes = value; }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body) return;
            if (_player == null) return;

            Vector3 playerPos = _player.position;
            if (!_initialized)
            {
                _startPlayerZ = playerPos.z;
                _initialized = true;
            }

            Vector3 pos = state.RawPosition;
            float dt = Mathf.Max(0f, deltaTime);

            float targetX = playerPos.x + _followOffset.x;
            float targetY = (_useFixedY ? _baseGroundY : playerPos.y) + _followOffset.y;
            float effectivePlayerZ = Mathf.Max(_startPlayerZ, playerPos.z - _forwardDeadzoneLanes);
            float targetZ = effectivePlayerZ + _followOffset.z;

            // 카메라 X 클램프: 화면 끝이 맵 경계(±_mapHalfSpan)를 넘지 않도록.
            // MapXLimit = _mapHalfSpan - _visibleHalfWidth
            // 카메라 중심이 MapXLimit 에 있을 때 화면 끝 = mapHalfSpan (맵 경계 정확히 일치)
            float limit = Mathf.Max(0f, _mapHalfSpan - _visibleHalfWidth);
            MapXLimit = limit;
            targetX = Mathf.Clamp(targetX, -limit, limit);

            float tX = dt > 0f ? Mathf.Clamp01(dt * _lateralDamping) : 1f;
            float tZ = dt > 0f ? Mathf.Clamp01(dt * _forwardFollowSpeed) : 1f;

            pos.x = Mathf.Lerp(pos.x, targetX, tX);
            pos.y = targetY;
            pos.z = Mathf.Lerp(pos.z, targetZ, tZ);

            state.RawPosition = pos;
        }
    }
}
