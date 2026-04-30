using UnityEngine;
using VoxelRoad.Vehicles;

namespace VoxelRoad.World
{
    /// <summary>도로 레인. 회색 아스팔트 + 중앙 점선 마커 + 차량 스폰.</summary>
    public sealed class RoadLane : BaseLane
    {
        [SerializeField] private MeshRenderer _asphalt;
        [SerializeField] private Transform _markerRoot;
        [SerializeField] private GameObject _markerSegment;
        [SerializeField] private VehicleSpawner _spawner;
        [SerializeField] private VehicleConfigSO _vehicleConfig;

        [Header("Decor")]
        [SerializeField] private Material _curbMaterial;

        [Header("Color Jitter")]
        [SerializeField, Range(0f, 1f)] private float _patchDensity = 0.25f;
        [SerializeField, Range(0f, 0.2f)] private float _patchJitter = 0.08f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        // MonoBehaviour 정적 필드 초기자에서 native 객체(MaterialPropertyBlock)를 생성하면
        // Unity가 CreateImpl을 거부해 타입 로딩이 실패함 → 첫 사용 시 지연 생성.
        private static MaterialPropertyBlock _patchMpb;

        public override LaneType Type => LaneType.Road;

        // 도로 청크 내부 경계에만 점선을 그리기 위한 플래그. 기본값은 양쪽 모두 그림.
        private bool _drawBackEdge = true;   // localZ = -0.5 (이전 레인과의 경계)
        private bool _drawFrontEdge = true;  // localZ = +0.5 (다음 레인과의 경계)

        /// <summary>도로 청크 내 위치에 따른 경계 점선 표시 여부. 청크 맨 앞/뒤는 바깥쪽 점선 생략.</summary>
        public void SetLaneEdges(bool drawBack, bool drawFront)
        {
            _drawBackEdge = drawBack;
            _drawFrontEdge = drawFront;
        }

        protected override void Build()
        {
            // 차량 진행 방향: 짝수 zIndex → +X, 홀수 → -X
            if (_spawner != null && _vehicleConfig != null)
            {
                float dir = (_zIndex % 2 == 0) ? 1f : -1f;
                _spawner.Initialize(_vehicleConfig, dir, _laneSpanX, _difficultyMultiplier);
            }
            // 레인 타입별 Y 오프셋 (Grass=-0.02, Road=0, River=-0.01)로 Z-fighting 방지.
            if (_asphalt != null)
            {
                _asphalt.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
                var lp = _asphalt.transform.localPosition;
                _asphalt.transform.localPosition = new Vector3(lp.x, 0f, lp.z);
            }

            if (_markerRoot == null) return;

            // 기존 마커 제거 (재활용 시 대비)
            for (int i = _markerRoot.childCount - 1; i >= 0; i--)
                Destroy(_markerRoot.GetChild(i).gameObject);

            // 차선 점선: 차량(레인 중앙 주행)의 양옆에 오도록 레인 경계(±0.5)에 두 줄 배치.
            // 인접 Road 레인끼리는 같은 worldZ에 겹쳐 그려지지만 동일 형상이라 한 줄로 보임.
            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            var rows = new System.Collections.Generic.List<float>(2);
            if (_drawBackEdge) rows.Add(-0.5f);
            if (_drawFrontEdge) rows.Add(0.5f);
            for (int row = 0; row < rows.Count; row++)
            {
                float zOffset = rows[row];
                for (int x = -halfSpan; x < halfSpan; x += 2)
                {
                    GameObject marker;
                    if (_markerSegment != null)
                    {
                        marker = Instantiate(_markerSegment, _markerRoot);
                    }
                    else
                    {
                        marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        marker.transform.SetParent(_markerRoot, false);
                        var mr = marker.GetComponent<MeshRenderer>();
                        mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
                        Destroy(marker.GetComponent<Collider>());
                    }
                    marker.transform.localPosition = new Vector3(x + 0.5f, 0.01f, zOffset);
                    marker.transform.localScale = new Vector3(0.6f, 0.02f, 0.08f);
                }
            }

            BuildCurbs();
            BuildColorPatches();
        }

        private void BuildColorPatches()
        {
            if (_asphalt == null || _patchDensity <= 0f) return;
            Material baseMat = _asphalt.sharedMaterial;
            if (baseMat == null || !baseMat.HasProperty(BaseColorId)) return;
            Color baseColor = baseMat.GetColor(BaseColorId);

            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            for (int x = -halfSpan; x < halfSpan; x++)
            {
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

                // 아스팔트 y=0 위에 살짝. 점선 마커(z=±0.5, y=0.01) 영역과 Z 분리(z=0).
                patch.transform.localPosition = new Vector3(x, 0.005f, 0f);
                patch.transform.localScale = new Vector3(0.6f, 0.005f, 0.6f);
            }
        }

        private void BuildCurbs()
        {
            if (_curbMaterial == null) return;
            // 청크 바깥 경계(잔디·강·철길과 맞닿는 면)에만 연석 배치 → 도로 청크의 진입/이탈을 시각적으로 강조.
            // _drawBackEdge=false 면 이 레인이 청크의 시작(back이 바깥 경계),
            // _drawFrontEdge=false 면 청크의 끝(front가 바깥 경계).
            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            if (!_drawBackEdge) SpawnCurbRow(halfSpan, -0.45f);
            if (!_drawFrontEdge) SpawnCurbRow(halfSpan, 0.45f);
        }

        private void SpawnCurbRow(int halfSpan, float zLocal)
        {
            // 차량 Z 콜라이더 ±0.4. 연석 z=±0.45·Z=0.08 → 차량 외연과 0.01m 간격, 레인 경계(±0.5)와 0.01m 간격.
            // X=1로 셀당 1개 생성 시 인접 셀 연석과 정확히 맞닿아 연속된 띠로 보임.
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                var curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = "Curb";
                curb.transform.SetParent(transform, false);
                Destroy(curb.GetComponent<Collider>());
                curb.GetComponent<MeshRenderer>().sharedMaterial = _curbMaterial;
                curb.transform.localPosition = new Vector3(x, 0.05f, zLocal);
                curb.transform.localScale = new Vector3(1f, 0.10f, 0.08f);
            }
        }
    }
}
