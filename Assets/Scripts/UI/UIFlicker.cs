using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelRoad.UI
{
    /// <summary>일정 주기로 알파를 0/1 토글하는 깜빡임 효과. GAME OVER, NEW! 등에 사용.</summary>
    public sealed class UIFlicker : MonoBehaviour
    {
        [SerializeField] private Graphic _graphic;
        [SerializeField] private TMP_Text _tmpText;
        [SerializeField] private float _onDuration = 0.6f;
        [SerializeField] private float _offDuration = 0.2f;

        private float _timer;
        private bool _isOn = true;

        private void Reset()
        {
            _graphic = GetComponent<Graphic>();
            _tmpText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            _timer = 0f;
            _isOn = true;
            ApplyAlpha(1f);
        }

        private void OnDisable()
        {
            ApplyAlpha(1f);
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            float threshold = _isOn ? _onDuration : _offDuration;
            if (_timer < threshold) return;

            _timer -= threshold;
            _isOn = !_isOn;
            ApplyAlpha(_isOn ? 1f : 0f);
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
