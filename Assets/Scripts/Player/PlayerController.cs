using System.Collections;
using UnityEngine;
using VoxelRoad.Game;
using VoxelRoad.World;

/// <summary>플레이어 그리드 이동·점프 연출·입력 큐 처리, 사망(차량·익사) 시 입력 차단.</summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private float _tileSize = 1f;
    [SerializeField] private float _moveDuration = 0.12f;
    [SerializeField] private float _jumpHeight = 0.5f;

    private GridPosition _gridPos;
    private GridPosition _moveTarget;   // 현재 진행 중인 이동의 도착 셀 (큐 입력 기준점)
    private GridPosition? _queuedTarget;
    private bool _isMoving;

    public GridPosition GridPos => _gridPos;
    public int MaxZ { get; private set; }

    private void Start()
    {
        _gridPos = new GridPosition(0, 0);
        transform.position = _gridPos.ToWorldPosition(_tileSize);
    }

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.OnMoveInput += HandleMoveInput;
        GameManager.OnPlayerDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.OnMoveInput -= HandleMoveInput;
        GameManager.OnPlayerDied -= HandleDied;
    }

    private void HandleDied()
    {
        StopAllCoroutines();
        if (_inputReader != null) _inputReader.enabled = false;
        _isMoving = false;
    }

    private void Update()
    {
        // 통나무 탑승 중에는 월드 X에 따라 그리드 X 재동기화, 후진/좌우 이탈 감지
        if (!GameManager.IsAlive || _isMoving) return;
        if (transform.parent == null) return;

        int worldX = Mathf.RoundToInt(transform.position.x - 0.5f);
        if (worldX != _gridPos.X)
        {
            _gridPos = new GridPosition(worldX, _gridPos.Z);
        }
    }

    private void HandleMoveInput(MoveDirection dir)
    {
        if (!GameManager.IsAlive) return;
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
        // 좌우 경계: LaneSpanX 절반 밖으로 이동 금지.
        var wg = WorldGenerator.Instance;
        int halfSpan = wg != null ? wg.LaneHalfSpan : 25;
        if (target.X < -halfSpan || target.X >= halfSpan) return;
        // 고정 장애물(나무/바위 등)이 있는 셀은 통행 불가
        if (wg != null && wg.IsCellBlocked(target.X, target.Z)) return;

        // 이동 방향으로 즉시 시선 회전 (Crossy Road 특유의 틱 회전)
        transform.rotation = Quaternion.LookRotation(new Vector3(dx, 0f, dz));

        if (_isMoving)
        {
            _queuedTarget = target;
            return;
        }
        StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(GridPosition target)
    {
        _isMoving = true;
        _moveTarget = target;
        _queuedTarget = null;

        // 이동 시작 시점에 통나무로부터 언패런트 (이동은 그리드 기준)
        Transform prevParent = transform.parent;
        if (prevParent != null)
            transform.SetParent(null, true);

        Vector3 from = transform.position;
        Vector3 to = target.ToWorldPosition(_tileSize);
        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _moveDuration);
            float arc = Mathf.Sin(t * Mathf.PI) * _jumpHeight;
            transform.position = Vector3.Lerp(from, to, t) + new Vector3(0f, arc, 0f);
            yield return null;
        }

        transform.position = to;
        _gridPos = target;
        if (_gridPos.Z > MaxZ) MaxZ = _gridPos.Z;
        _isMoving = false;

        CheckRiverArrival();

        if (GameManager.IsAlive && _queuedTarget.HasValue)
        {
            GridPosition next = _queuedTarget.Value;
            _queuedTarget = null;
            StartCoroutine(MoveRoutine(next));
        }
    }

    /// <summary>도착 레인이 River인데 통나무 위가 아니면 익사 처리.</summary>
    private void CheckRiverArrival()
    {
        var wg = WorldGenerator.Instance;
        if (wg == null) return;
        if (wg.GetLaneTypeAt(_gridPos.Z) != LaneType.River) return;
        // Trigger 판정이 끝난 뒤 parent 가 Log 이면 탑승 성공
        if (transform.parent != null) return;
        GameManager.KillPlayer("drown");
    }
}
