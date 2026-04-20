using UnityEngine;
using VoxelRoad.River;

namespace VoxelRoad.World
{
    /// <summary>강 레인. 파란 물 바닥 + 통나무 스포너 + 익사 판정용 물 영역.</summary>
    public sealed class RiverLane : BaseLane
    {
        [SerializeField] private MeshRenderer _water;
        [SerializeField] private LogSpawner _spawner;
        [SerializeField] private LogConfigSO _logConfig;

        public override LaneType Type => LaneType.River;

        protected override void Build()
        {
            if (_water != null)
            {
                _water.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
            }
            if (_spawner != null && _logConfig != null)
            {
                float dir = (_zIndex % 2 == 0) ? 1f : -1f;
                _spawner.Initialize(_logConfig, dir, _laneSpanX);
            }
        }
    }
}
