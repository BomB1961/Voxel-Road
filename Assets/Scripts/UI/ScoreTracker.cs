using System;
using UnityEngine;

namespace VoxelRoad.UI
{
    /// <summary>플레이어 MaxZ를 폴링해 점수로 노출. BestScore PlayerPrefs 영속화.</summary>
    public sealed class ScoreTracker : MonoBehaviour
    {
        private const string BestScoreKey = "VoxelRoad.BestScore";

        [SerializeField] private PlayerController _player;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnBestScoreChanged;

        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public bool IsNewRecord { get; private set; }

        private void Awake()
        {
            if (_player == null)
            {
                Debug.LogError("[ScoreTracker] _player 미할당");
                enabled = false;
                return;
            }

            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            Score = 0;
            IsNewRecord = false;
        }

        private void Start()
        {
            OnScoreChanged?.Invoke(Score);
            OnBestScoreChanged?.Invoke(BestScore);
        }

        private void Update()
        {
            int current = _player.MaxZ;
            if (current <= Score) return;

            Score = current;
            OnScoreChanged?.Invoke(Score);

            if (Score > BestScore)
            {
                bool firstTimeBeatingRecord = !IsNewRecord;
                BestScore = Score;
                IsNewRecord = true;
                OnBestScoreChanged?.Invoke(BestScore);

                if (firstTimeBeatingRecord)
                    PlayerPrefs.SetInt(BestScoreKey, BestScore);
            }
        }

        /// <summary>사망 시 GameOverPanel이 호출. 신기록이면 PlayerPrefs 즉시 flush.</summary>
        public void CommitBestScore()
        {
            if (!IsNewRecord) return;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.Save();
        }
    }
}
