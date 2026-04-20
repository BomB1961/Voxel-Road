using UnityEngine;

namespace VoxelRoad.River
{
    /// <summary>RiverLane 자식. 주기적으로 Log 생성, 레인 경계 밖에서 스폰.
    /// 레인 속도만 고정(추월·관통 방지)하고 스폰마다 다른 프리팹을 뽑아 다양성을 확보한다.</summary>
    public sealed class LogSpawner : MonoBehaviour
    {
        private LogConfigSO _config;
        private float _direction;
        private float _laneSpanX;
        private float _nextSpawnTime;
        private float _currentSpeed;
        private bool _hasPrefabs;
        private bool _initialized;

        public void Initialize(LogConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;

            _hasPrefabs = config.LogPrefabs != null && config.LogPrefabs.Length > 0;
            var speeds = config.LogSpeeds;
            if (speeds != null && speeds.Length > 0)
            {
                float s = speeds[Random.Range(0, speeds.Length)];
                _currentSpeed = s > 0f ? s : Random.Range(config.MinSpeed, config.MaxSpeed);
            }
            else
            {
                _currentSpeed = Random.Range(config.MinSpeed, config.MaxSpeed);
            }

            _nextSpawnTime = Time.time + Random.Range(0f, config.FirstSpawnDelayMax);
            _initialized = true;
            Prewarm();
        }

        private GameObject PickPrefab()
        {
            if (!_hasPrefabs) return null;
            var prefabs = _config.LogPrefabs;
            return prefabs[Random.Range(0, prefabs.Length)];
        }

        /// <summary>게임 시작 시 레인 전반에 통나무를 미리 배치해 즉시 탑승 가능하게 함.</summary>
        private void Prewarm()
        {
            if (!_hasPrefabs) return;
            const int Count = 6;
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
            if (!_initialized || _config == null || !_hasPrefabs) return;
            if (Time.time < _nextSpawnTime) return;

            float startX = _direction > 0f ? -_laneSpanX * 0.5f - 1.5f : _laneSpanX * 0.5f + 1.5f;
            SpawnAt(new Vector3(startX, 0f, 0f));

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }

        private void SpawnAt(Vector3 localPos)
        {
            var prefab = PickPrefab();
            if (prefab == null) return;
            var logGO = Instantiate(prefab, transform);
            logGO.transform.localPosition = localPos;
            float s = _config.SpawnScale;
            if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                logGO.transform.localScale = new Vector3(s, s, s);
            var logComp = logGO.GetComponent<Log>();
            if (logComp != null) logComp.Launch(_currentSpeed, _direction, _laneSpanX);
        }
    }
}
