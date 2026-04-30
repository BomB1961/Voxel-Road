using UnityEngine;
using VoxelRoad.River;

namespace VoxelRoad.World
{
    /// <summary>강 레인. 파란 물 바닥 + 가장자리 연잎 데코 + 통나무 스포너 + 익사 판정용 물 영역.</summary>
    public sealed class RiverLane : BaseLane
    {
        [SerializeField] private MeshRenderer _water;
        [SerializeField] private LogSpawner _spawner;
        [SerializeField] private LogConfigSO _logConfig;

        [Header("Decor")]
        [SerializeField] private Material _lilyPadMaterial;
        [Range(0f, 1f)]
        [SerializeField] private float _lilyPadDensity = 0.25f;

        public override LaneType Type => LaneType.River;

        protected override void Build()
        {
            // 레인 타입별 Y 오프셋 (Grass=-0.02, Road=0, River=-0.01)로 Z-fighting 방지.
            if (_water != null)
            {
                _water.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
                var lp = _water.transform.localPosition;
                _water.transform.localPosition = new Vector3(lp.x, -0.01f, lp.z);
            }

            BuildLilyPads();

            if (_spawner != null && _logConfig != null)
            {
                float dir = (_zIndex % 2 == 0) ? 1f : -1f;
                _spawner.Initialize(_logConfig, dir, _laneSpanX, _difficultyMultiplier);
            }
        }

        private void BuildLilyPads()
        {
            if (_lilyPadMaterial == null) return;
            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            // 레인 Z 범위 ±0.5. Log 콜라이더 Z 너비 0.26 → ±0.13.
            // 연잎 중심 z=±0.32 + Z 외연 ±0.13 = [±0.19, ±0.45] → 인접 강 레인 연잎과 가시 간격 0.10m,
            // 통나무 경로(±0.13)와 0.06m 분리. 회전은 사용하지 않음(90°/270° 회전 시 X 스케일이 Z로 돌아 경계 초과).
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                for (int side = 0; side < 2; side++)
                {
                    if (Random.value > _lilyPadDensity) continue;
                    float z = side == 0 ? -0.32f : 0.32f;
                    var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pad.name = "LilyPad";
                    pad.transform.SetParent(transform, false);
                    Destroy(pad.GetComponent<Collider>());
                    pad.GetComponent<MeshRenderer>().sharedMaterial = _lilyPadMaterial;
                    // 수면(y=-0.01) 위 살짝. 디딤돌 오인 방지 위해 매우 얇고 작게.
                    pad.transform.localPosition = new Vector3(x, 0.005f, z);
                    pad.transform.localScale = new Vector3(0.34f, 0.02f, 0.26f);
                }
            }
        }
    }
}
