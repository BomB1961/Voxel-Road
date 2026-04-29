using System.Collections;
using UnityEngine;

namespace VoxelRoad.UI
{
    /// <summary>이전 BestScore를 처음 추월한 순간 NEW BEST 배너를 1회 페이드 인-아웃. BestScore=0 첫 플레이는 트리거하지 않음.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class NewBestBanner : MonoBehaviour
    {
        [SerializeField] private ScoreTracker _tracker;
        [SerializeField] private RectTransform _bannerRect;
        [SerializeField] private float _fadeInSeconds = 0.25f;
        [SerializeField] private float _holdSeconds = 1.5f;
        [SerializeField] private float _fadeOutSeconds = 0.4f;
        [SerializeField] private float _peakScale = 1.1f;

        private CanvasGroup _group;
        private bool _hasShown;
        private int _initialBest;
        private Coroutine _routine;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_tracker == null || _bannerRect == null)
            {
                Debug.LogError("[NewBestBanner] 필수 참조 미할당");
                enabled = false;
                return;
            }
            _group.alpha = 0f;
            _bannerRect.localScale = Vector3.one * 0.8f;
        }

        private void Start()
        {
            _initialBest = _tracker.BestScore;
        }

        private void OnEnable()
        {
            if (_tracker != null) _tracker.OnBestScoreChanged += HandleBestScoreChanged;
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.OnBestScoreChanged -= HandleBestScoreChanged;
        }

        private void HandleBestScoreChanged(int best)
        {
            if (_hasShown) return;
            if (_initialBest <= 0) return;
            if (best <= _initialBest) return;

            _hasShown = true;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PlayBannerRoutine());
        }

        private IEnumerator PlayBannerRoutine()
        {
            float t = 0f;
            while (t < _fadeInSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / _fadeInSeconds);
                _group.alpha = k;
                _bannerRect.localScale = Vector3.one * Mathf.Lerp(0.8f, _peakScale, k);
                yield return null;
            }
            _group.alpha = 1f;
            _bannerRect.localScale = Vector3.one * _peakScale;

            float settle = 0.15f;
            float st = 0f;
            while (st < settle)
            {
                st += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(st / settle);
                _bannerRect.localScale = Vector3.one * Mathf.Lerp(_peakScale, 1f, k);
                yield return null;
            }
            _bannerRect.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(_holdSeconds);

            float ot = 0f;
            while (ot < _fadeOutSeconds)
            {
                ot += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(ot / _fadeOutSeconds);
                _group.alpha = 1f - k;
                yield return null;
            }
            _group.alpha = 0f;
            _routine = null;
        }
    }
}
