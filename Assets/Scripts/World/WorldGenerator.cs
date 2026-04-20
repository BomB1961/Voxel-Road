using System.Collections.Generic;
using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>플레이어 전진에 따라 레인을 앞으로 스폰하고 뒤쪽을 정리한다.</summary>
    public sealed class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private LaneConfigSO _config;
        [SerializeField] private Transform _player;

        private readonly Dictionary<int, BaseLane> _lanes = new();
        private int _furthestSpawnedZ = -1;
        private LaneType _lastType;
        private int _sameTypeStreak;

        public static WorldGenerator Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

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
            {
                SpawnLane(z, LaneType.Grass);
            }
            for (int z = safeEnd; z < safeEnd + _config.LookaheadLanes; z++)
            {
                SpawnLane(z, ChooseNextType());
            }
        }

        private void Update()
        {
            int playerZ = Mathf.RoundToInt(_player.position.z);
            int desiredFront = playerZ + _config.LookaheadLanes;
            while (_furthestSpawnedZ < desiredFront)
            {
                SpawnLane(_furthestSpawnedZ + 1, ChooseNextType());
            }

            int despawnBehind = playerZ - _config.LookbehindLanes;
            var toRemove = new List<int>();
            foreach (var kv in _lanes)
            {
                if (kv.Key < despawnBehind) toRemove.Add(kv.Key);
            }
            foreach (var z in toRemove)
            {
                _lanes[z].Despawn();
                _lanes.Remove(z);
            }
        }

        /// <summary>해당 z 인덱스 레인의 타입을 반환. 존재하지 않으면 Grass로 간주.</summary>
        public LaneType GetLaneTypeAt(int zIndex)
        {
            return _lanes.TryGetValue(zIndex, out var lane) ? lane.Type : LaneType.Grass;
        }

        private LaneType ChooseNextType()
        {
            float gw = _config.GrassWeight;
            float rw = _config.RoadWeight;
            float vw = _config.RiverWeight;
            float total = gw + rw + vw;
            float roll = Random.value * total;
            LaneType candidate;
            if (roll < gw) candidate = LaneType.Grass;
            else if (roll < gw + rw) candidate = LaneType.Road;
            else candidate = LaneType.River;

            if (candidate == _lastType && _sameTypeStreak >= _config.MaxSameTypeInARow)
            {
                // 연속 제한 돌파 시 나머지 중 가중치 큰 쪽으로 회전
                if (candidate == LaneType.Grass) candidate = rw >= vw ? LaneType.Road : LaneType.River;
                else if (candidate == LaneType.Road) candidate = gw >= vw ? LaneType.Grass : LaneType.River;
                else candidate = gw >= rw ? LaneType.Grass : LaneType.Road;
            }
            return candidate;
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
                    Debug.LogWarning("[WorldGenerator] RiverLane prefab 미지정 — Grass 대체");
                    var gp = _config.GrassLanePrefab;
                    var inst = Instantiate(gp, transform);
                    inst.SetConfig(_config);
                    lane = inst;
                    type = LaneType.Grass;
                }
                else
                {
                    lane = Instantiate(prefab, transform);
                }
            }
            else
            {
                Debug.LogWarning("[WorldGenerator] 미구현 레인 유형 — Grass로 대체");
                var gp = _config.GrassLanePrefab;
                var inst = Instantiate(gp, transform);
                inst.SetConfig(_config);
                lane = inst;
                type = LaneType.Grass;
            }

            lane.Initialize(zIndex, _config.LaneSpanX);
            _lanes[zIndex] = lane;
            _furthestSpawnedZ = Mathf.Max(_furthestSpawnedZ, zIndex);

            _sameTypeStreak = (type == _lastType) ? _sameTypeStreak + 1 : 1;
            _lastType = type;
        }
    }
}
