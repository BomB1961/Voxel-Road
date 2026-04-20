using UnityEngine;

namespace VoxelRoad.River
{
    /// <summary>RiverLane 자식. 주기적으로 Log 생성, 레인 경계 밖에서 스폰.
    /// 레인당 단일 프리팹·단일 속도로 고정해 같은 레인 내 추월 없이 타입별 속도 차이를 보여준다.</summary>
    public sealed class LogSpawner : MonoBehaviour
    {
        private LogConfigSO _config;
        private float _direction;
        private float _laneSpanX;
        private float _nextSpawnTime;
        private float _currentSpeed;
        private GameObject _lanePrefab;
        private bool _initialized;

        public void Initialize(LogConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;

            var prefabs = config.LogPrefabs;
            if (prefabs != null && prefabs.Length > 0)
            {
                int idx = Random.Range(0, prefabs.Length);
                _lanePrefab = prefabs[idx];
                var speeds = config.LogSpeeds;
                _currentSpeed = (speeds != null && idx < speeds.Length && speeds[idx] > 0f)
                    ? speeds[idx]
                    : Random.Range(config.MinSpeed, config.MaxSpeed);
            }
            else
            {
                _currentSpeed = Random.Range(config.MinSpeed, config.MaxSpeed);
            }

            _nextSpawnTime = Time.time + Random.Range(0f, config.FirstSpawnDelayMax);
            _initialized = true;
            Prewarm();
        }

        /// <summary>게임 시작 시 레인 전반에 통나무를 미리 배치해 즉시 탑승 가능하게 함.</summary>
        private void Prewarm()
        {
            if (_lanePrefab == null) return;
            const int Count = 3;
            float halfSpan = _laneSpanX * 0.5f;
            float spacing = _laneSpanX / Count;
            for (int i = 0; i < Count; i++)
            {
                float baseX = -halfSpan + spacing * (i + 0.5f);
                float jitter = Random.Range(-spacing * 0.25f, spacing * 0.25f);
                SpawnAt(new Vector3(baseX + jitter, 0f, 0f));
            }
        }

        private void Update()
        {
            if (!_initialized || _config == null || _lanePrefab == null) return;
            if (Time.time < _nextSpawnTime) return;

            float startX = _direction > 0f ? -_laneSpanX * 0.5f - 1.5f : _laneSpanX * 0.5f + 1.5f;
            SpawnAt(new Vector3(startX, 0f, 0f));

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }

        private void SpawnAt(Vector3 localPos)
        {
            var logGO = Instantiate(_lanePrefab, transform);
            logGO.transform.localPosition = localPos;
            float s = _config.SpawnScale;
            if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                logGO.transform.localScale = new Vector3(s, s, s);
            var logComp = logGO.GetComponent<Log>();
            if (logComp != null) logComp.Launch(_currentSpeed, _direction, _laneSpanX);
        }
    }
}
