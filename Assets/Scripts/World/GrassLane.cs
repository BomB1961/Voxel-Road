using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>잔디 레인. 녹색 바닥 + 확률적 데코(나무·바위·꽃) 배치. 데코 위치는 플레이어 통행 불가.</summary>
    public sealed class GrassLane : BaseLane
    {
        [SerializeField] private MeshRenderer _ground;
        [SerializeField] private Transform _decorRoot;

        private LaneConfigSO _config;
        private readonly System.Collections.Generic.HashSet<int> _blockedCells = new();

        public override LaneType Type => LaneType.Grass;

        public void SetConfig(LaneConfigSO config) => _config = config;

        /// <summary>데코가 점유한 X 셀 (타일 중심 좌표 int) — 충돌 판정에서 사용.</summary>
        public System.Collections.Generic.IReadOnlyCollection<int> BlockedCells => _blockedCells;

        protected override void Build()
        {
            _blockedCells.Clear();

            // 바닥 스케일: Unity Plane = 10m → laneSpanX / 10
            if (_ground != null)
            {
                _ground.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
            }

            if (_decorRoot == null || _config == null) return;
            var prefabs = _config.GrassDecorPrefabs;
            if (prefabs == null || prefabs.Length == 0) return;

            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                if (Random.value > _config.GrassDecorDensity) continue;
                var prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null) continue;
                var decor = Instantiate(prefab, _decorRoot);
                decor.transform.localPosition = new Vector3(x + 0.5f, 0f, 0f);
                decor.transform.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                _blockedCells.Add(x);
            }
        }
    }
}
