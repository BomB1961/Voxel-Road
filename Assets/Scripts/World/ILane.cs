using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>레인 공통 라이프사이클. 향후 River/Rail도 동일 인터페이스 준수.</summary>
    public interface ILane
    {
        LaneType Type { get; }
        int ZIndex { get; }
        Transform Transform { get; }
        /// <summary>레인 초기화. difficultyMultiplier=1이면 기본 난이도, 1보다 크면 더 어려움(속도↑·빈도↑).</summary>
        void Initialize(int zIndex, float laneSpanX, float difficultyMultiplier = 1f);
        void Despawn();
    }
}
