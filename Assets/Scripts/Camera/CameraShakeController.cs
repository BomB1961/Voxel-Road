// Assets/Scripts/Camera/CameraShakeController.cs
// CinemachineBasicMultiChannelPerlin 진폭을 일정 시간 상승 → 자동 복원하는 싱글톤 셰이커.
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace VoxelRoad.CameraSystem
{
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CameraShakeController : MonoBehaviour
    {
        public static CameraShakeController Instance { get; private set; }

        private CinemachineBasicMultiChannelPerlin _perlin;
        private float _baseAmplitude;

        private void Awake()
        {
            Instance = this;
            AcquirePerlin();
        }

        private void Start()
        {
            if (_perlin == null) AcquirePerlin();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void AcquirePerlin()
        {
            _perlin = GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (_perlin == null) return;
            // 상시 흔들림 금지: 기본 진폭은 항상 0으로 고정. 셰이크 종료 시 0으로 복원.
            _baseAmplitude = 0f;
            _perlin.AmplitudeGain = 0f;
        }

        public void Shake(float intensity, float duration)
        {
            if (_perlin == null) AcquirePerlin();
            if (_perlin == null) return;
            StopAllCoroutines();
            StartCoroutine(ShakeRoutine(intensity, duration));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            _perlin.AmplitudeGain = intensity;
            yield return new WaitForSeconds(duration);
            _perlin.AmplitudeGain = _baseAmplitude;
        }
    }
}
