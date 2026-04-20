using UnityEngine;

namespace VoxelRoad.CameraSystem
{
    /// <summary>
    /// Crossy Road 스타일 쿼터뷰 카메라.
    /// X/Y는 고정, Z는 타깃의 최대 도달 Z를 따라 전진 방향으로만 이동 (후진 불가).
    /// </summary>
    public sealed class CameraFollower : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 12f, -8f);
        [SerializeField] private float _followSpeed = 3.5f;
        [SerializeField] private bool _lockX = true;

        private float _maxTargetZ;
        private float _fixedX;

        private void Awake()
        {
            if (_target != null)
            {
                _maxTargetZ = _target.position.z;
                _fixedX = _target.position.x + _offset.x;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // 타깃 Z 최대값 갱신 (전진만 허용)
            if (_target.position.z > _maxTargetZ)
            {
                _maxTargetZ = _target.position.z;
            }

            Vector3 desired = new Vector3(
                _lockX ? _fixedX : _target.position.x + _offset.x,
                _offset.y,
                _maxTargetZ + _offset.z
            );

            transform.position = Vector3.Lerp(transform.position, desired, _followSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            if (target != null)
            {
                _maxTargetZ = target.position.z;
                _fixedX = target.position.x + _offset.x;
            }
        }
    }
}
