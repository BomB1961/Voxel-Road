using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>잔디 레인. 녹색 바닥 + 확률적 데코(나무·바위·꽃) 배치. 데코 위치는 플레이어 통행 불가.</summary>
    public sealed class GrassLane : BaseLane
    {
        [SerializeField] private MeshRenderer _ground;
        [SerializeField] private Transform _decorRoot;

        [Header("Color Jitter")]
        [SerializeField, Range(0f, 1f)] private float _patchDensity = 0.30f;
        [SerializeField, Range(0f, 0.2f)] private float _patchJitter = 0.08f;

        private LaneConfigSO _config;
        private bool _isSafeStart;
        private readonly System.Collections.Generic.HashSet<int> _blockedCells = new();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        // MonoBehaviour 정적 필드 초기자에서 native 객체(MaterialPropertyBlock)를 생성하면
        // Unity가 CreateImpl을 거부해 타입 로딩이 실패함 → 첫 사용 시 지연 생성.
        private static MaterialPropertyBlock _patchMpb;

        public override LaneType Type => LaneType.Grass;

        public void SetConfig(LaneConfigSO config) => _config = config;

        /// <summary>시작 안전 구간 표시(플레이어 시작 지점 셀에는 데코를 배치하지 않음).</summary>
        public void SetSafeStart(bool safe) => _isSafeStart = safe;

        /// <summary>데코가 점유한 X 셀 (타일 중심 좌표 int) — 충돌 판정에서 사용.</summary>
        public System.Collections.Generic.IReadOnlyCollection<int> BlockedCells => _blockedCells;

        public override bool IsBlockedAt(int x) => _blockedCells.Contains(x);

        protected override void Build()
        {
            _blockedCells.Clear();

            // 바닥 스케일: Unity Plane = 10m → laneSpanX / 10.
            // 레인 타입별 Y 오프셋으로 Z-fighting 완전 분리 (Grass=-0.02, Road=0, River=-0.01).
            if (_ground != null)
            {
                _ground.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
                var lp = _ground.transform.localPosition;
                _ground.transform.localPosition = new Vector3(lp.x, -0.02f, lp.z);
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
                // 안전 구간 레인은 플레이어 시작 셀(x=0) 및 좌우 1셀을 비워 길을 보장
                if (place && _isSafeStart && Mathf.Abs(x) <= 1) place = false;
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

            BuildColorPatches();
        }

        private void BuildColorPatches()
        {
            if (_ground == null || _patchDensity <= 0f) return;
            Material baseMat = _ground.sharedMaterial;
            if (baseMat == null || !baseMat.HasProperty(BaseColorId)) return;
            Color baseColor = baseMat.GetColor(BaseColorId);

            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                if (_blockedCells.Contains(x)) continue;
                if (Random.value > _patchDensity) continue;

                var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                patch.name = "Patch";
                patch.transform.SetParent(transform, false);
                Destroy(patch.GetComponent<Collider>());
                var mr = patch.GetComponent<MeshRenderer>();
                mr.sharedMaterial = baseMat;

                Color jittered = new Color(
                    Mathf.Clamp01(baseColor.r + Random.Range(-_patchJitter, _patchJitter)),
                    Mathf.Clamp01(baseColor.g + Random.Range(-_patchJitter, _patchJitter)),
                    Mathf.Clamp01(baseColor.b + Random.Range(-_patchJitter, _patchJitter)),
                    1f);
                if (_patchMpb == null) _patchMpb = new MaterialPropertyBlock();
                _patchMpb.Clear();
                _patchMpb.SetColor(BaseColorId, jittered);
                mr.SetPropertyBlock(_patchMpb);

                // Grass 지면 y=-0.02 위에 살짝 떠 있게. 두께는 매우 얇게.
                patch.transform.localPosition = new Vector3(x, -0.014f, 0f);
                patch.transform.localScale = new Vector3(0.6f, 0.005f, 0.6f);
            }
        }
    }
}
