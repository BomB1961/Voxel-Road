using System.Collections;
using TMPro;
using UnityEngine;

namespace VoxelRoad.UI
{
    /// <summary>5자리 숫자를 자리별 TMP로 분리 표시. 갱신 시 변경된 자리만 Pop 코루틴 발동.</summary>
    public sealed class PerDigitScoreDisplay : MonoBehaviour
    {
        private const string DigitFormat = "{0}";
        private const int DigitCount = 5;

        [SerializeField] private ScoreTracker _tracker;
        [SerializeField] private TMP_Text[] _digits = new TMP_Text[DigitCount]; // 0=10000자리(좌측), 4=1자리(우측)
        [SerializeField] private float _peakScale = 1.2f;
        [SerializeField] private float _popDuration = 0.15f;

        private readonly char[] _newDigits = new char[DigitCount];
        private readonly char[] _oldDigits = new char[DigitCount];
        private Coroutine[] _routines;

        private void Awake()
        {
            if (_tracker == null || _digits == null || _digits.Length != DigitCount)
            {
                Debug.LogError("[PerDigitScoreDisplay] 필수 참조 미할당 또는 자리수 불일치");
                enabled = false;
                return;
            }
            for (int i = 0; i < DigitCount; i++)
            {
                if (_digits[i] == null)
                {
                    Debug.LogError($"[PerDigitScoreDisplay] _digits[{i}] 미할당");
                    enabled = false;
                    return;
                }
            }
            _routines = new Coroutine[DigitCount];
            for (int i = 0; i < DigitCount; i++) _oldDigits[i] = '0';
        }

        private void OnEnable()
        {
            if (_tracker != null) _tracker.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.OnScoreChanged -= HandleScoreChanged;
        }

        private void Start()
        {
            HandleScoreChanged(_tracker.Score);
        }

        private void HandleScoreChanged(int score)
        {
            FormatDigits(score, _newDigits);
            for (int i = 0; i < DigitCount; i++)
            {
                int digitValue = _newDigits[i] - '0';
                _digits[i].SetText(DigitFormat, digitValue);
                if (_newDigits[i] != _oldDigits[i])
                {
                    if (_routines[i] != null) StopCoroutine(_routines[i]);
                    _routines[i] = StartCoroutine(PopRoutine(i));
                }
                _oldDigits[i] = _newDigits[i];
            }
        }

        private static void FormatDigits(int score, char[] buffer)
        {
            int v = score < 0 ? 0 : score;
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                buffer[i] = (char)('0' + v % 10);
                v /= 10;
            }
            // v > 0 이면 6자리 이상 → 좌측 잘림. 게임 길이 고려 시 사실상 도달 불가.
        }

        private IEnumerator PopRoutine(int index)
        {
            var rt = (RectTransform)_digits[index].transform;
            float elapsed = 0f;
            while (elapsed < _popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _popDuration);
                float curve = Mathf.Sin(t * Mathf.PI);
                float scale = Mathf.Lerp(1f, _peakScale, curve);
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            _routines[index] = null;
        }
    }
}
