using UnityEngine;

namespace VoxelRoad.River
{
    /// <summary>강 위를 흐르는 통나무. Trigger 진입 시 Player를 자식으로 탑승시켜 함께 이동.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Log : MonoBehaviour
    {
        [SerializeField] private float _boundsPadding = 2.5f;
        private float _speed;
        private float _direction;
        private float _halfLaneSpan;
        private float _leftLimit;
        private float _rightLimit;
        private Transform _passenger;

        /// <summary>X축 속도(부호 포함). 점프 착지 지점 예측에 사용.</summary>
        public float VelocityX => _speed * _direction;

        public void Launch(float speed, float direction, float laneSpanX)
        {
            _speed = speed;
            _direction = Mathf.Sign(direction);
            _halfLaneSpan = laneSpanX * 0.5f;
            _leftLimit = -_halfLaneSpan - _boundsPadding;
            _rightLimit = _halfLaneSpan + _boundsPadding;
        }

        private void Update()
        {
            transform.position += new Vector3(_speed * _direction * Time.deltaTime, 0f, 0f);
            float x = transform.position.x;

            if ((_direction > 0f && x > _rightLimit) || (_direction < 0f && x < _leftLimit))
            {
                if (_passenger != null && _passenger.parent == transform)
                    _passenger.SetParent(null, true);
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            // 점프 호 이동 중에는 탑승하지 않음. 착지 후 TryBoardLog()가 처리.
            if (PlayerController.Instance != null && PlayerController.Instance.IsMoving) return;

            // 레인 경계 밖(패딩 영역)에 있는 통나무는 탑승 금지
            if (Mathf.Abs(transform.position.x) > _halfLaneSpan) return;

            // 플레이어와 통나무 중심이 X·Z 모두 반칸 이내로 정렬돼야 탑승 (일직선 강제)
            const float alignmentTolerance = 0.5f;
            float dx = Mathf.Abs(other.transform.position.x - transform.position.x);
            float dz = Mathf.Abs(other.transform.position.z - transform.position.z);
            if (dx > alignmentTolerance || dz > alignmentTolerance) return;

            _passenger = other.transform;
            _passenger.SetParent(transform, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (other.transform.parent == transform)
                other.transform.SetParent(null, true);
            _passenger = null;
        }

        private void OnDestroy()
        {
            if (_passenger != null && _passenger.parent == transform)
                _passenger.SetParent(null, true);
        }
    }
}
