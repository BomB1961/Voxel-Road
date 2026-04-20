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
            vehicle.Launch(_currentSpeed, _direction, _laneSpanX);

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }
    }
}
