using UnityEngine;
using VoxelRoad.Common;

namespace VoxelRoad.Vehicles
{
    /// <summary>RoadLane에 부착되어 주기적으로 차량을 생성한다.
    /// 레인 속도는 고정(추월·관통 방지)하되, 스폰마다 다른 프리팹을 뽑아 시각적 다양성을 확보한다.</summary>
    public sealed class VehicleSpawner : PeriodicLaneSpawnerBase<VehicleConfigSO>
    {
        protected override bool HasPrefabs => _config.VehiclePrefabs != null && _config.VehiclePrefabs.Length > 0;
        protected override int PrewarmCount => 6;
        protected override float FirstSpawnDelayMax => _config.FirstSpawnDelayMax;
        protected override (float min, float max) SpawnIntervalRange => (_config.MinSpawnInterval, _config.MaxSpawnInterval);

        protected override float ResolveBaseSpeed()
        {
            // 레인 속도만 고정: speeds 배열이 있으면 그중 랜덤, 아니면 Min/Max 랜덤.
            var speeds = _config.VehicleSpeeds;
            if (speeds != null && speeds.Length > 0)
            {
                float s = speeds[Random.Range(0, speeds.Length)];
                return s > 0f ? s : Random.Range(_config.MinSpeed, _config.MaxSpeed);
            }
            return Random.Range(_config.MinSpeed, _config.MaxSpeed);
        }

        protected override void SpawnAt(float laneRelativeX)
        {
            var prefabs = _config.VehiclePrefabs;
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) return;
            // VehicleSpawner 가 lane 자식이 아닌 위치에 있을 가능성을 감안한 worldPos 보정 유지.
            Vector3 worldPos = transform.position + new Vector3(laneRelativeX - transform.localPosition.x, 0f, 0f);
            var vehicle = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            // 레인 폭(1m) 안에 들어오도록 Config 의 균일 스케일 적용.
            float s = _config.SpawnScale;
            if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                vehicle.transform.localScale = new Vector3(s, s, s);
            vehicle.Launch(_currentSpeed, _direction, _laneSpanX);
        }
    }
}
