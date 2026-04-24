using UnityEngine;

namespace VoxelRoad.Rail
{
    /// <summary>철길 기차 스폰·속도·경고 시간 설정.</summary>
    [CreateAssetMenu(fileName = "TrainConfig", menuName = "VoxelRoad/TrainConfig")]
    public sealed class TrainConfigSO : ScriptableObject
    {
        [Header("Train Prefabs")]
        [SerializeField] private GameObject[] _trainPrefabs;

        [Header("Speed (m/s)")]
        [SerializeField] private float _speed = 15f;

        [Header("Warning (초)")]
        [Tooltip("경고 시작부터 기차 돌진까지 시간.")]
        [SerializeField] private float _warningSeconds = 1.5f;

        [Header("Cycle Interval (초)")]
        [SerializeField] private float _minCycleInterval = 4f;
        [SerializeField] private float _maxCycleInterval = 8f;

        [Header("Scale")]
        [Tooltip("기차 프리팹 전체 스케일 배율.")]
        [SerializeField] private float _spawnScale = 1f;
        [Tooltip("기차 길이 배율 (로컬 X). 최종 월드 길이 = 프리팹 X × spawnScale × lengthScale.")]
        [SerializeField] private float _lengthScale = 4f;

        [Header("Cars (연결 차량)")]
        [Tooltip("한 기차당 연결 차량 수. 1이면 단일 차량.")]
        [SerializeField] private int _carsPerTrain = 3;
        [Tooltip("차량 사이 간격 (월드 단위). 0이면 완전히 붙음.")]
        [SerializeField] private float _carSpacing = 0.05f;

        public GameObject[] TrainPrefabs => _trainPrefabs;
        public float Speed => _speed;
        public float WarningSeconds => _warningSeconds;
        public float MinCycleInterval => _minCycleInterval;
        public float MaxCycleInterval => _maxCycleInterval;
        public float SpawnScale => _spawnScale;
        public float LengthScale => _lengthScale;
        public int CarsPerTrain => Mathf.Max(1, _carsPerTrain);
        public float CarSpacing => Mathf.Max(0f, _carSpacing);
    }
}
