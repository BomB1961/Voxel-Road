using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>레인 공통 라이프사이클. 향후 River/Rail도 동일 인터페이스 준수.</summary>
    public interface ILane
    {
        LaneType Type { get; }
        int ZIndex { get; }
        Transform Transform { get; }
        void Initialize(int zIndex, float laneSpanX);
        void Despawn();
    }
}
