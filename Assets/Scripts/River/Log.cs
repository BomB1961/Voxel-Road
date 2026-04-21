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
            // 점프 호 이동 중에는 탑승하지 않음. 착지 후 TryBoardLog()가 처리.
            if (PlayerController.Instance != null && PlayerController.Instance.IsMoving) return;

            // 플레이어 Z가 통나무 Z 범위(폭 방향) 안에 있어야 탑승 (정지 중 통나무가 흘러올 때)
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Bounds b = col.bounds;
                if (other.transform.position.z < b.min.z || other.transform.position.z > b.max.z) return;
            }

            _passenger = other.transform;
            // X 스냅 없이 현재 위치 그대로 탑승
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
