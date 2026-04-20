using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>모든 레인의 공통 기반. 타일 크기 1m 기준 X축으로 laneSpanX 칸 폭, Z축 1칸.</summary>
    public abstract class BaseLane : MonoBehaviour, ILane
    {
        protected int _zIndex;
        protected float _laneSpanX;

        public abstract LaneType Type { get; }
        public int ZIndex => _zIndex;
        public Transform Transform => transform;

        public virtual void Initialize(int zIndex, float laneSpanX)
        {
            _zIndex = zIndex;
            _laneSpanX = laneSpanX;
            transform.position = new Vector3(0f, 0f, zIndex);
            Build();
        }

        /// <summary>구체 클래스가 레인 비주얼을 구성.</summary>
        protected abstract void Build();

        public virtual void Despawn()
        {
            Destroy(gameObject);
        }
    }
}
