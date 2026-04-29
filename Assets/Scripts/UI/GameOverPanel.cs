using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoxelRoad.Game;

namespace VoxelRoad.UI
{
    /// <summary>사망 시 활성화되는 패널. 최종 점수·신기록·재시작 버튼.</summary>
    public sealed class GameOverPanel : MonoBehaviour
    {
        private const string FinalScoreFormat = "SCORE {0:D5}";
        private const string BestScoreFormat = "HI-SCORE {0:D5}";

        [SerializeField] private GameManager _gameManager;
        [SerializeField] private ScoreTracker _scoreTracker;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _finalScoreText;
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private GameObject _newRecordBadge;
        [SerializeField] private Button _restartButton;

        [Header("Death Motion 후 패널 표시 지연 (초)")]
        [SerializeField] private float _delayVehicle = 0.7f;
        [SerializeField] private float _delayTrain = 0.7f;
        [SerializeField] private float _delayDrown = 1.5f;
        [SerializeField] private float _delayFallOver = 0.5f; // OutOfBounds, Idle

        private void Awake()
        {
            if (_gameManager == null || _scoreTracker == null || _root == null
                || _finalScoreText == null || _bestScoreText == null
                || _newRecordBadge == null || _restartButton == null)
            {
                Debug.LogError("[GameOverPanel] 필수 참조 미할당");
                enabled = false;
                return;
            }

            _root.SetActive(false);
            _newRecordBadge.SetActive(false);
            _restartButton.onClick.AddListener(Restart);
        }

        private void OnEnable()
        {
            if (_gameManager != null)
                _gameManager.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (_gameManager != null)
                _gameManager.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandlePlayerDied(DeathReason reason)
        {
            _scoreTracker.CommitBestScore();

            _finalScoreText.SetText(FinalScoreFormat, _scoreTracker.Score);
            _bestScoreText.SetText(BestScoreFormat, _scoreTracker.BestScore);
            _newRecordBadge.SetActive(_scoreTracker.IsNewRecord);

            float delay = GetDelayForReason(reason);
            if (delay <= 0f) _root.SetActive(true);
            else StartCoroutine(ShowAfterDelay(delay));
        }

        private float GetDelayForReason(DeathReason reason)
        {
            switch (reason)
            {
                case DeathReason.Vehicle: return _delayVehicle;
                case DeathReason.Train: return _delayTrain;
                case DeathReason.Drown: return _delayDrown;
                case DeathReason.OutOfBounds:
                case DeathReason.Idle: return _delayFallOver;
                default: return 0f;
            }
        }

        private IEnumerator ShowAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _root.SetActive(true);
        }

        private static void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
