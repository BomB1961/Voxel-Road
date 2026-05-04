using UnityEngine;
using VoxelRoad.Common;

namespace VoxelRoad.River
{
    /// <summary>RiverLane 자식. 주기적으로 Log 생성, 레인 경계 밖에서 스폰.
    /// 레인 속도만 고정(추월·관통 방지)하고 스폰마다 다른 프리팹을 뽑아 다양성을 확보한다.</summary>
    public sealed class LogSpawner : PeriodicLaneSpawnerBase<LogConfigSO>
    {
        protected override bool HasPrefabs => _config.LogPrefabs != null && _config.LogPrefabs.Length > 0;
        protected override int PrewarmCount => 8;
        protected override float FirstSpawnDelayMax => _config.FirstSpawnDelayMax;
        protected override (float min, float max) SpawnIntervalRange => (_config.MinSpawnInterval, _config.MaxSpawnInterval);

        protected override float ResolveBaseSpeed()
        {
            var speeds = _config.LogSpeeds;
            if (speeds != null && speeds.Length > 0)
            {
                float s = speeds[Random.Range(0, speeds.Length)];
                return s > 0f ? s : Random.Range(_config.MinSpeed, _config.MaxSpeed);
            }
            return Random.Range(_config.MinSpeed, _config.MaxSpeed);
        }

        protected override void SpawnAt(float laneRelativeX)
        {
            var prefabs = _config.LogPrefabs;
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) return;
            var logGO = Instantiate(prefab, transform);
            logGO.transform.localPosition = new Vector3(laneRelativeX, 0f, 0f);
            float s = _config.SpawnScale;
            float lx = s * _config.LengthScale;
            float lz = s * _config.WidthScale;
            logGO.transform.localScale = new Vector3(lx, s, lz);
            var logComp = logGO.GetComponent<Log>();
            if (logComp != null) logComp.Launch(_currentSpeed, _direction, _laneSpanX);
        }
    }
}
