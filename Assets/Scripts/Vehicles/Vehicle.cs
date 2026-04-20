using UnityEngine;
using VoxelRoad.Game;

namespace VoxelRoad.Vehicles
{
    /// <summary>단일 차량. X축 일방향 주행, 레인 밖 이탈 시 소멸, 플레이어 충돌 시 사망 트리거.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Vehicle : MonoBehaviour
    {
        [SerializeField] private float _boundsPadding = 2f;

        private float _speed;
        private float _direction = 1f;
        private float _leftLimit;
        private float _rightLimit;

        public void Launch(float speed, float direction, float laneSpanX)
        {
            _speed = speed;
            _direction = Mathf.Sign(direction);
            _leftLimit = -laneSpanX * 0.5f - _boundsPadding;
            _rightLimit = laneSpanX * 0.5f + _boundsPadding;

            // 진행 방향 시각화: 오른쪽 이동 시 +X 바라봄
            transform.rotation = Quaternion.Euler(0f, _direction > 0f ? 90f : -90f, 0f);
        }

        private void Update()
        {
            transform.position += new Vector3(_speed * _direction * Time.deltaTime, 0f, 0f);
            float x = transform.position.x;
            if ((_direction > 0f && x > _rightLimit) || (_direction < 0f && x < _leftLimit))
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                GameManager.KillPlayer("vehicle");
            }
        }
    }
}
