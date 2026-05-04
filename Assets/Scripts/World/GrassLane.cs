using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>잔디 레인. 녹색 바닥 + 확률적 데코(나무·바위·꽃) 배치. 데코 위치는 플레이어 통행 불가.</summary>
    public sealed class GrassLane : BaseLane
    {
        [SerializeField] private MeshRenderer _ground;
        [SerializeField] private Transform _decorRoot;

        [Header("Color Jitter")]
        [SerializeField, Range(0f, 1f)] private float _patchDensity = 0.30f;
        [SerializeField, Range(0f, 0.2f)] private float _patchJitter = 0.08f;

        [Header("Safe Passage")]
        [Tooltip("플레이어 가시 X 범위(±n). 이 범위 안 빈 셀 수를 보장.")]
        [SerializeField] private int _safePassageHalfSpan = 3;
        [Tooltip("가시 범위 내 최소 빈 셀 수. 사이드스텝 1~2회로 통과 가능하도록 보장.")]
        [SerializeField] private int _minEmptyCellsInRange = 3;

        private LaneConfigSO _config;
        private bool _isSafeStart;
        private readonly System.Collections.Generic.HashSet<int> _blockedCells = new();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        // MonoBehaviour 정적 필드 초기자에서 native 객체(MaterialPropertyBlock)를 생성하면
        // Unity가 CreateImpl을 거부해 타입 로딩이 실패함 → 첫 사용 시 지연 생성.
        private static MaterialPropertyBlock _patchMpb;

        public override LaneType Type => LaneType.Grass;

        public void SetConfig(LaneConfigSO config) => _config = config;

        /// <summary>시작 안전 구간 표시(플레이어 시작 지점 셀에는 데코를 배치하지 않음).</summary>
        public void SetSafeStart(bool safe) => _isSafeStart = safe;

        /// <summary>데코가 점유한 X 셀 (타일 중심 좌표 int) — 충돌 판정에서 사용.</summary>
        public System.Collections.Generic.IReadOnlyCollection<int> BlockedCells => _blockedCells;

        public override bool IsBlockedAt(int x) => _blockedCells.Contains(x);

        protected override void Build()
        {
            _blockedCells.Clear();

            // 바닥 스케일: Unity Plane = 10m → laneSpanX / 10.
            // 레인 타입별 Y 오프셋으로 Z-fighting 완전 분리 (Grass=-0.02, Road=0, River=-0.01).
            if (_ground != null)
            {
                _ground.transform.localScale = new Vector3(_laneSpanX / 10f, 1f, 0.1f);
                var lp = _ground.transform.localPosition;
                _ground.transform.localPosition = new Vector3(lp.x, -0.02f, lp.z);
            }

            if (_decorRoot == null || _config == null) return;
            var prefabs = _config.GrassDecorPrefabs;
            if (prefabs == null || prefabs.Length == 0) return;

            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            // 좌·우 즉시 이동 보장: 차단 그룹은 최대 2셀 연속, 그룹 사이 최소 2 빈 셀.
            // (X X _ X X) 같은 고립 빈 셀(좌·우 모두 차단) 패턴을 막아 어느 빈 셀에서든
            // 좌·우 중 한 방향은 1번 입력으로 옆 칸 이동이 가능해짐. 결과 차단률은 자연스럽게
            // 원본 density 의 ~70~80% 수준으로 감소(시각 데코는 밀도 유지·통로는 명확).
            int consecutiveBlocked = 0;
            int forcedEmptyRemaining = 0;
            const int MAX_CONSECUTIVE = 2;
            const int MIN_GAP_AFTER_GROUP = 2;
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                bool place = Random.value <= _config.GrassDecorDensity;
                if (place && consecutiveBlocked >= MAX_CONSECUTIVE) place = false;
                if (place && forcedEmptyRemaining > 0) place = false;
                // 안전 구간 레인은 플레이어 시작 셀(x=0) 및 좌우 1셀을 비워 길을 보장
                if (place && _isSafeStart && Mathf.Abs(x) <= 1) place = false;

                GameObject prefab = null;
                if (place)
                {
                    prefab = prefabs[Random.Range(0, prefabs.Length)];
                    if (prefab == null) place = false;
                }

                if (!place)
                {
                    if (consecutiveBlocked > 0)
                        forcedEmptyRemaining = MIN_GAP_AFTER_GROUP - 1;  // 이번 빈 셀이 갭 1개 차감
                    else if (forcedEmptyRemaining > 0)
                        forcedEmptyRemaining--;
                    consecutiveBlocked = 0;
                    continue;
                }

                var decor = Instantiate(prefab, _decorRoot);
                // 셀 정중앙에 배치 (플레이어 GridPosition.ToWorldPosition 과 동일한 정수 X 규약).
                decor.transform.localPosition = new Vector3(x, 0f, 0f);
                decor.transform.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                float s = _config.GrassDecorScale;
                if (s > 0f && Mathf.Abs(s - 1f) > 0.001f)
                    decor.transform.localScale = new Vector3(s, s, s);
                _blockedCells.Add(x);
                consecutiveBlocked++;
            }

            EnsureSafePassage();
            BuildColorPatches();
        }

        private void EnsureSafePassage()
        {
            if (_decorRoot == null) return;
            int hs = _safePassageHalfSpan;
            int rangeSize = hs * 2 + 1;
            int targetEmpty = Mathf.Min(_minEmptyCellsInRange, rangeSize);

            int emptyCount = 0;
            for (int x = -hs; x <= hs; x++)
                if (!_blockedCells.Contains(x)) emptyCount++;
            if (emptyCount >= targetEmpty) return;

            // 차단 셀 후보 수집.
            var blockedInRange = new System.Collections.Generic.List<int>(rangeSize);
            for (int x = -hs; x <= hs; x++)
                if (_blockedCells.Contains(x)) blockedInRange.Add(x);

            int needed = targetEmpty - emptyCount;
            for (int i = 0; i < needed && blockedInRange.Count > 0; i++)
            {
                int pickIdx = Random.Range(0, blockedInRange.Count);
                int pickX = blockedInRange[pickIdx];
                blockedInRange.RemoveAt(pickIdx);
                ClearDecorAt(pickX);
                _blockedCells.Remove(pickX);
            }
        }

        private void ClearDecorAt(int x)
        {
            for (int i = _decorRoot.childCount - 1; i >= 0; i--)
            {
                var child = _decorRoot.GetChild(i);
                if (Mathf.RoundToInt(child.localPosition.x) == x)
                    Destroy(child.gameObject);
            }
        }

        /// <summary>이전 grass 레인의 차단 셀과 함께 통행성을 보장. WorldGenerator가 grass-after-grass 시 호출.
        /// Phase A: 수직 2-스택 제거(같은 X 양 레인 차단 → current 비움).
        /// Phase A.5: prev 차단 X 위치에서 current 좌우 동시 차단 방지(우회 후 옆 이동 강제 방지).
        /// Phase B: BFS로 prev 빈 셀이 모두 current 빈 셀에 도달 가능한지 검증, 실패 시 가장 가까운 current 차단 셀 제거 반복.
        /// 100k 시뮬에서 0 실패 검증 완료(2026-05-04).</summary>
        public void EnsurePassageWithPrevious(System.Collections.Generic.IReadOnlyCollection<int> prevBlockedCells)
        {
            if (_decorRoot == null || prevBlockedCells == null) return;
            int hs = _safePassageHalfSpan;
            int rangeSize = hs * 2 + 1;

            var prevSet = new System.Collections.Generic.HashSet<int>(prevBlockedCells);

            // Phase A: prev·current 차단 X 거리 ≥ 2 강제. 같은 X(수직 z-stack)와 ±1(대각선 z-stack)
            // 모두 방지 → 어떤 X에서든 두 lane 중 한 쪽은 비어 있어 +z 한 번에 통과 가능.
            var toRemoveA = new System.Collections.Generic.List<int>();
            foreach (int b in _blockedCells)
            {
                if (prevSet.Contains(b) || prevSet.Contains(b - 1) || prevSet.Contains(b + 1))
                    toRemoveA.Add(b);
            }
            for (int i = 0; i < toRemoveA.Count; i++)
            {
                int x = toRemoveA[i];
                ClearDecorAt(x);
                _blockedCells.Remove(x);
            }

            // Phase A.5: prev 차단 X 위치를 통과해 current[X] 빈 셀에 도달했을 때
            // current[X-1]과 current[X+1]이 모두 차단이면 또다시 +z/-z 우회 강제됨 → 한 쪽 비움.
            for (int x = -hs; x <= hs; x++)
            {
                if (!prevSet.Contains(x)) continue;
                if (_blockedCells.Contains(x)) continue;
                bool leftBlocked = _blockedCells.Contains(x - 1);
                bool rightBlocked = _blockedCells.Contains(x + 1);
                if (!leftBlocked || !rightBlocked) continue;

                bool leftInRange = (x - 1) >= -hs;
                bool rightInRange = (x + 1) <= hs;
                int target;
                if (leftInRange && rightInRange) target = (Random.value < 0.5f) ? (x - 1) : (x + 1);
                else if (leftInRange) target = x - 1;
                else if (rightInRange) target = x + 1;
                else continue;

                ClearDecorAt(target);
                _blockedCells.Remove(target);
            }

            // Phase B
            for (int iter = 0; iter < rangeSize; iter++)
            {
                int unreachableX = FindUnreachablePrevEmpty(prevSet);
                if (unreachableX == int.MinValue) return;

                int nearestBlocked = FindNearestBlockedInRange(unreachableX);
                if (nearestBlocked == int.MinValue) return;
                ClearDecorAt(nearestBlocked);
                _blockedCells.Remove(nearestBlocked);
            }
        }

        private int FindUnreachablePrevEmpty(System.Collections.Generic.HashSet<int> prevBlocked)
        {
            int hs = _safePassageHalfSpan;
            for (int x = -hs; x <= hs; x++)
            {
                if (prevBlocked.Contains(x)) continue;
                if (!CanReachThisLaneFromPrev(x, prevBlocked)) return x;
            }
            return int.MinValue;
        }

        private bool CanReachThisLaneFromPrev(int startX, System.Collections.Generic.HashSet<int> prevBlocked)
        {
            int hs = _safePassageHalfSpan;
            int rangeSize = hs * 2 + 1;
            var visited = new bool[2 * rangeSize];
            var queue = new System.Collections.Generic.Queue<int>();
            int startIdx = startX + hs;
            queue.Enqueue(startIdx);
            visited[startIdx] = true;
            while (queue.Count > 0)
            {
                int c = queue.Dequeue();
                int row = c / rangeSize;
                int col = c % rangeSize;
                TryEnqueueNeighbor(visited, queue, prevBlocked, row, col - 1, rangeSize);
                TryEnqueueNeighbor(visited, queue, prevBlocked, row, col + 1, rangeSize);
                TryEnqueueNeighbor(visited, queue, prevBlocked, row - 1, col, rangeSize);
                TryEnqueueNeighbor(visited, queue, prevBlocked, row + 1, col, rangeSize);
            }
            for (int col = 0; col < rangeSize; col++)
            {
                int x = col - hs;
                if (!_blockedCells.Contains(x) && visited[rangeSize + col]) return true;
            }
            return false;
        }

        private void TryEnqueueNeighbor(bool[] visited, System.Collections.Generic.Queue<int> queue,
            System.Collections.Generic.HashSet<int> prevBlocked, int row, int col, int rangeSize)
        {
            if (row < 0 || row > 1 || col < 0 || col >= rangeSize) return;
            int x = col - _safePassageHalfSpan;
            bool blocked = (row == 0) ? prevBlocked.Contains(x) : _blockedCells.Contains(x);
            if (blocked) return;
            int idx = row * rangeSize + col;
            if (visited[idx]) return;
            visited[idx] = true;
            queue.Enqueue(idx);
        }

        private int FindNearestBlockedInRange(int x)
        {
            int hs = _safePassageHalfSpan;
            int best = int.MinValue;
            int bestDist = int.MaxValue;
            for (int xx = -hs; xx <= hs; xx++)
            {
                if (!_blockedCells.Contains(xx)) continue;
                int dist = Mathf.Abs(xx - x);
                if (dist < bestDist) { bestDist = dist; best = xx; }
            }
            return best;
        }

        private void BuildColorPatches()
        {
            if (_ground == null || _patchDensity <= 0f) return;
            Material baseMat = _ground.sharedMaterial;
            if (baseMat == null || !baseMat.HasProperty(BaseColorId)) return;
            Color baseColor = baseMat.GetColor(BaseColorId);

            int halfSpan = Mathf.RoundToInt(_laneSpanX / 2f);
            for (int x = -halfSpan; x < halfSpan; x++)
            {
                if (_blockedCells.Contains(x)) continue;
                if (Random.value > _patchDensity) continue;

                var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                patch.name = "Patch";
                patch.transform.SetParent(transform, false);
                Destroy(patch.GetComponent<Collider>());
                var mr = patch.GetComponent<MeshRenderer>();
                mr.sharedMaterial = baseMat;

                Color jittered = new Color(
                    Mathf.Clamp01(baseColor.r + Random.Range(-_patchJitter, _patchJitter)),
                    Mathf.Clamp01(baseColor.g + Random.Range(-_patchJitter, _patchJitter)),
                    Mathf.Clamp01(baseColor.b + Random.Range(-_patchJitter, _patchJitter)),
                    1f);
                if (_patchMpb == null) _patchMpb = new MaterialPropertyBlock();
                _patchMpb.Clear();
                _patchMpb.SetColor(BaseColorId, jittered);
                mr.SetPropertyBlock(_patchMpb);

                // Grass 지면 y=-0.02 위에 살짝 떠 있게. 두께는 매우 얇게.
                patch.transform.localPosition = new Vector3(x, -0.014f, 0f);
                patch.transform.localScale = new Vector3(0.6f, 0.005f, 0.6f);
            }
        }
    }
}
