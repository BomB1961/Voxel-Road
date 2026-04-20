// Assets/Scripts/Camera/CrossyRoadCameraSetup.cs
// CinemachineCamera에 붙여 Main Camera 각도/FOV와 Follow·Body·Extension을 자동 구성.
using UnityEngine;
using Unity.Cinemachine;

namespace VoxelRoad.CameraSystem
{
    [ExecuteAlways]
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CrossyRoadCameraSetup : MonoBehaviour
    {
        [Header("Main Camera")]
        [Tooltip("Crossy Road 구도: yaw=0 정면, pitch≈30°")]
        [SerializeField] private Vector3 _mainCameraEuler = new Vector3(60f, 0f, 0f);
        [SerializeField] private bool _orthographic = true;
        [SerializeField] private float _orthographicSize = 5f;
        [SerializeField] private float _fieldOfView = 40f;

        [Header("Follow")]
        [SerializeField] private string _playerTag = "Player";
        [Tooltip("Player 기준 카메라 위치. Y는 높이, Z는 뒤 거리.")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 20f, -6f);
        [SerializeField] private Vector3 _positionDamping = new Vector3(1.5f, 0f, 0.3f);

        private void OnValidate() { if (isActiveAndEnabled) Apply(); }
        private void Start() { Apply(); }

        public void Apply()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.fieldOfView = _fieldOfView;
                mainCam.orthographic = _orthographic;
                mainCam.orthographicSize = _orthographicSize;
                if (mainCam.GetComponent<CinemachineBrain>() == null)
                    mainCam.gameObject.AddComponent<CinemachineBrain>();
            }

            var vcam = GetComponent<CinemachineCamera>();
            if (vcam == null) return;

            // vcam 자체 회전이 Main Camera 회전으로 반영됨 (Aim = Do Nothing)
            transform.rotation = Quaternion.Euler(_mainCameraEuler);

            if (vcam.Follow == null && !string.IsNullOrEmpty(_playerTag))
            {
                var playerGo = GameObject.FindGameObjectWithTag(_playerTag);
                if (playerGo != null) vcam.Follow = playerGo.transform;
            }

            var lens = vcam.Lens;
            lens.FieldOfView = _fieldOfView;
            lens.OrthographicSize = _orthographicSize;
            lens.ModeOverride = _orthographic ? LensSettings.OverrideModes.Orthographic : LensSettings.OverrideModes.None;
            vcam.Lens = lens;

            // Body: CinemachineFollow (v3 Transposer 계승)
            var follow = GetComponent<CinemachineFollow>();
            if (follow == null) follow = gameObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = _followOffset;
            var tracker = follow.TrackerSettings;
            tracker.PositionDamping = _positionDamping;
            follow.TrackerSettings = tracker;

            // Aim: Do Nothing — 기존 Aim 컴포넌트 제거
            RemoveIfPresent<CinemachineRotationComposer>();
            RemoveIfPresent<CinemachineHardLookAt>();
            RemoveIfPresent<CinemachinePanTilt>();
            RemoveIfPresent<CinemachineRotateWithFollowTarget>();

            // Extension + Shake
            var ext = GetComponent<CrossyRoadCameraExtension>();
            if (ext == null) ext = gameObject.AddComponent<CrossyRoadCameraExtension>();
            if (vcam.Follow != null) ext.Player = vcam.Follow;
            ext.FollowOffset = _followOffset;

            if (GetComponent<CameraShakeController>() == null)
                gameObject.AddComponent<CameraShakeController>();

            if (GetComponent<CinemachineBasicMultiChannelPerlin>() == null)
                gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
        }

        private void RemoveIfPresent<T>() where T : Component
        {
            var c = GetComponent<T>();
            if (c == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(c); return; }
#endif
            Destroy(c);
        }
    }
}
