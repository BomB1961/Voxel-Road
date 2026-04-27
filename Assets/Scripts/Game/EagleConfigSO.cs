using UnityEngine;

namespace VoxelRoad.Game
{
    [CreateAssetMenu(menuName = "VoxelRoad/EagleConfig", fileName = "EagleConfig")]
    public sealed class EagleConfigSO : ScriptableObject
    {
        [SerializeField] private float _idleTimeoutSeconds = 3f;
        [SerializeField] private float _cameraTrailOffsetZ = 7f;

        public float IdleTimeoutSeconds => _idleTimeoutSeconds;
        public float CameraTrailOffsetZ => _cameraTrailOffsetZ;
    }
}
