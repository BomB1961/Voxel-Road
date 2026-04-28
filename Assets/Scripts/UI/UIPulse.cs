using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelRoad.UI
{
    /// <summary>알파를 사인파로 펄스시키는 효과. TAP TO RESTART 등에 사용.</summary>
    public sealed class UIPulse : MonoBehaviour
    {
        [SerializeField] private Graphic _graphic;
        [SerializeField] private TMP_Text _tmpText;
        [SerializeField] private float _minAlpha = 0.35f;
        [SerializeField] private float _maxAlpha = 1f;
        [SerializeField] private float _frequency = 1.2f;

        private void Reset()
        {
            _graphic = GetComponent<Graphic>();
            _tmpText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            float t = (Mathf.Sin(Time.unscaledTime * _frequency * Mathf.PI * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(_minAlpha, _maxAlpha, t);
            ApplyAlpha(alpha);
        }

        private void OnDisable()
        {
            ApplyAlpha(_maxAlpha);
        }

        private void ApplyAlpha(float alpha)
        {
            if (_tmpText != null)
            {
                Color c = _tmpText.color;
                c.a = alpha;
                _tmpText.color = c;
            }
            else if (_graphic != null)
            {
                Color c = _graphic.color;
                c.a = alpha;
                _graphic.color = c;
            }
        }
    }
}
