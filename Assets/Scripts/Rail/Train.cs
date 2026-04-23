using UnityEngine;

namespace VoxelRoad.Rail
{
    /// <summary>철길 기차. 월드 X 직선 이동, 레인 경계 벗어나면 Destroy.
    /// 충돌 판정은 Player 쪽에서 GetComponentInParent&lt;Train&gt;()로 감지.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Train : MonoBehaviour
    {
        [SerializeField] private float _boundsPadding = 3f;
        private float _speed;
        private float _direction;
        private float _leftLimit;
        private float _rightLimit;

        public void Launch(float speed, float direction, float laneSpanX)
        {
            _speed = speed;
            _direction = Mathf.Sign(direction);
            float halfLaneSpan = laneSpanX * 0.5f;
            _leftLimit = -halfLaneSpan - _boundsPadding;
            _rightLimit = halfLaneSpan + _boundsPadding;
        }

        private void Update()
        {
            transform.position += new Vector3(_speed * _direction * Time.deltaTime, 0f, 0f);
            float x = transform.position.x;
            if ((_direction > 0f && x > _rightLimit) || (_direction < 0f && x < _leftLimit))
                Destroy(gameObject);
        }
    }
}
