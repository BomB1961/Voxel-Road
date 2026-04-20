using UnityEngine;

namespace VoxelRoad.River
{
    /// <summary>강 위 통나무 스폰 파라미터.</summary>
    [CreateAssetMenu(fileName = "LogConfig", menuName = "VoxelRoad/Log Config")]
    public sealed class LogConfigSO : ScriptableObject
    {
        [SerializeField] private GameObject[] _logPrefabs;
        [SerializeField] private float _minSpeed = 1.5f;
        [SerializeField] private float _maxSpeed = 3f;
        [SerializeField] private float _minSpawnInterval = 2f;
        [SerializeField] private float _maxSpawnInterval = 3.5f;
        [SerializeField] private float _firstSpawnDelayMax = 2f;

        public GameObject[] LogPrefabs => _logPrefabs;
        public float MinSpeed => _minSpeed;
        public float MaxSpeed => _maxSpeed;
        public float MinSpawnInterval => _minSpawnInterval;
        public float MaxSpawnInterval => _maxSpawnInterval;
        public float FirstSpawnDelayMax => _firstSpawnDelayMax;
    }
}
