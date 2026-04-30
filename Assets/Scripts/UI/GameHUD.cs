using TMPro;
using UnityEngine;

namespace VoxelRoad.UI
{
    /// <summary>인게임 상단 HUD. SCORE 표시. HI-SCORE는 NewBestBanner / GameOverPanel에서 처리.</summary>
    public sealed class GameHUD : MonoBehaviour
    {
        // TMP의 SetText 포매터는 C# string.Format과 다름. {0:D5} 대신 {0:00000} 사용해야 5자리 0-padding 정수가 됨.
        private const string ScoreFormat = "SCORE {0:00000}";

        [SerializeField] private ScoreTracker _scoreTracker;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private UIScorePop _scorePop;

        private void Awake()
        {
            if (_scoreTracker == null || _scoreText == null)
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
        }

        private void OnDisable()
        {
            if (_scoreTracker == null) return;
            _scoreTracker.OnScoreChanged -= HandleScoreChanged;
        }

        private void HandleScoreChanged(int score)
        {
            _scoreText.SetText(ScoreFormat, score);
            if (_scorePop != null) _scorePop.Pop();
        }
    }
}
