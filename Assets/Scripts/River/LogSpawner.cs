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
        private float _currentSpeed;
        private bool _initialized;

        public void Initialize(LogConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;
            // 한 강 레인 내 모든 통나무는 동일 속도 → 추월·관통 현상 방지
            _currentSpeed = Random.Range(config.MinSpeed, config.MaxSpeed);
            _nextSpawnTime = Time.time + Random.Range(0f, config.FirstSpawnDelayMax);
            _initialized = true;
            Prewarm();
        }

        /// <summary>게임 시작 시 레인 전반에 통나무를 미리 배치해 즉시 탑승 가능하게 함.</summary>
        private void Prewarm()
        {
            var prefabs = _config.LogPrefabs;
            if (prefabs == null || prefabs.Length == 0) return;
            const int Count = 3;
            float halfSpan = _laneSpanX * 0.5f;
            float spacing = _laneSpanX / Count;
            for (int i = 0; i < Count; i++)
            {
                var prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null) continue;
                float baseX = -halfSpan + spacing * (i + 0.5f);
                float jitter = Random.Range(-spacing * 0.25f, spacing * 0.25f);
                var logGO = Instantiate(prefab, transform);
                logGO.transform.localPosition = new Vector3(baseX + jitter, 0f, 0f);
                float s = _config.SpawnScale;
                if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                    logGO.transform.localScale = new Vector3(s, s, s);
                var logComp = logGO.GetComponent<Log>();
                if (logComp != null) logComp.Launch(_currentSpeed, _direction, _laneSpanX);
            }
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
            var logComp = logGO.GetComponent<Log>();
            if (logComp != null) logComp.Launch(_currentSpeed, _direction, _laneSpanX);

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }
    }
}
