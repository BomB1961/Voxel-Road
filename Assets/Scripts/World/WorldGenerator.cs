using System.Collections.Generic;
using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>플레이어 전진에 따라 청크 단위로 레인을 스폰/디스폰.</summary>
    public sealed class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private LaneConfigSO _config;
        [SerializeField] private Transform _player;

        private readonly Dictionary<int, BaseLane> _lanes = new();
        private int _furthestSpawnedZ = -1;
        private LaneType _lastChunkType;

        public static WorldGenerator Instance { get; private set; }

        /// <summary>레인 가로 폭의 절반(셀 기준). 플레이어 좌우 경계 판정에 사용.</summary>
        public int LaneHalfSpan => _config != null ? Mathf.RoundToInt(_config.LaneSpanX / 2f) : 25;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            if (_config == null || _player == null)
            {
                Debug.LogError("[WorldGenerator] config/player 미지정");
                enabled = false;
                return;
            }

            int start = -_config.LookbehindLanes;
            int safeEnd = _config.SafeStartLanes;
            for (int z = start; z < safeEnd; z++)
                SpawnLane(z, LaneType.Grass);
            _furthestSpawnedZ = Mathf.Max(_furthestSpawnedZ, safeEnd - 1);
            _lastChunkType = LaneType.Grass;

            int target = safeEnd + _config.LookaheadLanes;
            while (_furthestSpawnedZ < target) SpawnNextChunk();
        }

        private void Update()
        {
            int playerZ = Mathf.RoundToInt(_player.position.z);
            int desiredFront = playerZ + _config.LookaheadLanes;
            while (_furthestSpawnedZ < desiredFront) SpawnNextChunk();

            int despawnBehind = playerZ - _config.LookbehindLanes;
            var toRemove = new List<int>();
            foreach (var kv in _lanes)
                if (kv.Key < despawnBehind) toRemove.Add(kv.Key);
            foreach (var z in toRemove) { _lanes[z].Despawn(); _lanes.Remove(z); }
        }

        public LaneType GetLaneTypeAt(int zIndex)
            => _lanes.TryGetValue(zIndex, out var lane) ? lane.Type : LaneType.Grass;

        /// <summary>해당 셀이 통행 불가(데코 점유 등)이면 true.</summary>
        public bool IsCellBlocked(int x, int zIndex)
            => _lanes.TryGetValue(zIndex, out var lane) && lane.IsBlockedAt(x);

        private void SpawnNextChunk()
        {
            LaneType type = ChooseNextChunkType();
            int length = Random.Range(_config.MinChunkLength(type), _config.MaxChunkLength(type) + 1);
            for (int i = 0; i < length; i++)
                SpawnLane(_furthestSpawnedZ + 1, type);
            _lastChunkType = type;
        }

        private LaneType ChooseNextChunkType()
        {
            // 직전 청크와 다른 타입으로 강제 → 자연스러운 패턴 변화
            float gw = _config.GrassWeight;
            float rw = _config.RoadWeight;
            float vw = _config.RiverWeight;
            // 직전 타입 가중치 제거
            if (_lastChunkType == LaneType.Grass) gw = 0f;
            else if (_lastChunkType == LaneType.Road) rw = 0f;
            else if (_lastChunkType == LaneType.River) vw = 0f;

            float total = gw + rw + vw;
            if (total <= 0f) return LaneType.Grass;
            float roll = Random.value * total;
            if (roll < gw) return LaneType.Grass;
            if (roll < gw + rw) return LaneType.Road;
            return LaneType.River;
        }

        private void SpawnLane(int zIndex, LaneType type)
        {
            BaseLane lane = null;
            if (type == LaneType.Grass)
            {
                var prefab = _config.GrassLanePrefab;
                if (prefab == null) { Debug.LogError("[WorldGenerator] GrassLane prefab 없음"); return; }
                var instance = Instantiate(prefab, transform);
                instance.SetConfig(_config);
                lane = instance;
            }
            else if (type == LaneType.Road)
            {
                var prefab = _config.RoadLanePrefab;
                if (prefab == null) { Debug.LogError("[WorldGenerator] RoadLane prefab 없음"); return; }
                lane = Instantiate(prefab, transform);
            }
            else if (type == LaneType.River)
            {
                var prefab = _config.RiverLanePrefab;
                if (prefab == null)
                {
                    var gp = _config.GrassLanePrefab;
                    var inst = Instantiate(gp, transform);
                    inst.SetConfig(_config);
                    lane = inst;
                }
                else
                {
                    lane = Instantiate(prefab, transform);
                }
            }

            lane.Initialize(zIndex, _config.LaneSpanX);
            _lanes[zIndex] = lane;
            _furthestSpawnedZ = Mathf.Max(_furthestSpawnedZ, zIndex);
        }
    }
}
