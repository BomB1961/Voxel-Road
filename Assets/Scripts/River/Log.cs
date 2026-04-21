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
        private float _leftLimit;
        private float _rightLimit;
        private Transform _passenger;

        public void Launch(float speed, float direction, float laneSpanX)
        {
            _speed = speed;
            _direction = Mathf.Sign(direction);
            _leftLimit = -laneSpanX * 0.5f - _boundsPadding;
            _rightLimit = laneSpanX * 0.5f + _boundsPadding;
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
            // 점프 호 이동 중에는 콜라이더가 스쳐도 탑승하지 않음.
            // 착지 후 PlayerController.TryBoardLog()가 정확히 처리한다.
            if (PlayerController.Instance != null && PlayerController.Instance.IsMoving) return;

            _passenger = other.transform;
            _passenger.SetParent(transform, true);
            // 통나무 정중앙에 스냅 (X만 보정, Z/Y는 유지)
            var p = _passenger.position;
            _passenger.position = new Vector3(transform.position.x, p.y, p.z);
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
