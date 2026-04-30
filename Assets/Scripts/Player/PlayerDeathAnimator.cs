using System.Collections;
using UnityEngine;
using VoxelRoad.Game;

namespace VoxelRoad.Player
{
    /// <summary>사망 원인별 애니메이션 모션. Animator/Clip 미사용, transform 조작만.</summary>
    public sealed class PlayerDeathAnimator : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [Tooltip("차량/기차 흡착 모션에 LastImpactSource를 읽기 위함. 같은 GameObject에 있으면 자동 GetComponent.")]
        [SerializeField] private PlayerController _player;
        [Tooltip("Drown 페이드용. null이면 페이드 스킵.")]
        [SerializeField] private Renderer _renderer;

        [Header("Vehicle/Train Squash")]
        [SerializeField] private float _vehicleDuration = 0.2f;
        [SerializeField] private float _trainDuration = 0.25f;
        [SerializeField] private float _squashY = 0.1f;
        [SerializeField] private float _squashXZ = 1.3f;

        [Header("Vehicle Death (옆/앞으로 쓰러짐 + 바닥 흡착)")]
        [Tooltip("|player.forward.z|이 이 값보다 크면 정면(+Z) 상태로 판정 → 옆으로(world X) 쓰러짐. 미만이면 회전 상태 → 자기 forward 방향으로 앞으로 쓰러짐.")]
        [SerializeField] private float _facingDefaultThreshold = 0.7f;
        [Tooltip("쓰러질 때 진행 방향으로 밀려나는 거리(월드 유닛)")]
        [SerializeField] private float _knockbackDistance = 0.5f;
        [Tooltip("쓰러진 후 X축 스케일 (월드 X 또는 player local X)")]
        [SerializeField] private float _flatScaleX = 1.2f;
        [Tooltip("쓰러진 후 Y축 스케일")]
        [SerializeField] private float _flatScaleY = 0.5f;
        [Tooltip("쓰러진 후 Z축 스케일")]
        [SerializeField] private float _flatScaleZ = 1.2f;

        [Header("Drown")]
        [SerializeField] private float _drownDuration = 1.5f;
        [SerializeField] private float _drownDepth = 2.5f;
        [Tooltip("머터리얼 alpha 페이드 활성화. URP transparent 머터리얼 필요.")]
        [SerializeField] private bool _drownFade = false;

        [Header("Fall Over")]
        [SerializeField] private float _fallDuration = 0.4f;

        private Material _materialInstance;

        private void Awake()
        {
            if (_gameManager == null)
            {
                Debug.LogError("[PlayerDeathAnimator] _gameManager 미할당");
                enabled = false;
                return;
            }
            if (_player == null) _player = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (_gameManager != null) _gameManager.OnPlayerDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_gameManager != null) _gameManager.OnPlayerDied -= HandleDied;
        }

        private void HandleDied(DeathReason reason)
        {
            if (transform.parent != null) transform.SetParent(null, true);

            Transform impact = _player != null ? _player.LastImpactSource : null;
            switch (reason)
            {
                case DeathReason.Vehicle:
                    if (impact != null) StartCoroutine(VehicleDeathRoutine(impact, _vehicleDuration));
                    else StartCoroutine(SquashRoutine(_vehicleDuration));
                    break;
                case DeathReason.Train:
                    if (impact != null) StartCoroutine(VehicleDeathRoutine(impact, _trainDuration));
                    else StartCoroutine(SquashRoutine(_trainDuration));
                    break;
                case DeathReason.Drown: StartCoroutine(DrownRoutine()); break;
                case DeathReason.OutOfBounds: StartCoroutine(FallRoutine(true)); break;
                case DeathReason.Idle: StartCoroutine(FallRoutine(false)); break;
            }
        }

        private IEnumerator VehicleDeathRoutine(Transform impactSource, float duration)
        {
            // 차량 진행 방향(±X) 추출
            Vector3 vehicleDir = impactSource.forward;
            vehicleDir.y = 0f;
            if (vehicleDir.sqrMagnitude > 1e-4f) vehicleDir = vehicleDir.normalized;
            else vehicleDir = Vector3.right;

            Quaternion startRot = transform.rotation;
            Quaternion endRot;
            Vector3 pushDir;

            // 플레이어 정면 판정: |forward.z| 큰 → 정면(+Z) 상태, 작은 → 회전 상태
            bool facingDefault = Mathf.Abs(transform.forward.z) >= _facingDefaultThreshold;

            if (facingDefault)
            {
                // 옆으로 쓰러짐 — 머리가 차량 진행 방향 쪽으로 가도록 world Z축 회전.
                // 캐릭터 up(+Y)을 vehicleDir(±X)로 회전: vehicleDir.x>0 -> -90°, <0 -> +90°
                float angle = vehicleDir.x > 0f ? -90f : 90f;
                endRot = Quaternion.AngleAxis(angle, Vector3.forward) * startRot;
                pushDir = vehicleDir;
            }
            else
            {
                // 앞으로 쓰러짐 — 자기 forward 축으로 엎어짐 (local right axis 90°).
                endRot = startRot * Quaternion.Euler(90f, 0f, 0f);
                Vector3 fwd = transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-4f) fwd = fwd.normalized;
                else fwd = vehicleDir;
                pushDir = fwd;
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + pushDir * _knockbackDistance;
            endPos.y = 0f; // 바닥 흡착

            Vector3 startScale = transform.localScale;
            Vector3 endScale = new Vector3(
                startScale.x * _flatScaleX,
                startScale.y * _flatScaleY,
                startScale.z * _flatScaleZ);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
                transform.position = Vector3.Lerp(startPos, endPos, eased);
                transform.localScale = Vector3.Lerp(startScale, endScale, eased);
                yield return null;
            }
            transform.rotation = endRot;
            transform.position = endPos;
            transform.localScale = endScale;
        }

        private IEnumerator SquashRoutine(float duration)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = new Vector3(startScale.x * _squashXZ, startScale.y * _squashY, startScale.z * _squashXZ);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.Lerp(startScale, endScale, eased);
                yield return null;
            }
            transform.localScale = endScale;
        }

        private IEnumerator DrownRoutine()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * _drownDepth;
            Quaternion startRot = transform.rotation;
            Quaternion endRot = startRot * Quaternion.Euler(15f, 0f, 10f);

            Material mat = null;
            Color startColor = Color.white;
            string colorProp = null;
            if (_drownFade && _renderer != null)
            {
                _materialInstance = new Material(_renderer.sharedMaterial);
                _renderer.material = _materialInstance;
                mat = _materialInstance;
                if (mat.HasProperty("_BaseColor")) { colorProp = "_BaseColor"; startColor = mat.GetColor(colorProp); }
                else if (mat.HasProperty("_Color")) { colorProp = "_Color"; startColor = mat.GetColor(colorProp); }
            }

            float elapsed = 0f;
            while (elapsed < _drownDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _drownDuration);
                float eased = t * t;
                transform.position = Vector3.Lerp(startPos, endPos, eased);
                transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
                if (mat != null && colorProp != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, eased);
                    mat.SetColor(colorProp, c);
                }
                yield return null;
            }
        }

        private IEnumerator FallRoutine(bool forward)
        {
            Quaternion startRot = transform.rotation;
            float angle = forward ? 90f : -90f;
            Quaternion endRot = startRot * Quaternion.Euler(angle, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < _fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fallDuration);
                float eased = 1f - (1f - t) * (1f - t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
                yield return null;
            }
            transform.rotation = endRot;
        }

        private void OnDestroy()
        {
            if (_materialInstance != null) Destroy(_materialInstance);
        }
    }
}
