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
        [Tooltip("추적 대상 Player Transform. Inspector 에서 직접 드래그 연결.")]
        [SerializeField] private Transform _playerTransform;
        [Tooltip("Player 기준 카메라 위치. Y는 높이, Z는 뒤 거리. CrossyRoadCameraExtension 이 사용.")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 20f, -6f);

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

            if (vcam.Follow == null && _playerTransform != null)
                vcam.Follow = _playerTransform;

            var lens = vcam.Lens;
            lens.FieldOfView = _fieldOfView;
            lens.OrthographicSize = _orthographicSize;
            lens.ModeOverride = _orthographic ? LensSettings.OverrideModes.Orthographic : LensSettings.OverrideModes.None;
            vcam.Lens = lens;

            // Body: 커스텀 CrossyRoadCameraExtension 이 전담 (CinemachineFollow 불필요 — Body 결과를 덮어쓰므로 중복).

            // 기존 Body/Aim 컴포넌트 제거 (Extension 이 Body 단계 점유)
            RemoveIfPresent<CinemachineFollow>();
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
            if (!Application.isPlaying)
            {
                // OnValidate/렌더/물리 콜백 중 DestroyImmediate 금지 → delayCall 로 지연.
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (c != null) DestroyImmediate(c);
                };
                return;
            }
#endif
            Destroy(c);
        }
    }
}
