using UnityEngine;

namespace VoxelRoad.Vehicles
{
    /// <summary>RoadLane에 부착되어 주기적으로 차량을 생성한다.</summary>
    public sealed class VehicleSpawner : MonoBehaviour
    {
        private VehicleConfigSO _config;
        private float _direction = 1f;
        private float _laneSpanX;
        private float _nextSpawnTime;
        private float _currentSpeed;

        public void Initialize(VehicleConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;
            _currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            _nextSpawnTime = Time.time + Random.Range(0f, _config.FirstSpawnDelayMax);
            Prewarm();
        }

        /// <summary>게임 시작 시 레인 전반에 차량을 미리 배치해 즉시 트래픽이 보이게 함.</summary>
        private void Prewarm()
        {
            if (_config.VehiclePrefabs == null || _config.VehiclePrefabs.Length == 0) return;
            const int Count = 3;
            float halfSpan = _laneSpanX * 0.5f;
            float spacing = _laneSpanX / Count;
            for (int i = 0; i < Count; i++)
            {
                var prefab = _config.VehiclePrefabs[Random.Range(0, _config.VehiclePrefabs.Length)];
                if (prefab == null) continue;
                float baseX = -halfSpan + spacing * (i + 0.5f);
                float jitter = Random.Range(-spacing * 0.25f, spacing * 0.25f);
                Vector3 spawnPos = transform.position + new Vector3(baseX + jitter - transform.localPosition.x, 0f, 0f);
                var v = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                float s = _config.SpawnScale;
                if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                    v.transform.localScale = new Vector3(s, s, s);
                v.Launch(_currentSpeed, _direction, _laneSpanX);
            }
        }

        private void Update()
        {
            if (_config == null || _config.VehiclePrefabs == null || _config.VehiclePrefabs.Length == 0) return;
            if (Time.time < _nextSpawnTime) return;

            var prefab = _config.VehiclePrefabs[Random.Range(0, _config.VehiclePrefabs.Length)];
            if (prefab == null) return;

            float startX = _direction > 0f ? -_laneSpanX * 0.5f - 1.5f : _laneSpanX * 0.5f + 1.5f;
            Vector3 spawnPos = transform.position + new Vector3(startX - transform.position.x, 0f, 0f);
            var vehicle = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            // 레인 폭(1m) 안에 들어오도록 Config 의 균일 스케일 적용
            float s = _config.SpawnScale;
            if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                vehicle.transform.localScale = new Vector3(s, s, s);
            vehicle.Launch(_currentSpeed, _direction, _laneSpanX);

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }
    }
}
