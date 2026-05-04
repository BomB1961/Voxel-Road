using UnityEngine;
using VoxelRoad.Game;
using VoxelRoad.River;
using VoxelRoad.World;

namespace VoxelRoad.Player
{
    /// <summary>통나무 탑승·예측 착지 보정·강 도착 시 익사 판정.</summary>
    public sealed class PlayerLogRider : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;

        private void Awake()
        {
            if (_player == null) { Debug.LogError("[PlayerLogRider] _player 미지정"); enabled = false; return; }
        }

        /// <summary>점프 시작 시 착지 셀 위 통나무를 탐색해 점프 목표 X를 통나무 예측 위치로 보정.
        /// 보정 범위는 ±0.6 이내로 제한 — 옆 셀 통나무에 자석처럼 끌려가지 않게.</summary>
        public void AdjustJumpTargetForLog(ref Vector3 to, GridPosition target, float moveDuration)
        {
            if (_player.WorldGenerator.GetLaneTypeAt(target.Z) != LaneType.River) return;

            float halfSpan = _player.WorldGenerator.LaneHalfSpan;
            var hits = Physics.OverlapBox(to + Vector3.up * 0.3f,
                new Vector3(0.6f, 0.4f, 0.6f), Quaternion.identity,
                ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var log = hits[i].GetComponentInParent<Log>();
                if (log == null) continue;

                float predictedLogX = log.transform.position.x + log.VelocityX * moveDuration;
                if (Mathf.Abs(predictedLogX) > halfSpan) continue;
                if (Mathf.Abs(predictedLogX - to.x) > 0.6f) continue;

                to.x = predictedLogX;
                return;
            }
        }

        /// <summary>착지 후 통나무 탑승 처리. AlignmentTolerance 이내일 때만 탑승 → 일직선 정렬 강제.
        /// 실제 부착 절차는 Log.TryAttachPassenger 가 일괄 처리.</summary>
        public void TryBoardLog()
        {
            if (_player.WorldGenerator.GetLaneTypeAt(_player.GridPos.Z) != LaneType.River) return;

            var hits = Physics.OverlapBox(_player.transform.position + Vector3.up * 0.3f,
                new Vector3(0.6f, 0.4f, 0.6f), Quaternion.identity,
                ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var log = hits[i].GetComponentInParent<Log>();
                if (log == null) continue;
                if (log.TryAttachPassenger(_player.transform)) return;
            }
        }

        /// <summary>도착 레인이 River인데 통나무 위가 아니면 익사 처리.</summary>
        public void CheckRiverArrival()
        {
            if (_player.WorldGenerator.GetLaneTypeAt(_player.GridPos.Z) != LaneType.River) return;
            if (_player.transform.parent != null) return;
            _player.GameManager.KillPlayer(DeathReason.Drown);
        }
    }
}
