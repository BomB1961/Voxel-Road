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

        public override bool IsBlockedAt(int x) => _blockedCells.Contains(x);

        protected override void Build()
        {
            _blockedCells.Clear();

            // 바닥 스케일: Unity Plane = 10m → laneSpanX / 10.
            // Y를 도로/강 대비 -0.01 낮춰 경계 Z-fighting 방지(도로 마커가 잔디 위에 얹혀 보이는 현상 제거).
            if (_ground != null)
            {
                _ground.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
                var lp = _ground.transform.localPosition;
                _ground.transform.localPosition = new Vector3(lp.x, -0.01f, lp.z);
            }

            if (_decorRoot == null || _config == null) return;
            var prefabs = _config.GrassDecorPrefabs;
            if (prefabs == null || prefabs.Length == 0) return;

            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            // 연속 차단 방지: 같은 레인에서 3셀 이상 연속 차단되면 벽처럼 느껴져 길이 막힘.
            // 직전 2셀이 연속 차단 상태면 현재 셀은 강제로 비워 둠.
            int consecutiveBlocked = 0;
            const int MAX_CONSECUTIVE = 2;
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                bool place = Random.value <= _config.GrassDecorDensity;
                if (place && consecutiveBlocked >= MAX_CONSECUTIVE) place = false;
                if (!place) { consecutiveBlocked = 0; continue; }

                var prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null) { consecutiveBlocked = 0; continue; }
                var decor = Instantiate(prefab, _decorRoot);
                // 셀 정중앙에 배치 (플레이어 GridPosition.ToWorldPosition 과 동일한 정수 X 규약).
                decor.transform.localPosition = new Vector3(x, 0f, 0f);
                decor.transform.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                float s = _config.GrassDecorScale;
                if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                    decor.transform.localScale = new Vector3(s, s, s);
                _blockedCells.Add(x);
                consecutiveBlocked++;
            }
        }
    }
}
