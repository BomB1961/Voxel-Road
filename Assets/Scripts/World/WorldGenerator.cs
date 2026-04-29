using System.Collections.Generic;
using UnityEngine;
using VoxelRoad.Common;
using VoxelRoad.Game;

namespace VoxelRoad.World
{
    /// <summary>플레이어 전진에 따라 청크 단위로 레인을 스폰/디스폰.</summary>
    public sealed class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private LaneConfigSO _config;
        [SerializeField] private Transform _player;

        private PlayerController _playerController;
        private readonly Dictionary<int, BaseLane> _lanes = new();
        private readonly List<LaneType> _deck = new();
        private int _furthestSpawnedZ = -1;
        private LaneType _lastChunkType;
        private readonly List<int> _despawnBuffer = new();

        /// <summary>레인 가로 폭의 절반(셀 기준). 플레이어 좌우 경계 판정에 사용.</summary>
        public int LaneHalfSpan => _config != null ? Mathf.RoundToInt(_config.LaneSpanX / 2f) : 25;

        private void Start()
        {
            if (_config == null || _player == null)
            {
                Debug.LogError("[WorldGenerator] config/player 미지정");
                enabled = false;
                return;
            }

            // PlayerController 캐싱: BalanceCurve가 MaxZ 참조 (거리 기반 base multiplier)
            _playerController = _player.GetComponent<PlayerController>();

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

            // 매 프레임 GC 할당 방지: 재사용 리스트에 후보 수집 후 일괄 제거
            int despawnBehind = playerZ - _config.LookbehindLanes;
            _despawnBuffer.Clear();
            foreach (var kv in _lanes)
                if (kv.Key < despawnBehind) _despawnBuffer.Add(kv.Key);
            for (int i = 0; i < _despawnBuffer.Count; i++)
            {
                int z = _despawnBuffer[i];
                _lanes[z].Despawn();
                _lanes.Remove(z);
            }
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
            // 청크 단위 난이도 추첨 + 거리 base 곱 → 청크 내 모든 레인이 같은 multiplier 공유.
            // 같은 청크 안에서는 일관성 유지(예: 도로 청크 4 lane 전체가 Hard 속도).
            int currentMaxZ = _playerController != null ? _playerController.MaxZ : 0;
            ChunkDifficulty diff = BalanceCurve.PickRandomDifficulty();
            float multiplier = (type == LaneType.Grass)
                ? 1f
                : BalanceCurve.Combined(currentMaxZ, diff);
            for (int i = 0; i < length; i++)
                SpawnLane(_furthestSpawnedZ + 1, type, i == 0, i == length - 1, multiplier);
            _lastChunkType = type;
        }

        /// <summary>덱이 비어 있으면 쿼터 기반으로 채우고 섞는다.</summary>
        private void RefillDeck()
        {
            _deck.Clear();
            for (int i = 0; i < _config.GrassQuota; i++) _deck.Add(LaneType.Grass);
            for (int i = 0; i < _config.RoadQuota; i++) _deck.Add(LaneType.Road);
            for (int i = 0; i < _config.RiverQuota; i++) _deck.Add(LaneType.River);
            for (int i = 0; i < _config.RailQuota; i++) _deck.Add(LaneType.Rail);
            // Fisher-Yates 셔플
            for (int i = _deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
            }
        }

        private LaneType ChooseNextChunkType()
        {
            // 테스트 모드: 청크 강제 Grass.
            if (TestMode.ForceGrassOnly) return LaneType.Grass;
            // 직전 타입과 같으면 덱 앞에서 건너뛰어 교착 방지.
            // 위험→Grass 강제는 단조로움 우려로 제거(2026-04-28 디렉터 결정) — 청크 단위 무작위 다양성 보존.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (_deck.Count == 0) RefillDeck();
                LaneType candidate = _deck[0];
                if (candidate != _lastChunkType)
                {
                    _deck.RemoveAt(0);
                    return candidate;
                }
                // 같은 타입이면 뒤로 보내고 다음 카드 시도
                _deck.RemoveAt(0);
                _deck.Add(candidate);
            }
            // 두 번 다 같으면 그냥 사용 (덱이 단일 타입으로만 채워진 극단 케이스)
            if (_deck.Count == 0) RefillDeck();
            LaneType fallback = _deck[0];
            _deck.RemoveAt(0);
            return fallback;
        }

        private void SpawnLane(int zIndex, LaneType type, bool isChunkStart = false, bool isChunkEnd = false, float difficultyMultiplier = 1f)
        {
            // 동일 zIndex 에 이미 레인이 있으면 제거 후 재생성 (중복 겹침 방지)
            if (_lanes.TryGetValue(zIndex, out BaseLane existingLane))
            {
                Debug.LogWarning($"[WorldGenerator] Lane already exists at Z={zIndex} (type={existingLane.Type}), replacing with {type}.");
                if (existingLane != null) existingLane.Despawn();
                _lanes.Remove(zIndex);
            }

            BaseLane lane = null;
            if (type == LaneType.Grass)
            {
                var prefab = _config.GrassLanePrefab;
                if (prefab == null) { Debug.LogError("[WorldGenerator] GrassLane prefab 없음"); return; }
                var instance = Instantiate(prefab, transform);
                instance.SetConfig(_config);
                // 시작 안전 구간(0 이상 SafeStartLanes 미만)이면 플레이어 시작 셀을 비움
                instance.SetSafeStart(zIndex >= 0 && zIndex < _config.SafeStartLanes);
                lane = instance;
            }
            else if (type == LaneType.Road)
            {
                var prefab = _config.RoadLanePrefab;
                if (prefab == null) { Debug.LogError("[WorldGenerator] RoadLane prefab 없음"); return; }
                var road = Instantiate(prefab, transform);
                // 청크 바깥 경계(잔디/강과 맞닿는 면)의 점선은 생략 → Initialize 전에 세팅
                road.SetLaneEdges(drawBack: !isChunkStart, drawFront: !isChunkEnd);
                lane = road;
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
            else if (type == LaneType.Rail)
            {
                var prefab = _config.RailLanePrefab;
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

            lane.Initialize(zIndex, _config.LaneSpanX, difficultyMultiplier);
            _lanes[zIndex] = lane;
            _furthestSpawnedZ = Mathf.Max(_furthestSpawnedZ, zIndex);
        }
    }
}
