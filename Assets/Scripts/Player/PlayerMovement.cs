using System.Collections;
using UnityEngine;
using VoxelRoad.World;

namespace VoxelRoad.Player
{
    /// <summary>입력 검증·점프 애니메이션·통나무 좌표 추적.</summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerLogRider _logRider;
        [SerializeField] private PlayerDeathTriggers _deathTriggers;
        [SerializeField] private float _moveDuration = 0.12f;
        [SerializeField] private float _jumpHeight = 0.5f;
        [Tooltip("MaxZ로부터 이 그리드 수까지만 후퇴 가능. 카메라 deadzone과 일치시킬 것.")]
        [SerializeField] private int _backwardLimitGrids = 5;
        [Tooltip("플레이어 X 이동 한계(절대값). cube 마커 위치(±23)에서 멈춤. WorldGenerator.LaneHalfSpan보다 좁아야 함.")]
        [SerializeField] private int _playableHalfSpan = 23;

        public float MoveDuration => _moveDuration;

        private void Awake()
        {
            if (_player == null)        { Debug.LogError("[PlayerMovement] _player 미지정");        enabled = false; return; }
            if (_logRider == null)      { Debug.LogError("[PlayerMovement] _logRider 미지정");      enabled = false; return; }
            if (_deathTriggers == null) { Debug.LogError("[PlayerMovement] _deathTriggers 미지정"); enabled = false; return; }
        }

        private void OnEnable()
        {
            if (_player != null && _player.InputReader != null)
                _player.InputReader.OnMoveInput += HandleMoveInput;
        }

        private void OnDisable()
        {
            if (_player != null && _player.InputReader != null)
                _player.InputReader.OnMoveInput -= HandleMoveInput;
        }

        private void HandleMoveInput(MoveDirection dir)
        {
            if (!_player.GameManager.IsAlive) return;
            // 입력 시 idle timer reset — 좌우 무빙도 정당한 행동(통나무 대기)으로 인정.
            _deathTriggers.ResetIdleTimer();

            int dx = 0, dz = 0;
            switch (dir)
            {
                case MoveDirection.Forward:  dz =  1; break;
                case MoveDirection.Backward: dz = -1; break;
                case MoveDirection.Left:     dx = -1; break;
                case MoveDirection.Right:    dx =  1; break;
            }

            // 이동 중이면 진행 중인 도착 셀을 기준으로 다음 칸 계산 (연타 시 2칸 점프 방지).
            GridPosition basePos = _player.IsMoving ? _player.MoveTarget : _player.GridPos;
            GridPosition target = basePos.Move(dx, dz);
            // 맵 뒤쪽 경계: 시작 지점(z=0) 보다 뒤로는 이동 불가.
            if (target.Z < 0) return;
            // 후퇴 제한: 카메라 deadzone과 동일.
            if (target.Z < _player.MaxZ - _backwardLimitGrids) return;
            if (target.X < -_playableHalfSpan || target.X > _playableHalfSpan) return;
            if (_player.WorldGenerator.IsCellBlocked(target.X, target.Z)) return;

            // 이동 방향으로 즉시 시선 회전 (Crossy Road 특유의 틱 회전)
            _player.transform.rotation = Quaternion.LookRotation(new Vector3(dx, 0f, dz));

            if (_player.IsMoving)
            {
                _player.QueuedTarget = target;
                return;
            }
            StartCoroutine(MoveRoutine(target, dx));
        }

        private IEnumerator MoveRoutine(GridPosition target, int dx)
        {
            _player.IsMoving = true;
            _player.MoveTarget = target;
            _player.QueuedTarget = null;

            // 이동 시작 시점에 통나무로부터 언패런트 (이동은 그리드 기준)
            Transform prevParent = _player.transform.parent;
            if (prevParent != null)
            {
                _player.transform.SetParent(null, true);
                var wp = _player.transform.position;
                _player.transform.position = new Vector3(wp.x, 0f, wp.z);
            }

            float tileSize = _player.TileSize;
            Vector3 from = _player.transform.position;
            Vector3 to = target.ToWorldPosition(tileSize);

            // 통나무에서 출발하는 경우 경로 계산을 분기:
            //  - 좌우 점프(dx!=0): 출발 통나무 프레임 기준 상대 좌표 보간 → 드리프트 자동 추적
            //  - 전후 점프(dx==0): 월드 X 고정 → 출발 통나무가 밀려도 도착 X 불변
            bool trackLog = prevParent != null && dx != 0;
            float fromRelX = 0f, toRelX = 0f;
            if (prevParent != null)
            {
                if (trackLog)
                {
                    fromRelX = Mathf.Round((from.x - prevParent.position.x) / tileSize) * tileSize;
                    toRelX = fromRelX + dx * tileSize;
                }
                else
                {
                    to.x = from.x;
                }
            }
            else
            {
                from.x = _player.GridPos.X * tileSize;
                _player.transform.position = new Vector3(from.x, from.y, from.z);
                _logRider.AdjustJumpTargetForLog(ref to, target, _moveDuration);
            }

            float elapsed = 0f;
            while (elapsed < _moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _moveDuration);
                float arc = Mathf.Sin(t * Mathf.PI) * _jumpHeight;
                float z = Mathf.Lerp(from.z, to.z, t);
                float x;
                if (trackLog && prevParent != null)
                {
                    float relX = Mathf.Lerp(fromRelX, toRelX, t);
                    x = prevParent.position.x + relX;
                }
                else
                {
                    x = Mathf.Lerp(from.x, to.x, t);
                }
                _player.transform.position = new Vector3(x, arc, z);
                yield return null;
            }

            if (trackLog && prevParent != null)
                _player.transform.position = new Vector3(prevParent.position.x + toRelX, 0f, to.z);
            else
                _player.transform.position = to;

            _player.NotifyArrived(target);
            _player.IsMoving = false;

            _logRider.TryBoardLog();
            _logRider.CheckRiverArrival();

            // 땅(비강) 착지 시 X를 그리드 정수로 재스냅 — 로그 드리프트 누적 방지
            if (_player.transform.parent == null && _player.WorldGenerator.GetLaneTypeAt(_player.GridPos.Z) != LaneType.River)
                _player.transform.position = new Vector3(_player.GridPos.X * tileSize, _player.transform.position.y, _player.transform.position.z);

            if (_player.GameManager.IsAlive && _player.QueuedTarget.HasValue)
            {
                GridPosition next = _player.QueuedTarget.Value;
                _player.QueuedTarget = null;
                StartCoroutine(MoveRoutine(next, next.X - _player.GridPos.X));
            }
        }
    }
}
