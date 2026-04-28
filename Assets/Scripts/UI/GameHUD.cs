using TMPro;
using UnityEngine;

namespace VoxelRoad.UI
{
    /// <summary>인게임 상단 HUD. SCORE(좌측 상단) + HI-SCORE(우측 상단). 5자리 0-padding.</summary>
    public sealed class GameHUD : MonoBehaviour
    {
        private const string ScoreFormat = "SCORE {0:D5}";
        private const string BestFormat = "HI {0:D5}";

        [SerializeField] private ScoreTracker _scoreTracker;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private UIScorePop _scorePop;

        private void Awake()
        {
            if (_scoreTracker == null || _scoreText == null || _bestScoreText == null)
            {
                Debug.LogError("[GameHUD] 필수 참조 미할당");
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (_scoreTracker == null) return;
            _scoreTracker.OnScoreChanged += HandleScoreChanged;
            _scoreTracker.OnBestScoreChanged += HandleBestScoreChanged;
        }

        private void OnDisable()
        {
            if (_scoreTracker == null) return;
            _scoreTracker.OnScoreChanged -= HandleScoreChanged;
            _scoreTracker.OnBestScoreChanged -= HandleBestScoreChanged;
        }

        private void HandleScoreChanged(int score)
        {
            _scoreText.SetText(ScoreFormat, score);
            if (_scorePop != null) _scorePop.Pop();
        }

        private void HandleBestScoreChanged(int best)
        {
            _bestScoreText.SetText(BestFormat, best);
        }
    }
}
