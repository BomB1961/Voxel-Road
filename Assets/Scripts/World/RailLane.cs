using UnityEngine;
using VoxelRoad.Rail;

namespace VoxelRoad.World
{
    /// <summary>철길 레인. 바닥 선로 + 침목 + 기차 스포너 + 경고등.</summary>
    public sealed class RailLane : BaseLane
    {
        [SerializeField] private MeshRenderer _track;
        [SerializeField] private TrainSpawner _spawner;
        [SerializeField] private TrainConfigSO _trainConfig;

        [Header("Decor")]
        [SerializeField] private Material _sleeperMaterial;

        public override LaneType Type => LaneType.Rail;

        protected override void Build()
        {
            if (_track != null)
            {
                _track.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
                var lp = _track.transform.localPosition;
                _track.transform.localPosition = new Vector3(lp.x, 0f, lp.z);
            }

            BuildSleepers();

            if (_spawner != null && _trainConfig != null)
            {
                float dir = (_zIndex % 2 == 0) ? 1f : -1f;
                _spawner.Initialize(_trainConfig, dir, _laneSpanX, _difficultyMultiplier);
            }
        }

        private void BuildSleepers()
        {
            if (_sleeperMaterial == null) return;
            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                var sleeper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sleeper.name = "Sleeper";
                sleeper.transform.SetParent(transform, false);
                Destroy(sleeper.GetComponent<Collider>());
                sleeper.GetComponent<MeshRenderer>().sharedMaterial = _sleeperMaterial;
                // 트랙 위 살짝 솟음. 침목은 X(주행 방향)에 좁고 Z(레인 폭)에 넓은 막대.
                sleeper.transform.localPosition = new Vector3(x, 0.04f, 0f);
                sleeper.transform.localScale = new Vector3(0.7f, 0.08f, 0.85f);
            }
        }
    }
}
