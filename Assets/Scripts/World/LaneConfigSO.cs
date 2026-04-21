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
        [Tooltip("잔디 데코 인스턴스 스케일 배율 (Player 대비 크게 보이도록).")]
        [SerializeField] private float _grassDecorScale = 1.4f;

        [Header("Weights (미사용 — Balance Quota로 대체)")]
        [Range(0f, 1f)]
        [SerializeField] private float _grassWeight = 0.4f;
        [Range(0f, 1f)]
        [SerializeField] private float _roadWeight = 0.35f;
        [Range(0f, 1f)]
        [SerializeField] private float _riverWeight = 0.25f;

        [Header("Balance Quota (덱 1주기당 각 타입 청크 수)")]
        [Tooltip("덱 1사이클에 잔디 청크가 몇 개 포함될지.")]
        [SerializeField] private int _grassQuota = 2;
        [Tooltip("덱 1사이클에 도로 청크가 몇 개 포함될지.")]
        [SerializeField] private int _roadQuota = 2;
        [Tooltip("덱 1사이클에 강 청크가 몇 개 포함될지.")]
        [SerializeField] private int _riverQuota = 2;

        [Header("Chunk Length (per lane type)")]
        [SerializeField] private Vector2Int _grassChunk = new Vector2Int(1, 2);
        [SerializeField] private Vector2Int _roadChunk = new Vector2Int(2, 4);
        [SerializeField] private Vector2Int _riverChunk = new Vector2Int(2, 3);

        public int LaneSpanX => _laneSpanX;
        public int LookaheadLanes => _lookaheadLanes;
        public int LookbehindLanes => _lookbehindLanes;
        public int SafeStartLanes => _safeStartLanes;
        public GrassLane GrassLanePrefab => _grassLanePrefab;
        public RoadLane RoadLanePrefab => _roadLanePrefab;
        public RiverLane RiverLanePrefab => _riverLanePrefab;
        public GameObject[] GrassDecorPrefabs => _grassDecorPrefabs;
        public float GrassDecorDensity => _grassDecorDensity;
        public float GrassDecorScale => _grassDecorScale;
        public float GrassWeight => _grassWeight;
        public float RoadWeight => _roadWeight;
        public float RiverWeight => _riverWeight;
        public int GrassQuota => _grassQuota;
        public int RoadQuota => _roadQuota;
        public int RiverQuota => _riverQuota;

        public int MinChunkLength(LaneType t) => t switch
        {
            LaneType.Road => _roadChunk.x,
            LaneType.River => _riverChunk.x,
            _ => _grassChunk.x,
        };
        public int MaxChunkLength(LaneType t) => t switch
        {
            LaneType.Road => _roadChunk.y,
            LaneType.River => _riverChunk.y,
            _ => _grassChunk.y,
        };
    }
}
