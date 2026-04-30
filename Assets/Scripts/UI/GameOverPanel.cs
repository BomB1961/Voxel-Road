using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoxelRoad.Game;

namespace VoxelRoad.UI
{
    /// <summary>사망 시 활성화되는 패널. 최종 점수·Replay/Quit 버튼.
    /// 패널 표시 후 일정 시간이 지나면 텍스트들을 페이드 처리해 버튼이 시선의 주인공이 되도록 함.</summary>
    public sealed class GameOverPanel : MonoBehaviour
    {
        // TMP의 SetText 포매터는 C# string.Format과 다름. {0:D5}는 인식 안 되어 소수점 1자리 fallback 발생.
        // {0:00000} 사용해야 5자리 0-padding 정수로 출력. ScoreCard와 동일 형식.
        private const string FinalScoreFormat = "SCORE {0:00000}";
        private const string BestScoreFormat = "BEST SCORE {0:00000}";

        [SerializeField] private GameManager _gameManager;
        [SerializeField] private ScoreTracker _scoreTracker;
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _gameOverText;
        [SerializeField] private TMP_Text _finalScoreText;
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _quitButton;

        [Header("Death Motion 후 패널 표시 지연 (초)")]
        [SerializeField] private float _delayVehicle = 0.5f;
        [SerializeField] private float _delayTrain = 0.5f;
        [SerializeField] private float _delayDrown = 1.5f;
        [SerializeField] private float _delayFallOver = 0.5f; // OutOfBounds, Idle

        [Header("텍스트 페이드 (버튼 강조용)")]
        [SerializeField] private float _dimDelaySeconds = 2f;
        [SerializeField] private float _dimFadeSeconds = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _dimTargetAlpha = 0.25f;

        private void Awake()
        {
            if (_gameManager == null || _scoreTracker == null || _root == null
                || _gameOverText == null || _finalScoreText == null || _bestScoreText == null
                || _replayButton == null || _quitButton == null)
            {
                Debug.LogError("[GameOverPanel] 필수 참조 미할당");
                enabled = false;
                return;
            }

            _root.SetActive(false);
            _replayButton.onClick.AddListener(Replay);
            _quitButton.onClick.AddListener(Quit);

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

            ResetTextAlpha();

            float delay = GetDelayForReason(reason);
            // _root가 self를 가리켜 Awake에서 비활성화 → this.StartCoroutine 불가.
            // 활성 상태인 _gameManager에 코루틴 호스팅 위임.
            if (delay <= 0f)
            {
                _root.SetActive(true);
                _gameManager.StartCoroutine(DimAfterDelay());
            }
            else
            {
                _gameManager.StartCoroutine(ShowAfterDelay(delay));
            }
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
            yield return DimAfterDelay();
        }

        private IEnumerator DimAfterDelay()
        {
            yield return new WaitForSeconds(_dimDelaySeconds);
            float t = 0f;
            while (t < _dimFadeSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / _dimFadeSeconds);
                float alpha = Mathf.Lerp(1f, _dimTargetAlpha, k);
                SetTextAlpha(alpha);
                yield return null;
            }
            SetTextAlpha(_dimTargetAlpha);
        }

        private void ResetTextAlpha() => SetTextAlpha(1f);

        private void SetTextAlpha(float alpha)
        {
            SetAlpha(_gameOverText, alpha);
            SetAlpha(_finalScoreText, alpha);
            SetAlpha(_bestScoreText, alpha);
        }

        private static void SetAlpha(TMP_Text text, float alpha)
        {
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        private static void Replay()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
