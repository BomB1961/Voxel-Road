using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>월드 생성 규칙 설정.</summary>
    [CreateAssetMenu(fileName = "LaneConfig", menuName = "VoxelRoad/LaneConfig")]
    public sealed class LaneConfigSO : ScriptableObject
    {
        [Header("Dimensions")]
        [Tooltip("레인의 X축 길이 (타일 수). 짝수 권장.")]
        [SerializeField] private int _laneSpanX = 16;

        [Header("Spawn Range")]
        [Tooltip("플레이어 앞쪽으로 유지할 레인 수.")]
        [SerializeField] private int _lookaheadLanes = 14;
        [Tooltip("플레이어 뒤쪽으로 유지할 레인 수.")]
        [SerializeField] private int _lookbehindLanes = 4;
        [Tooltip("게임 시작 시 안전지대 잔디 레인 수.")]
        [SerializeField] private int _safeStartLanes = 6;

        [Header("Lane Prefabs")]
        [SerializeField] private GrassLane _grassLanePrefab;
        [SerializeField] private RoadLane _roadLanePrefab;
        [SerializeField] private RiverLane _riverLanePrefab;

        [Header("Grass Decor")]
        [Tooltip("잔디 레인 데코 프리팹 목록 (Kenney nature-kit).")]
        [SerializeField] private GameObject[] _grassDecorPrefabs;
        [Range(0f, 1f)]
        [Tooltip("타일당 데코 배치 확률.")]
        [SerializeField] private float _grassDecorDensity = 0.35f;

        [Header("Weights")]
        [Range(0f, 1f)]
        [SerializeField] private float _grassWeight = 0.4f;
        [Range(0f, 1f)]
        [SerializeField] private float _roadWeight = 0.35f;
        [Range(0f, 1f)]
        [SerializeField] private float _riverWeight = 0.25f;

        [Header("Road")]
        [Tooltip("같은 유형 레인 최대 연속 반복 수.")]
        [SerializeField] private int _maxSameTypeInARow = 3;

        public int LaneSpanX => _laneSpanX;
        public int LookaheadLanes => _lookaheadLanes;
        public int LookbehindLanes => _lookbehindLanes;
        public int SafeStartLanes => _safeStartLanes;
        public GrassLane GrassLanePrefab => _grassLanePrefab;
        public RoadLane RoadLanePrefab => _roadLanePrefab;
        public RiverLane RiverLanePrefab => _riverLanePrefab;
        public GameObject[] GrassDecorPrefabs => _grassDecorPrefabs;
        public float GrassDecorDensity => _grassDecorDensity;
        public float GrassWeight => _grassWeight;
        public float RoadWeight => _roadWeight;
        public float RiverWeight => _riverWeight;
        public int MaxSameTypeInARow => _maxSameTypeInARow;
    }
}
