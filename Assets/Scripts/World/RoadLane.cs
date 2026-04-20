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

        public override LaneType Type => LaneType.Road;

        protected override void Build()
        {
            // 차량 진행 방향: 짝수 zIndex → +X, 홀수 → -X
            if (_spawner != null && _vehicleConfig != null)
            {
                float dir = (_zIndex % 2 == 0) ? 1f : -1f;
                _spawner.Initialize(_vehicleConfig, dir, _laneSpanX);
            }
            if (_asphalt != null)
            {
                _asphalt.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
            }

            if (_markerRoot == null) return;

            // 기존 마커 제거 (재활용 시 대비)
            for (int i = _markerRoot.childCount - 1; i >= 0; i--)
                Destroy(_markerRoot.GetChild(i).gameObject);

            // 중앙 점선: 2타일 간격
            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
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
                marker.transform.localPosition = new Vector3(x + 0.5f, 0.01f, 0f);
                marker.transform.localScale = new Vector3(0.6f, 0.02f, 0.08f);
            }
        }
    }
}
