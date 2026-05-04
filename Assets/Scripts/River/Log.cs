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
            // 점프 호 도중에는 탑승 차단 — 트리거는 1회성이므로 GetComponent 허용
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsMoving) return;
            TryAttachPassenger(other.transform);
        }

        /// <summary>승객 부착 진입점 통일: lane 안 통나무·콜라이더 안 위치 검증 후
        /// SetParent + 표면 스냅 + _passenger 등록까지 일괄 처리. 탑승 성공 시 true.
        /// OnTriggerEnter(트리거 진입)와 PlayerLogRider.TryBoardLog(착지 능동 검사)
        /// 양쪽 진입점에서 호출돼 탑승 절차가 항상 한 곳에서만 일어나게 한다.</summary>
        public bool TryAttachPassenger(Transform passenger)
        {
            if (Mathf.Abs(transform.position.x) > _halfLaneSpan) return false;

            float dx = Mathf.Abs(passenger.position.x - transform.position.x);
            float dz = Mathf.Abs(passenger.position.z - transform.position.z);
            if (dx > _halfWidthX || dz > _halfLengthZ) return false;

            _passenger = passenger;
            _passenger.SetParent(transform, true);
            SnapToSurface(_passenger);
            return true;
        }

        /// <summary>탑승자를 통나무 비주얼 표면(Y)과 정수 슬롯(X: 통나무 중심±tileSize)으로 스냅.
        /// X 스냅 근거: 점프 착지 시 _moveDuration 초과 프레임 동안 통나무가 vx×dt 만큼 더 흘러
        /// 상대 오프셋에 드리프트가 영구 고정되는 문제 제거 (3슬롯 이산화).</summary>
        public void SnapToSurface(Transform passenger)
        {
            var p = passenger.position;
            float relX = p.x - transform.position.x;
            float snappedRelX = Mathf.Round(relX);
            float y = _surfaceY > 0.001f ? _surfaceY : p.y;
            passenger.position = new Vector3(transform.position.x + snappedRelX, y, p.z);
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
