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
        private BoxCollider _col;

        /// <summary>X축 속도(부호 포함). 점프 착지 지점 예측에 사용.</summary>
        public float VelocityX => _speed * _direction;

        /// <summary>BoxCollider 월드 X 절반 폭. 탑승 가능 X 범위 판정에 사용.</summary>
        public float HalfWidthX
        {
            get
            {
                if (_col == null) _col = GetComponent<BoxCollider>();
                return _col != null ? _col.bounds.extents.x : 1.5f;
            }
        }

        /// <summary>비주얼 표면 월드 Y. 탑승 시 플레이어 Y 스냅에 사용.</summary>
        public float SurfaceY
        {
            get
            {
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.gameObject.name == "_BlobShadow") continue;
                    return r.bounds.max.y;
                }
                return transform.position.y;
            }
        }

        public void Launch(float speed, float direction, float laneSpanX)
        {
            _col = GetComponent<BoxCollider>();
            _speed = speed;
            _direction = Mathf.Sign(direction);
            _halfLaneSpan = laneSpanX * 0.5f;
            _leftLimit  = -_halfLaneSpan - _boundsPadding;
            _rightLimit =  _halfLaneSpan + _boundsPadding;
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
            if (PlayerController.Instance != null && PlayerController.Instance.IsMoving) return;
            if (Mathf.Abs(transform.position.x) > _halfLaneSpan) return;

            float dx = Mathf.Abs(other.transform.position.x - transform.position.x);
            float dz = Mathf.Abs(other.transform.position.z - transform.position.z);
            if (dx > HalfWidthX || dz > 0.6f) return;

            _passenger = other.transform;
            _passenger.SetParent(transform, true);
            SnapToSurface(_passenger);
        }

        /// <summary>탑승자를 통나무 비주얼 표면 위로 Y 스냅.</summary>
        public void SnapToSurface(Transform passenger)
        {
            float sy = SurfaceY;
            if (sy > 0.001f)
            {
                var p = passenger.position;
                passenger.position = new Vector3(p.x, sy, p.z);
            }
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
