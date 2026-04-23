using UnityEngine;
using VoxelRoad.Rail;

namespace VoxelRoad.World
{
    /// <summary>철길 레인. 바닥 선로 + 기차 스포너 + 경고등.</summary>
    public sealed class RailLane : BaseLane
    {
        [SerializeField] private MeshRenderer _track;
        [SerializeField] private TrainSpawner _spawner;
        [SerializeField] private TrainConfigSO _trainConfig;

        public override LaneType Type => LaneType.Rail;

        protected override void Build()
        {
            if (_track != null)
            {
                _track.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
                var lp = _track.transform.localPosition;
                _track.transform.localPosition = new Vector3(lp.x, 0f, lp.z);
            }
            if (_spawner != null && _trainConfig != null)
            {
                float dir = (_zIndex % 2 == 0) ? 1f : -1f;
                _spawner.Initialize(_trainConfig, dir, _laneSpanX);
            }
        }
    }
}
