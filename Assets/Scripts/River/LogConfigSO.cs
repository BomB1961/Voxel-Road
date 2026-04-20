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

        [Header("Scale")]
        [Tooltip("스폰 시 통나무에 적용할 균일 스케일. 레인 폭 1m 기준 약 0.85 권장.")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _spawnScale = 0.85f;

        public GameObject[] LogPrefabs => _logPrefabs;
        public float MinSpeed => _minSpeed;
        public float MaxSpeed => _maxSpeed;
        public float MinSpawnInterval => _minSpawnInterval;
        public float MaxSpawnInterval => _maxSpawnInterval;
        public float FirstSpawnDelayMax => _firstSpawnDelayMax;
        public float SpawnScale => _spawnScale;
    }
}
