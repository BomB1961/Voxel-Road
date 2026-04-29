using System.Collections;
using UnityEngine;
using VoxelRoad.Game;

namespace VoxelRoad.Player
{
    /// <summary>사망 원인별 애니메이션 모션. Animator/Clip 미사용, transform 조작만.</summary>
    public sealed class PlayerDeathAnimator : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [Tooltip("Drown 페이드용. null이면 페이드 스킵.")]
        [SerializeField] private Renderer _renderer;

        [Header("Vehicle/Train Squash")]
        [SerializeField] private float _vehicleDuration = 0.2f;
        [SerializeField] private float _trainDuration = 0.25f;
        [SerializeField] private float _squashY = 0.1f;
        [SerializeField] private float _squashXZ = 1.3f;

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
            // 통나무/차량 자식 상태에서 분리 → 사망 후 부모 변환에 끌려가지 않음
            if (transform.parent != null) transform.SetParent(null, true);

            switch (reason)
            {
                case DeathReason.Vehicle: StartCoroutine(SquashRoutine(_vehicleDuration)); break;
                case DeathReason.Train: StartCoroutine(SquashRoutine(_trainDuration)); break;
                case DeathReason.Drown: StartCoroutine(DrownRoutine()); break;
                case DeathReason.OutOfBounds: StartCoroutine(FallRoutine(true)); break;
                case DeathReason.Idle: StartCoroutine(FallRoutine(false)); break;
            }
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
