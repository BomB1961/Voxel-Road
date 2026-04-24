using UnityEngine;

namespace VoxelRoad.Rail
{
    /// <summary>RailLane 자식. 경고(깜빡임) → 기차 스폰 → 대기 주기 반복.</summary>
    public sealed class TrainSpawner : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _warningLight;
        [SerializeField] private Color _warningColor = Color.red;
        [SerializeField] private Color _idleColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private float _warningBlinkHz = 6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private TrainConfigSO _config;
        private float _direction;
        private float _laneSpanX;
        private float _nextWarningTime;
        private float _warningStartTime;
        private bool _isWarning;
        private bool _hasPrefabs;
        private bool _initialized;
        private Material _warningMat;

        public void Initialize(TrainConfigSO config, float direction, float laneSpanX)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;
            _hasPrefabs = config != null && config.TrainPrefabs != null && config.TrainPrefabs.Length > 0;
            if (_warningLight != null)
            {
                _warningMat = _warningLight.material;
                SetWarningColor(_idleColor);
            }
            // 첫 경고는 즉시 시작 (레인 스폰과 동시에 기차 등장 효과).
            _nextWarningTime = Time.time;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _config == null || !_hasPrefabs) return;

            if (!_isWarning)
            {
                if (Time.time >= _nextWarningTime)
                {
                    _isWarning = true;
                    _warningStartTime = Time.time;
                }
                return;
            }

            // 경고 중: 깜빡임
            float elapsed = Time.time - _warningStartTime;
            bool on = Mathf.FloorToInt(elapsed * _warningBlinkHz) % 2 == 0;
            SetWarningColor(on ? _warningColor : _idleColor);

            if (elapsed >= _config.WarningSeconds)
            {
                SpawnTrain();
                SetWarningColor(_idleColor);
                _isWarning = false;
                _nextWarningTime = Time.time + Random.Range(_config.MinCycleInterval, _config.MaxCycleInterval);
            }
        }

        private void SpawnTrain()
        {
            var prefab = _config.TrainPrefabs[Random.Range(0, _config.TrainPrefabs.Length)];
            if (prefab == null) return;

            float s = _config.SpawnScale;
            float lx = s * _config.LengthScale;
            int cars = _config.CarsPerTrain;
            float carStride = lx + _config.CarSpacing; // 차량 간 X 중심 거리
            float headX = _direction > 0f ? -_laneSpanX * 0.5f - 3f : _laneSpanX * 0.5f + 3f;

            // 진행 방향 기준 앞 차량(head)부터 뒤로 offset하여 연결된 모양.
            for (int i = 0; i < cars; i++)
            {
                float offsetX = -_direction * carStride * i;
                var go = Instantiate(prefab, transform);
                go.transform.localPosition = new Vector3(headX + offsetX, 0f, 0f);
                go.transform.localScale = new Vector3(lx, s, s);
                var train = go.GetComponent<Train>();
                if (train != null) train.Launch(_config.Speed, _direction, _laneSpanX);
            }
        }

        private void SetWarningColor(Color c)
        {
            if (_warningMat == null) return;
            _warningMat.color = c; // 레거시/Standard 호환
            if (_warningMat.HasProperty(BaseColorId))
                _warningMat.SetColor(BaseColorId, c); // URP Lit 호환
        }

        private void OnDestroy()
        {
            if (_warningMat != null) Destroy(_warningMat);
        }
    }
}
