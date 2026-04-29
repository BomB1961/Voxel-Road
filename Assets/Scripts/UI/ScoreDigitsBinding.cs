using TMPro;
using UnityEngine;

namespace VoxelRoad.UI
{
    /// <summary>ScoreTracker.OnScoreChanged를 구독해 자기 TMP_Text에 5자리 0-padding 숫자만 표시. SCORE 라벨과 분리되어 Pop 효과 대상이 숫자 부분만 되도록 함.</summary>
    public sealed class ScoreDigitsBinding : MonoBehaviour
    {
        private const string DigitsFormat = "{0:00000}";

        [SerializeField] private ScoreTracker _tracker;
        [SerializeField] private TMP_Text _digitsText;

        private void Awake()
        {
            if (_tracker == null || _digitsText == null)
            {
                Debug.LogError("[ScoreDigitsBinding] 필수 참조 미할당");
                enabled = false;
                return;
            }
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
            _digitsText.SetText(DigitsFormat, score);
        }
    }
}
