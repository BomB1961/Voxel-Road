using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoxelRoad.Game;

namespace VoxelRoad.UI
{
    /// <summary>사망 시 활성화되는 패널. 최종 점수·재시작 버튼. 신기록 갱신 알림은 인-게임 NewBestBanner에서 처리.</summary>
    public sealed class GameOverPanel : MonoBehaviour
    {
        // TMP의 SetText 포매터는 C# string.Format과 다름. {0:D5}는 인식 안 되어 소수점 1자리 fallback 발생.
        // {0:00000} 사용해야 5자리 0-padding 정수로 출력. ScoreCard와 동일 형식.
        private const string FinalScoreFormat = "SCORE {0:00000}";
        private const string BestScoreFormat = "BEST SCORE {0:00000}";

        [SerializeField] private GameManager _gameManager;
        [SerializeField] private ScoreTracker _scoreTracker;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _finalScoreText;
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private Button _restartButton;

        [Header("Death Motion 후 패널 표시 지연 (초)")]
        [SerializeField] private float _delayVehicle = 0.5f;
        [SerializeField] private float _delayTrain = 0.5f;
        [SerializeField] private float _delayDrown = 1.5f;
        [SerializeField] private float _delayFallOver = 0.5f; // OutOfBounds, Idle

        private void Awake()
        {
            if (_gameManager == null || _scoreTracker == null || _root == null
                || _finalScoreText == null || _bestScoreText == null
                || _restartButton == null)
            {
                Debug.LogError("[GameOverPanel] 필수 참조 미할당");
                enabled = false;
                return;
            }

            _root.SetActive(false);
            _restartButton.onClick.AddListener(Restart);

            // _root가 self를 가리키므로 SetActive(false) 직후 자기 자신이 비활성화 → OnEnable/OnDisable 경로로
            // 구독하면 죽음 이벤트가 도달하지 않음. Awake에서 직접 구독하고 OnDestroy에서 해제.
            _gameManager.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
                _gameManager.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandlePlayerDied(DeathReason reason)
        {
            _scoreTracker.CommitBestScore();

            _finalScoreText.SetText(FinalScoreFormat, _scoreTracker.Score);
            _bestScoreText.SetText(BestScoreFormat, _scoreTracker.BestScore);

            float delay = GetDelayForReason(reason);
            if (delay <= 0f) _root.SetActive(true);
            // _root가 self를 가리켜 Awake에서 비활성화 → this.StartCoroutine 불가.
            // 활성 상태인 _gameManager에 코루틴 호스팅 위임.
            else _gameManager.StartCoroutine(ShowAfterDelay(delay));
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
