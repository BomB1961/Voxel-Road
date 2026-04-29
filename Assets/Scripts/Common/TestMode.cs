using UnityEngine;

namespace VoxelRoad.Common
{
    /// <summary>테스트 전용 토글. Awake가 가장 먼저 실행되도록 ExecutionOrder 음수.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class TestMode : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _resetBestScoreOnStart = true;
        [SerializeField] private bool _grassOnly = true;
        [Tooltip("배너의 '첫 플레이 가드'(BestScore<=0 시 미발동)를 통과시키기 위해 1로 시작.")]
        [SerializeField] private int _bestScoreSeedValue = 1;

        public static bool ForceGrassOnly { get; private set; }

        private void Awake()
        {
            if (!_enabled)
            {
                ForceGrassOnly = false;
                return;
            }

            if (_resetBestScoreOnStart)
            {
                // 0이 아닌 작은 양수로 시드 → 배너의 첫 플레이 가드를 우회하면서도 거의 즉시 신기록 갱신 가능
                int seed = Mathf.Max(0, _bestScoreSeedValue);
                PlayerPrefs.SetInt("VoxelRoad.BestScore", seed);
                PlayerPrefs.Save();
            }

            ForceGrassOnly = _grassOnly;
        }

        private void OnDisable()
        {
            ForceGrassOnly = false;
        }
    }
}
