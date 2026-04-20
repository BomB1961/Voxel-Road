using UnityEngine;

namespace VoxelRoad.River
{
    /// <summary>RiverLane 자식. 주기적으로 Log 생성, 레인 경계 밖에서 스폰.</summary>
    public sealed class LogSpawner : MonoBehaviour
    {
        private LogConfigSO _config;
        private float _direction;
        private float _laneSpanX;
        private float _nextSpawnTime;
        private bool _initialized;

        public void Initialize(LogConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;
            _nextSpawnTime = Time.time + Random.Range(0f, config.FirstSpawnDelayMax);
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _config == null) return;
            if (Time.time < _nextSpawnTime) return;

            var prefabs = _config.LogPrefabs;
            if (prefabs == null || prefabs.Length == 0) return;

            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) return;

            float startX = _direction > 0f ? -_laneSpanX * 0.5f - 1.5f : _laneSpanX * 0.5f + 1.5f;
            var logGO = Instantiate(prefab, transform);
            logGO.transform.localPosition = new Vector3(startX, 0f, 0f);
            float s = _config.SpawnScale;
            if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                logGO.transform.localScale = new Vector3(s, s, s);
            float speed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            var logComp = logGO.GetComponent<Log>();
            if (logComp != null) logComp.Launch(speed, _direction, _laneSpanX);

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }
    }
}
