using System.Collections;
using UnityEngine;
using VoxelRoad.Game;

namespace VoxelRoad.Player
{
    /// <summary>사망 원인별 애니메이션 모션. Animator/Clip 미사용, transform 조작만.</summary>
    public sealed class PlayerDeathAnimator : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [Tooltip("차량/기차 충돌 모션에 LastImpactSource를 읽기 위함. 같은 GameObject에 있으면 자동 GetComponent.")]
        [SerializeField] private PlayerController _player;
        [Tooltip("Drown 페이드용. null이면 페이드 스킵.")]
        [SerializeField] private Renderer _renderer;

        [Header("Vehicle Death (공중제비 + 포물선)")]
        [Tooltip("차량 진행 방향으로 튕겨 나가는 수평 거리(그리드)")]
        [SerializeField] private float _vehicleKnockbackGrids = 2f;
        [Tooltip("포물선 최고 높이(월드 유닛)")]
        [SerializeField] private float _vehicleArcHeight = 0.6f;
        [Tooltip("비행 페이즈 시간(초)")]
        [SerializeField] private float _vehicleFlightDuration = 0.4f;
        [Tooltip("착지 후 슬라이드 거리(그리드)")]
        [SerializeField] private float _vehicleSlideGrids = 0.3f;
        [Tooltip("착지 후 슬라이드 시간(초)")]
        [SerializeField] private float _vehicleSlideDuration = 0.1f;
        [Tooltip("회전 바퀴 수. 1.25 = 등 대고 누운 자세로 마무리")]
        [SerializeField] private float _vehicleRotationTurns = 1.25f;
        [Tooltip("플레이어 비주얼 몸통 중심의 Y 오프셋(피벗으로부터). 회전 피벗 보정에 사용. 0이면 피벗(발) 기준 회전이라 공전 현상 발생.")]
        [SerializeField] private float _visualCenterY = 0.4f;

        [Header("Side Knockback (점프 중 차량/기차 측면 충돌 → 뒤로 튕김)")]
        [Tooltip("플레이어 forward 반대 방향으로 튕기는 거리(그리드)")]
        [SerializeField] private float _sideKnockbackGrids = 2.5f;
        [Tooltip("포물선 최고 높이(월드 유닛)")]
        [SerializeField] private float _sideKnockbackArcHeight = 1.5f;
        [Tooltip("비행 페이즈 시간(초)")]
        [SerializeField] private float _sideKnockbackFlightDuration = 0.55f;
        [Tooltip("착지 후 슬라이드 거리(그리드)")]
        [SerializeField] private float _sideKnockbackSlideGrids = 0.3f;
        [Tooltip("착지 후 슬라이드 시간(초)")]
        [SerializeField] private float _sideKnockbackSlideDuration = 0.1f;
        [Tooltip("백플립 회전 바퀴 수")]
        [SerializeField] private float _sideKnockbackRotationTurns = 1.0f;

        [Header("Train Death (즉시 압괴 + 직선 슬라이드)")]
        [Tooltip("즉시 납작해지는 시간(초)")]
        [SerializeField] private float _trainSquashDuration = 0.08f;
        [Tooltip("기차 진행 방향으로 미끄러지는 거리(그리드)")]
        [SerializeField] private float _trainSlideGrids = 5f;
        [Tooltip("슬라이드 시간(초). easeOut으로 감속")]
        [SerializeField] private float _trainSlideDuration = 0.42f;
        [Tooltip("슬라이드 중 살짝 기울이는 각도. 끌리는 느낌 연출")]
        [SerializeField] private float _trainTiltDegrees = 5f;

        [Header("Squash Fallback (impact 없을 때)")]
        [SerializeField] private float _squashFallbackDuration = 0.2f;
        [SerializeField] private float _squashY = 0.1f;
        [SerializeField] private float _squashXZ = 1.3f;

        [Header("Flat Scale (Vehicle/Train 공통 납작 스케일)")]
        [SerializeField] private float _flatScaleX = 1.2f;
        [SerializeField] private float _flatScaleY = 0.5f;
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
                    if (impact == null) StartCoroutine(SquashRoutine(_squashFallbackDuration));
                    else if (_player != null && _player.LastImpactIsSideHit) StartCoroutine(SideKnockbackRoutine());
                    else StartCoroutine(VehicleDeathRoutine(impact));
                    break;
                case DeathReason.Train:
                    if (impact == null) StartCoroutine(SquashRoutine(_squashFallbackDuration));
                    else if (_player != null && _player.LastImpactIsSideHit) StartCoroutine(SideKnockbackRoutine());
                    else StartCoroutine(TrainDeathRoutine(impact));
                    break;
                case DeathReason.Drown: StartCoroutine(DrownRoutine()); break;
                case DeathReason.OutOfBounds: StartCoroutine(FallRoutine(true)); break;
                case DeathReason.Idle: StartCoroutine(FallRoutine(false)); break;
            }
        }

        private IEnumerator VehicleDeathRoutine(Transform impactSource)
        {
            Vector3 vehicleDir = impactSource.forward;
            vehicleDir.y = 0f;
            if (vehicleDir.sqrMagnitude > 1e-4f) vehicleDir = vehicleDir.normalized;
            else vehicleDir = Vector3.right;

            // 차량이 +X면 -Z축, -X면 +Z축. 우수좌표계 + 진행 방향 forward somersault.
            Vector3 rotationAxis = vehicleDir.x > 0f ? Vector3.back : Vector3.forward;
            float totalRotationDeg = 360f * _vehicleRotationTurns;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 startScale = transform.localScale;
            Vector3 flatScale = new Vector3(
                startScale.x * _flatScaleX,
                startScale.y * _flatScaleY,
                startScale.z * _flatScaleZ);

            // 회전 피벗 보정: 비주얼 몸통 중심이 궤적을 따르도록.
            // 피벗(발) 기준 회전 시 발이 차량 위치에 있으면 몸통이 차량 주위를 공전하는 효과 발생.
            // 이를 막기 위해 매 프레임 (몸통 중심 - 회전된 오프셋) = 피벗 위치로 역산.
            Vector3 pivotOffset = new Vector3(0f, _visualCenterY, 0f);

            Vector3 centerStart = startPos + Vector3.up * _visualCenterY;
            Vector3 centerEnd = new Vector3(
                startPos.x + vehicleDir.x * _vehicleKnockbackGrids,
                _visualCenterY,
                startPos.z + vehicleDir.z * _vehicleKnockbackGrids);

            // === 페이즈 1: 비행 (포물선 + 회전, 몸통 중심 기준) ===
            float elapsed = 0f;
            while (elapsed < _vehicleFlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _vehicleFlightDuration);
                Vector3 center = Vector3.Lerp(centerStart, centerEnd, t);
                center.y += 4f * _vehicleArcHeight * t * (1f - t);
                Quaternion currentRot = Quaternion.AngleAxis(totalRotationDeg * t, rotationAxis) * startRot;
                transform.rotation = currentRot;
                transform.position = center - currentRot * pivotOffset;
                yield return null;
            }
            Quaternion flightEndRot = Quaternion.AngleAxis(totalRotationDeg, rotationAxis) * startRot;
            transform.rotation = flightEndRot;
            transform.position = centerEnd - flightEndRot * pivotOffset;

            // === 페이즈 2: 슬라이드 + 납작 ===
            Vector3 slideStartCenter = centerEnd;
            Vector3 slideEndCenter = new Vector3(
                centerEnd.x + vehicleDir.x * _vehicleSlideGrids,
                _visualCenterY,
                centerEnd.z + vehicleDir.z * _vehicleSlideGrids);

            elapsed = 0f;
            while (elapsed < _vehicleSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _vehicleSlideDuration);
                float eased = t * (2f - t); // easeOut
                Vector3 center = Vector3.Lerp(slideStartCenter, slideEndCenter, eased);
                transform.position = center - flightEndRot * pivotOffset;
                transform.localScale = Vector3.Lerp(startScale, flatScale, t);
                yield return null;
            }
            transform.position = slideEndCenter - flightEndRot * pivotOffset;
            transform.localScale = flatScale;
            transform.rotation = flightEndRot;
        }

        private IEnumerator SideKnockbackRoutine()
        {
            // 플레이어 정면 반대 방향으로 튕김 (= 뛰어들어간 방향의 반대로 백플립).
            // 차량·기차 측면에 정면으로 박은 직후의 운동량 보존 묘사.
            Vector3 knockbackDir = -transform.forward;
            knockbackDir.y = 0f;
            if (knockbackDir.sqrMagnitude > 1e-4f) knockbackDir = knockbackDir.normalized;
            else knockbackDir = Vector3.back;

            // 백플립 회전축 = -player.right. 머리가 뒤로 넘어가는 방향.
            Vector3 rotationAxis = -transform.right;
            float totalRotationDeg = 360f * _sideKnockbackRotationTurns;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 startScale = transform.localScale;
            Vector3 flatScale = new Vector3(
                startScale.x * _flatScaleX,
                startScale.y * _flatScaleY,
                startScale.z * _flatScaleZ);

            // 회전 피벗 보정 (VehicleDeathRoutine과 동일 방식)
            Vector3 pivotOffset = new Vector3(0f, _visualCenterY, 0f);

            Vector3 centerStart = startPos + Vector3.up * _visualCenterY;
            Vector3 centerEnd = new Vector3(
                startPos.x + knockbackDir.x * _sideKnockbackGrids,
                _visualCenterY,
                startPos.z + knockbackDir.z * _sideKnockbackGrids);

            // === 페이즈 1: 비행 (포물선 + 백플립, 몸통 중심 기준) ===
            float elapsed = 0f;
            while (elapsed < _sideKnockbackFlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _sideKnockbackFlightDuration);
                Vector3 center = Vector3.Lerp(centerStart, centerEnd, t);
                center.y += 4f * _sideKnockbackArcHeight * t * (1f - t);
                Quaternion currentRot = Quaternion.AngleAxis(totalRotationDeg * t, rotationAxis) * startRot;
                transform.rotation = currentRot;
                transform.position = center - currentRot * pivotOffset;
                yield return null;
            }
            Quaternion flightEndRot = Quaternion.AngleAxis(totalRotationDeg, rotationAxis) * startRot;
            transform.rotation = flightEndRot;
            transform.position = centerEnd - flightEndRot * pivotOffset;

            // === 페이즈 2: 슬라이드 + 납작 ===
            Vector3 slideStartCenter = centerEnd;
            Vector3 slideEndCenter = new Vector3(
                centerEnd.x + knockbackDir.x * _sideKnockbackSlideGrids,
                _visualCenterY,
                centerEnd.z + knockbackDir.z * _sideKnockbackSlideGrids);

            elapsed = 0f;
            while (elapsed < _sideKnockbackSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _sideKnockbackSlideDuration);
                float eased = t * (2f - t);
                Vector3 center = Vector3.Lerp(slideStartCenter, slideEndCenter, eased);
                transform.position = center - flightEndRot * pivotOffset;
                transform.localScale = Vector3.Lerp(startScale, flatScale, t);
                yield return null;
            }
            transform.position = slideEndCenter - flightEndRot * pivotOffset;
            transform.localScale = flatScale;
            transform.rotation = flightEndRot;
        }

        private IEnumerator TrainDeathRoutine(Transform impactSource)
        {
            Vector3 trainDir = impactSource.forward;
            trainDir.y = 0f;
            if (trainDir.sqrMagnitude > 1e-4f) trainDir = trainDir.normalized;
            else trainDir = Vector3.right;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 startScale = transform.localScale;
            Vector3 flatScale = new Vector3(
                startScale.x * _flatScaleX,
                startScale.y * _flatScaleY,
                startScale.z * _flatScaleZ);

            // === 페이즈 1: 즉시 압괴 ===
            float elapsed = 0f;
            while (elapsed < _trainSquashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _trainSquashDuration);
                transform.localScale = Vector3.Lerp(startScale, flatScale, t);
                yield return null;
            }
            transform.localScale = flatScale;

            // 슬라이드 중 살짝 기울임. 진행 방향과 수직인 수평축으로 회전.
            Vector3 tiltAxis = trainDir.x > 0f ? Vector3.back : Vector3.forward;
            Quaternion tiltRot = Quaternion.AngleAxis(_trainTiltDegrees, tiltAxis) * startRot;

            // === 페이즈 2: 직선 슬라이드 ===
            Vector3 slideStartPos = transform.position;
            Vector3 slideEndPos = new Vector3(
                slideStartPos.x + trainDir.x * _trainSlideGrids,
                0f,
                slideStartPos.z + trainDir.z * _trainSlideGrids);

            elapsed = 0f;
            while (elapsed < _trainSlideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _trainSlideDuration);
                float eased = t * (2f - t); // easeOut: 초반 빠르게 → 후반 감속
                Vector3 pos = Vector3.Lerp(slideStartPos, slideEndPos, eased);
                pos.y = 0f;
                transform.position = pos;
                transform.rotation = Quaternion.Slerp(startRot, tiltRot, t);
                yield return null;
            }
            transform.position = slideEndPos;
            transform.rotation = tiltRot;
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
