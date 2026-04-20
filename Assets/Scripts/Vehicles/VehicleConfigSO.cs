using UnityEngine;

namespace VoxelRoad.Vehicles
{
    /// <summary>차량 생성 규칙.</summary>
    [CreateAssetMenu(fileName = "VehicleConfig", menuName = "VoxelRoad/VehicleConfig")]
    public sealed class VehicleConfigSO : ScriptableObject
    {
        [SerializeField] private Vehicle[] _vehiclePrefabs;
        [Tooltip("각 프리팹에 대응하는 주행 속도(m/s). 배열 길이가 맞으면 해당 값 사용, 아니면 Min/MaxSpeed 랜덤.")]
        [SerializeField] private float[] _vehicleSpeeds;
        [SerializeField] private float _minSpeed = 2.5f;
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _minSpawnInterval = 1.2f;
        [SerializeField] private float _maxSpawnInterval = 2.8f;
        [SerializeField] private float _firstSpawnDelayMax = 2f;

        [Header("Scale")]
        [Tooltip("스폰 시 차량에 적용할 균일 스케일. 1m 레인 폭 기준 약 0.6 권장 (원본 차폭 1.5m → 0.9m).")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _spawnScale = 0.6f;

        public Vehicle[] VehiclePrefabs => _vehiclePrefabs;
        public float[] VehicleSpeeds => _vehicleSpeeds;
        public float MinSpeed => _minSpeed;
        public float MaxSpeed => _maxSpeed;
        public float MinSpawnInterval => _minSpawnInterval;
        public float MaxSpawnInterval => _maxSpawnInterval;
        public float FirstSpawnDelayMax => _firstSpawnDelayMax;
        public float SpawnScale => _spawnScale;
    }
}
