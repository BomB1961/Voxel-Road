using UnityEngine;

namespace VoxelRoad.Vehicles
{
    /// <summary>RoadLane에 부착되어 주기적으로 차량을 생성한다.
    /// 레인당 단일 프리팹·단일 속도로 고정해 추월·관통 없이 타입별 속도 차이를 보여준다.</summary>
    public sealed class VehicleSpawner : MonoBehaviour
    {
        private VehicleConfigSO _config;
        private float _direction = 1f;
        private float _laneSpanX;
        private float _nextSpawnTime;
        private float _currentSpeed;
        private Vehicle _lanePrefab;

        public void Initialize(VehicleConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;

            // 레인 고정: 프리팹 하나 선택, 그 프리팹에 대응하는 속도 사용
            if (_config.VehiclePrefabs != null && _config.VehiclePrefabs.Length > 0)
            {
                int idx = Random.Range(0, _config.VehiclePrefabs.Length);
                _lanePrefab = _config.VehiclePrefabs[idx];
                var speeds = _config.VehicleSpeeds;
                _currentSpeed = (speeds != null && idx < speeds.Length && speeds[idx] > 0f)
                    ? speeds[idx]
                    : Random.Range(_config.MinSpeed, _config.MaxSpeed);
            }
            else
            {
                _currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            }

            _nextSpawnTime = Time.time + Random.Range(0f, _config.FirstSpawnDelayMax);
            Prewarm();
        }

        /// <summary>게임 시작 시 레인 전반에 차량을 미리 배치해 즉시 트래픽이 보이게 함.</summary>
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
                Vector3 spawnPos = transform.position + new Vector3(baseX + jitter - transform.localPosition.x, 0f, 0f);
                SpawnAt(spawnPos);
            }
        }

        private void Update()
        {
            if (_config == null || _lanePrefab == null) return;
            if (Time.time < _nextSpawnTime) return;

            float startX = _direction > 0f ? -_laneSpanX * 0.5f - 1.5f : _laneSpanX * 0.5f + 1.5f;
            Vector3 spawnPos = transform.position + new Vector3(startX - transform.position.x, 0f, 0f);
            SpawnAt(spawnPos);

            _nextSpawnTime = Time.time + Random.Range(_config.MinSpawnInterval, _config.MaxSpawnInterval);
        }

        private void SpawnAt(Vector3 worldPos)
        {
            var vehicle = Instantiate(_lanePrefab, worldPos, Quaternion.identity, transform);
            // 레인 폭(1m) 안에 들어오도록 Config 의 균일 스케일 적용
            float s = _config.SpawnScale;
            if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                vehicle.transform.localScale = new Vector3(s, s, s);
            vehicle.Launch(_currentSpeed, _direction, _laneSpanX);
        }
    }
}
