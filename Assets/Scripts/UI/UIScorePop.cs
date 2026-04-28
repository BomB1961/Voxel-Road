using System.Collections;
using UnityEngine;

namespace VoxelRoad.UI
{
    /// <summary>점수 갱신 시 RectTransform 스케일을 펑 튕기는 효과.</summary>
    public sealed class UIScorePop : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _peakScale = 1.2f;
        [SerializeField] private float _popDuration = 0.15f;

        private Coroutine _routine;

        private void Reset()
        {
            _target = transform as RectTransform;
        }

        public void Pop()
        {
            if (_target == null) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PopRoutine());
        }

        private IEnumerator PopRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _popDuration);
                float curve = Mathf.Sin(t * Mathf.PI);
                float scale = Mathf.Lerp(1f, _peakScale, curve);
                _target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            _target.localScale = Vector3.one;
            _routine = null;
        }
    }
}
