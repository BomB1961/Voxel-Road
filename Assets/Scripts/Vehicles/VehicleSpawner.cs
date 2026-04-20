using UnityEngine;

namespace VoxelRoad.Vehicles
{
    /// <summary>RoadLane에 부착되어 주기적으로 차량을 생성한다.
    /// 레인 속도는 고정(추월·관통 방지)하되, 스폰마다 다른 프리팹을 뽑아 시각적 다양성을 확보한다.</summary>
    public sealed class VehicleSpawner : MonoBehaviour
    {
        private VehicleConfigSO _config;
        private float _direction = 1f;
        private float _laneSpanX;
        private float _nextSpawnTime;
        private float _currentSpeed;
        private bool _hasPrefabs;

        public void Initialize(VehicleConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;

            _hasPrefabs = _config.VehiclePrefabs != null && _config.VehiclePrefabs.Length > 0;

            // 레인 속도만 고정: speeds 배열이 있으면 그중 랜덤, 아니면 Min/Max 랜덤
            var speeds = _config.VehicleSpeeds;
            if (speeds != null && speeds.Length > 0)
            {
                float s = speeds[Random.Range(0, speeds.Length)];
                _currentSpeed = s > 0f ? s : Random.Range(_config.MinSpeed, _config.MaxSpeed);
            }
            else
            {
                _currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            }

            _nextSpawnTime = Time.time + Random.Range(0f, _config.FirstSpawnDelayMax);
            Prewarm();
        }

        private Vehicle PickPrefab()
        {
            if (!_hasPrefabs) return null;
            var prefabs = _config.VehiclePrefabs;
            return prefabs[Random.Range(0, prefabs.Length)];
        }

        /// <summary>게임 시작 시 레인 전반에 차량을 미리 배치해 즉시 트래픽이 보이게 함.</summary>
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
                Vector3 spawnPos = transform.position + new Vector3(baseX + jitter - transform.localPosition.x, 0f, 0f);
                SpawnAt(spawnPos);
            }
        }

        private void Update()
        {
            if (_config == null || !_hasPrefabs) return;
            if (Time.time < _nextSpawnTime) return;

            float startX = _direction > 0f ? -_laneSpanX * 0.5f - 1.5f : _laneSpanX * 0.5f + 1.5f;
            Vector3 spawnPos = transform.position + new Vector3(startX - transform.position.x, 0f, 0f);
            SpawnAt(spawnPos);

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }

        private void SpawnAt(Vector3 worldPos)
        {
            var prefab = PickPrefab();
            if (prefab == null) return;
            var vehicle = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            // 레인 폭(1m) 안에 들어오도록 Config 의 균일 스케일 적용
            float s = _config.SpawnScale;
            if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                vehicle.transform.localScale = new Vector3(s, s, s);
            vehicle.Launch(_currentSpeed, _direction, _laneSpanX);
        }
    }
}
