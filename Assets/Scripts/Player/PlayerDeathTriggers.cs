using UnityEngine;
using VoxelRoad.Game;
using VoxelRoad.Rail;
using VoxelRoad.Vehicles;

namespace VoxelRoad.Player
{
    /// <summary>Idle 타이머·OOB·차량/기차 충돌 사망 트리거.</summary>
    public sealed class PlayerDeathTriggers : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [Tooltip("입력이 이 시간(초) 동안 없으면 Idle 사망. 좌우·전진·후퇴 어떤 입력이든 reset.")]
        [SerializeField] private float _idleTimeoutSeconds = 5f;

        private float _idleTimer;

        private void Awake()
        {
            if (_player == null) { Debug.LogError("[PlayerDeathTriggers] _player 미지정"); enabled = false; return; }
        }

        public void ResetIdleTimer() { _idleTimer = 0f; }

        private void Update()
        {
            if (!_player.GameManager.IsAlive) return;

            // Idle Timer: 마지막 입력 시점부터 누적. 입력 시 ResetIdleTimer() 호출됨.
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleTimeoutSeconds)
            {
                _player.GameManager.KillPlayer(DeathReason.Idle);
                return;
            }

            // 통나무 탑승 중에는 월드 X에 따라 그리드 X 재동기화, 후진/좌우 이탈 감지
            if (_player.IsMoving) return;
            if (_player.transform.parent == null) return;

            // 통나무가 맵 경계까지 흘러가면 탈출·익사
            int halfSpan = _player.WorldGenerator.LaneHalfSpan;
            float px = _player.transform.position.x;
            if (px < -halfSpan || px > halfSpan)
            {
                float clampedX = Mathf.Clamp(px, -halfSpan, halfSpan);
                _player.transform.SetParent(null, true);
                var p = _player.transform.position;
                _player.transform.position = new Vector3(clampedX, p.y, p.z);
                _player.GameManager.KillPlayer(DeathReason.Drown);
                return;
            }

            int worldX = Mathf.RoundToInt(_player.transform.position.x);
            if (worldX != _player.GridPos.X)
                _player.SyncGridX(worldX);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_player.GameManager.IsAlive) return;
            var vehicle = other.GetComponentInParent<Vehicle>();
            if (vehicle != null)
            {
                _player.LastImpactSource = vehicle.transform;
                _player.LastImpactIsSideHit = _player.IsMoving;
                _player.GameManager.KillPlayer(DeathReason.Vehicle);
                return;
            }
            var train = other.GetComponentInParent<Train>();
            if (train != null)
            {
                _player.LastImpactSource = train.transform;
                _player.LastImpactIsSideHit = _player.IsMoving;
                _player.GameManager.KillPlayer(DeathReason.Train);
            }
        }
    }
}
