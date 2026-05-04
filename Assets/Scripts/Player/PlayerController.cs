using UnityEngine;
using VoxelRoad.Game;
using VoxelRoad.Player;
using VoxelRoad.World;

/// <summary>플레이어 오케스트레이터: 그리드 상태·생명주기·이벤트 라우팅.
/// 실제 동작은 자식 컴포넌트(PlayerMovement, PlayerLogRider, PlayerDeathTriggers).</summary>
public class PlayerController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private WorldGenerator _worldGenerator;
    [SerializeField] private float _tileSize = 1f;

    [Header("Sub-components")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerLogRider _logRider;
    [SerializeField] private PlayerDeathTriggers _deathTriggers;

    public GridPosition GridPos { get; internal set; }
    public GridPosition MoveTarget { get; internal set; }
    public GridPosition? QueuedTarget { get; internal set; }
    public bool IsMoving { get; internal set; }
    public int MaxZ { get; internal set; }
    /// <summary>마지막으로 플레이어를 사망시킨 차량/기차 Transform. PlayerDeathAnimator가 흡착·견인 모션에 사용.</summary>
    public Transform LastImpactSource { get; internal set; }
    /// <summary>충돌 시 점프 중이었는지 = 플레이어가 측면으로 돌진(true) / 가만히 있는 플레이어를 차량·기차가 침(false).</summary>
    public bool LastImpactIsSideHit { get; internal set; }

    public InputReader InputReader => _inputReader;
    public GameManager GameManager => _gameManager;
    public WorldGenerator WorldGenerator => _worldGenerator;
    public float TileSize => _tileSize;

    private void Awake()
    {
        if (_inputReader == null)   { Debug.LogError("[PlayerController] _inputReader 미지정");   enabled = false; return; }
        if (_gameManager == null)   { Debug.LogError("[PlayerController] _gameManager 미지정");   enabled = false; return; }
        if (_worldGenerator == null){ Debug.LogError("[PlayerController] _worldGenerator 미지정"); enabled = false; return; }
        if (_movement == null)      { Debug.LogError("[PlayerController] _movement 미지정");      enabled = false; return; }
        if (_logRider == null)      { Debug.LogError("[PlayerController] _logRider 미지정");      enabled = false; return; }
        if (_deathTriggers == null) { Debug.LogError("[PlayerController] _deathTriggers 미지정"); enabled = false; return; }
    }

    private void Start()
    {
        GridPos = new GridPosition(0, 0);
        transform.position = GridPos.ToWorldPosition(_tileSize);
    }

    private void OnEnable()
    {
        if (_gameManager != null) _gameManager.OnPlayerDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_gameManager != null) _gameManager.OnPlayerDied -= HandleDied;
    }

    private void HandleDied(DeathReason reason)
    {
        // 자식 컴포넌트의 코루틴까지 모두 정지 (방어적 — 향후 코루틴 추가 시 누락 방지).
        if (_movement != null)      _movement.StopAllCoroutines();
        if (_logRider != null)      _logRider.StopAllCoroutines();
        if (_deathTriggers != null) _deathTriggers.StopAllCoroutines();
        if (_inputReader != null) _inputReader.enabled = false;
        IsMoving = false;
    }

    /// <summary>이동 완료 후 그리드·MaxZ 갱신. PlayerMovement에서 호출.</summary>
    internal void NotifyArrived(GridPosition pos)
    {
        GridPos = pos;
        if (GridPos.Z > MaxZ) MaxZ = GridPos.Z;
    }

    /// <summary>월드 X 변경 감지 시 그리드 X 동기화. PlayerDeathTriggers에서 호출.</summary>
    internal void SyncGridX(int worldX)
    {
        GridPos = new GridPosition(worldX, GridPos.Z);
    }
}
