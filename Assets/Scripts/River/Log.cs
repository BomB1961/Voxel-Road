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

        // Launch 시 1회 캐싱 — 매 프레임 GetComponent/foreach 비용 제거
        private float _halfWidthX;
        private float _halfLengthZ;
        private float _surfaceY;

        /// <summary>X축 속도(부호 포함). 점프 착지 지점 예측에 사용.</summary>
        public float VelocityX => _speed * _direction;

        /// <summary>BoxCollider 기준 X 절반 폭. 탑승 X 판정.</summary>
        public float HalfWidthX => _halfWidthX;

        /// <summary>BoxCollider 기준 Z 절반 폭. 탑승 Z 판정.</summary>
        public float HalfLengthZ => _halfLengthZ;

        /// <summary>비주얼 표면 월드 Y. 탑승 시 플레이어 Y 스냅.</summary>
        public float SurfaceY => _surfaceY;

        public void Launch(float speed, float direction, float laneSpanX)
        {
            _speed = speed;
            _direction = Mathf.Sign(direction);
            _halfLaneSpan = laneSpanX * 0.5f;
            _leftLimit  = -_halfLaneSpan - _boundsPadding;
            _rightLimit =  _halfLaneSpan + _boundsPadding;

            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                // bounds.extents는 스케일 변경 직후 미반영될 수 있으므로 직접 계산
                _halfWidthX  = col.size.x * transform.lossyScale.x * 0.5f;
                _halfLengthZ = col.size.z * transform.lossyScale.z * 0.5f;
            }
            else
            {
                _halfWidthX  = 1.5f;
                _halfLengthZ = 0.6f;
            }

            // BlobShadow 제외한 첫 번째 렌더러의 표면 Y 캐싱
            _surfaceY = transform.position.y;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name == "_BlobShadow") continue;
                _surfaceY = r.bounds.max.y;
                break;
            }
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
            if (dx > _halfWidthX || dz > _halfLengthZ) return;

            _passenger = other.transform;
            _passenger.SetParent(transform, true);
            SnapToSurface(_passenger);
        }

        /// <summary>탑승자를 통나무 비주얼 표면 위로 Y 스냅.</summary>
        public void SnapToSurface(Transform passenger)
        {
            if (_surfaceY > 0.001f)
            {
                var p = passenger.position;
                passenger.position = new Vector3(p.x, _surfaceY, p.z);
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
