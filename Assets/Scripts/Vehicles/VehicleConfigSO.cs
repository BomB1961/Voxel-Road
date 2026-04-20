using UnityEngine;

namespace VoxelRoad.Vehicles
{
    /// <summary>차량 생성 규칙.</summary>
    [CreateAssetMenu(fileName = "VehicleConfig", menuName = "VoxelRoad/VehicleConfig")]
    public sealed class VehicleConfigSO : ScriptableObject
    {
        [SerializeField] private Vehicle[] _vehiclePrefabs;
        [SerializeField] private float _minSpeed = 2.5f;
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _minSpawnInterval = 1.2f;
        [SerializeField] private float _maxSpawnInterval = 2.8f;
        [SerializeField] private float _firstSpawnDelayMax = 2f;

        public Vehicle[] VehiclePrefabs => _vehiclePrefabs;
        public float MinSpeed => _minSpeed;
        public float MaxSpeed => _maxSpeed;
        public float MinSpawnInterval => _minSpawnInterval;
        public float MaxSpawnInterval => _maxSpawnInterval;
        public float FirstSpawnDelayMax => _firstSpawnDelayMax;
    }
}
