using System.Collections;
using UnityEngine;
using VoxelRoad.Game;
using VoxelRoad.Vehicles;
using VoxelRoad.World;

/// <summary>플레이어 그리드 이동·점프 연출·입력 큐 처리, 사망(차량·익사) 시 입력 차단.</summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private WorldGenerator _worldGenerator;
    [SerializeField] private float _tileSize = 1f;
    [SerializeField] private float _moveDuration = 0.12f;
    [SerializeField] private float _jumpHeight = 0.5f;

    private GridPosition _gridPos;
    private GridPosition _moveTarget;   // 현재 진행 중인 이동의 도착 셀 (큐 입력 기준점)
    private GridPosition? _queuedTarget;
    private bool _isMoving;

    public GridPosition GridPos => _gridPos;
    public int MaxZ { get; private set; }
    /// <summary>점프 호 이동 중이면 true. Log가 Trigger Enter를 무시하는 데 사용.</summary>
    public bool IsMoving => _isMoving;

    private void Awake()
    {
        if (_inputReader == null) { Debug.LogError("[PlayerController] _inputReader 미지정"); enabled = false; return; }
        if (_gameManager == null) { Debug.LogError("[PlayerController] _gameManager 미지정"); enabled = false; return; }
        if (_worldGenerator == null) { Debug.LogError("[PlayerController] _worldGenerator 미지정"); enabled = false; return; }
    }

    private void Start()
    {
        _gridPos = new GridPosition(0, 0);
        transform.position = _gridPos.ToWorldPosition(_tileSize);
    }

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.OnMoveInput += HandleMoveInput;
        if (_gameManager != null)
            _gameManager.OnPlayerDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.OnMoveInput -= HandleMoveInput;
        if (_gameManager != null)
            _gameManager.OnPlayerDied -= HandleDied;
    }

    private void HandleDied(DeathReason reason)
    {
        StopAllCoroutines();
        if (_inputReader != null) _inputReader.enabled = false;
        _isMoving = false;
    }

    private void Update()
    {
        // 통나무 탑승 중에는 월드 X에 따라 그리드 X 재동기화, 후진/좌우 이탈 감지
        if (!_gameManager.IsAlive || _isMoving) return;
        if (transform.parent == null) return;

        // 통나무가 맵 경계(halfSpan)까지 흘러가면 탈출·익사
        // 카메라는 MapXLimit에서 이미 멈추므로 플레이어가 화면 끝자락에서 사라지는 연출
        int halfSpan = _worldGenerator.LaneHalfSpan;
        float px = transform.position.x;
        if (px < -halfSpan || px > halfSpan)
        {
            float clampedX = Mathf.Clamp(px, -halfSpan, halfSpan);
            transform.SetParent(null, true);
            var p = transform.position;
            transform.position = new Vector3(clampedX, p.y, p.z);
            _gameManager.KillPlayer(DeathReason.Drown);
            return;
        }

        int worldX = Mathf.RoundToInt(transform.position.x);
        if (worldX != _gridPos.X)
        {
            _gridPos = new GridPosition(worldX, _gridPos.Z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 차량 충돌 사망: Player 쪽에서 감지해 Vehicle 이 GameManager 를 몰라도 되게 함
        if (!_gameManager.IsAlive) return;
        if (other.GetComponentInParent<Vehicle>() != null)
            _gameManager.KillPlayer(DeathReason.Vehicle);
    }

    private void HandleMoveInput(MoveDirection dir)
    {
        if (!_gameManager.IsAlive) return;
        int dx = 0, dz = 0;
        switch (dir)
        {
            case MoveDirection.Forward:  dz =  1; break;
            case MoveDirection.Backward: dz = -1; break;
            case MoveDirection.Left:     dx = -1; break;
            case MoveDirection.Right:    dx =  1; break;
        }
        // 이동 중이면 진행 중인 도착 셀을 기준으로 다음 칸 계산 (연타 시 2칸 점프 방지).
        GridPosition basePos = _isMoving ? _moveTarget : _gridPos;
        GridPosition target = basePos.Move(dx, dz);
        // 맵 뒤쪽 경계: 시작 지점(z=0) 보다 뒤로는 이동 불가 — 맵 끝에서 시작하는 설계.
        if (target.Z < 0) return;
        // 좌우 경계: 카메라 가시 영역 끝(MapXLimit)까지만 이동 허용. 좌우 동일 기준.
        int xLimit = Mathf.RoundToInt(VoxelRoad.CameraSystem.CrossyRoadCameraExtension.MapXLimit);
        if (target.X < -xLimit || target.X > xLimit) return;
        // 고정 장애물(나무/바위 등)이 있는 셀은 통행 불가
        if (_worldGenerator.IsCellBlocked(target.X, target.Z)) return;

        // 이동 방향으로 즉시 시선 회전 (Crossy Road 특유의 틱 회전)
        transform.rotation = Quaternion.LookRotation(new Vector3(dx, 0f, dz));

        if (_isMoving)
        {
            _queuedTarget = target;
            return;
        }
        StartCoroutine(MoveRoutine(target, dx));
    }

    private IEnumerator MoveRoutine(GridPosition target, int dx = 0)
    {
        _isMoving = true;
        _moveTarget = target;
        _queuedTarget = null;

        // 이동 시작 시점에 통나무로부터 언패런트 (이동은 그리드 기준)
        Transform prevParent = transform.parent;
        if (prevParent != null)
        {
            transform.SetParent(null, true);
            var wp = transform.position;
            transform.position = new Vector3(wp.x, 0f, wp.z); // Y만 리셋, X는 유지
        }

        Vector3 from = transform.position;
        Vector3 to = target.ToWorldPosition(_tileSize);

        // 통나무에서 출발하는 경우 경로 계산을 분기:
        //  - 좌우 점프(dx!=0): 출발 통나무 프레임 기준 상대 좌표로 보간 → 드리프트를 매 프레임 자동 추적
        //  - 전후 점프(dx==0): 월드 X 고정 → 출발 통나무가 밀려도 도착 X 불변 (다른 레인으로 건너가는 의미)
        bool trackLog = prevParent != null && dx != 0;
        float fromRelX = 0f;
        float toRelX = 0f;
        if (prevParent != null)
        {
            if (trackLog)
            {
                // 탑승 중 누적될 수 있는 서브그리드 드리프트 제거: 시작 오프셋을 정수 슬롯으로 스냅
                fromRelX = Mathf.Round((from.x - prevParent.position.x) / _tileSize) * _tileSize;
                toRelX = fromRelX + dx * _tileSize;
            }
            else
            {
                // 전후 이동: X를 출발 시점 월드 좌표로 고정 (대각선·드리프트 방지)
                to.x = from.x;
            }
        }
        else
        {
            // 지면에서 출발: X 비정수면 그리드로 재스냅 후 로그 X 보정
            from.x = _gridPos.X * _tileSize;
            transform.position = new Vector3(from.x, from.y, from.z);
            AdjustJumpTargetForLog(ref to, target);
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
            transform.position = new Vector3(x, arc, z);
            yield return null;
        }

        if (trackLog && prevParent != null)
            transform.position = new Vector3(prevParent.position.x + toRelX, 0f, to.z);
        else
            transform.position = to;
        _gridPos = target;
        if (_gridPos.Z > MaxZ) MaxZ = _gridPos.Z;
        _isMoving = false;

        TryBoardLog();
        CheckRiverArrival();

        // 땅(비강) 착지 시 X를 그리드 정수로 재스냅 — 로그 드리프트 누적 방지
        if (transform.parent == null && _worldGenerator.GetLaneTypeAt(_gridPos.Z) != LaneType.River)
            transform.position = new Vector3(_gridPos.X * _tileSize, transform.position.y, transform.position.z);

        if (_gameManager.IsAlive && _queuedTarget.HasValue)
        {
            GridPosition next = _queuedTarget.Value;
            _queuedTarget = null;
            StartCoroutine(MoveRoutine(next, next.X - _gridPos.X));
        }
    }

    /// <summary>점프 시작 시 착지 셀 위의 통나무를 탐색해 점프 목표 X를 통나무 예측 위치로 보정.
    /// 단, 보정 범위는 ±AlignmentTolerance 이내로 제한 — 옆 셀 통나무에 자석처럼 끌려가지 않게.</summary>
    private void AdjustJumpTargetForLog(ref Vector3 to, GridPosition target)
    {
        if (_worldGenerator.GetLaneTypeAt(target.Z) != LaneType.River) return;

        float halfSpan = _worldGenerator.LaneHalfSpan;
        var hits = Physics.OverlapBox(to + Vector3.up * 0.3f,
            new Vector3(0.6f, 0.4f, 0.6f), Quaternion.identity,
            ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var log = hits[i].GetComponentInParent<VoxelRoad.River.Log>();
            if (log == null) continue;

            float predictedLogX = log.transform.position.x + log.VelocityX * _moveDuration;
            // 착지 시점에 통나무가 레인 경계 밖이면 보정 대상에서 제외 (끝자락 오탑승 방지)
            if (Mathf.Abs(predictedLogX) > halfSpan) continue;
            // 점프 목표와 통나무 예측 위치가 0.6 이내일 때만 보정 — 멀리 있는 통나무로 끌려가지 않게
            if (Mathf.Abs(predictedLogX - to.x) > 0.6f) continue;

            to.x = predictedLogX;
            return;
        }
    }

    /// <summary>착지 후 통나무 탑승 처리. AlignmentTolerance 이내일 때만 탑승 → 일직선 정렬 강제.</summary>
    private void TryBoardLog()
    {
        if (_worldGenerator.GetLaneTypeAt(_gridPos.Z) != LaneType.River) return;

        float halfSpan = _worldGenerator.LaneHalfSpan;
        var hits = Physics.OverlapBox(transform.position + Vector3.up * 0.3f,
            new Vector3(0.6f, 0.4f, 0.6f), Quaternion.identity,
            ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var log = hits[i].GetComponentInParent<VoxelRoad.River.Log>();
            if (log == null) continue;

            if (Mathf.Abs(log.transform.position.x) > halfSpan) continue;

            float dx = Mathf.Abs(transform.position.x - log.transform.position.x);
            float dz = Mathf.Abs(transform.position.z - log.transform.position.z);
            if (dx > log.HalfWidthX || dz > log.HalfLengthZ) continue;

            transform.SetParent(log.transform, true);
            log.SnapToSurface(transform);
            return;
        }
    }


    /// <summary>도착 레인이 River인데 통나무 위가 아니면 익사 처리.</summary>
    private void CheckRiverArrival()
    {
        if (_worldGenerator.GetLaneTypeAt(_gridPos.Z) != LaneType.River) return;
        // Trigger 판정이 끝난 뒤 parent 가 Log 이면 탑승 성공
        if (transform.parent != null) return;
        _gameManager.KillPlayer(DeathReason.Drown);
    }
}
