using UnityEngine;

namespace VoxelRoad.Common
{
    /// <summary>RoadLane/RiverLane 자식 스포너의 공통 베이스.
    /// "주기적으로 한 객체를 한 방향으로 흘려보내는" 패턴 공유.
    /// 기차(상태머신·다차량 편성)는 본질이 달라 이 베이스를 쓰지 않음.</summary>
    public abstract class PeriodicLaneSpawnerBase<TConfig> : MonoBehaviour where TConfig : ScriptableObject
    {
        protected TConfig _config;
        protected float _direction;
        protected float _laneSpanX;
        protected float _multiplier = 1f;
        protected float _currentSpeed;
        protected float _nextSpawnTime;
        protected bool _hasPrefabs;
        protected bool _initialized;

        public void Initialize(TConfig config, float direction, float laneSpanX, float difficultyMultiplier = 1f)
        {
            _config = config;
            _direction = Mathf.Sign(direction);
            _laneSpanX = laneSpanX;
            _multiplier = Mathf.Clamp(difficultyMultiplier, 0.5f, 2f);

            _hasPrefabs = HasPrefabs;
            // 청크 난이도 적용: 속도×multiplier, 스폰 간격÷multiplier(빈도↑).
            _currentSpeed = ResolveBaseSpeed() * _multiplier;
            _nextSpawnTime = Time.time + Random.Range(0f, FirstSpawnDelayMax);
            _initialized = true;

            Prewarm();
        }

        private void Prewarm()
        {
            if (!_hasPrefabs) return;
            int count = PrewarmCount;
            float halfSpan = _laneSpanX * 0.5f;
            float spacing = _laneSpanX / count;
            for (int i = 0; i < count; i++)
            {
                float baseX = -halfSpan + spacing * (i + 0.5f);
                float jitter = Random.Range(-spacing * 0.25f, spacing * 0.25f);
                SpawnAt(baseX + jitter);
            }
        }

        protected virtual void Update()
        {
            if (!_initialized || _config == null || !_hasPrefabs) return;
            if (Time.time < _nextSpawnTime) return;

            float startX = _direction > 0f ? -_laneSpanX * 0.5f - 1.5f : _laneSpanX * 0.5f + 1.5f;
            SpawnAt(startX);

            var interval = SpawnIntervalRange;
            _nextSpawnTime = Time.time + Random.Range(interval.min, interval.max) / _multiplier;
        }

        protected abstract bool HasPrefabs { get; }
        protected abstract int PrewarmCount { get; }
        protected abstract float FirstSpawnDelayMax { get; }
        protected abstract (float min, float max) SpawnIntervalRange { get; }
        protected abstract float ResolveBaseSpeed();
        /// <summary>레인 중앙 기준 X 좌표에 객체 1개를 인스턴스화·Launch한다.</summary>
        protected abstract void SpawnAt(float laneRelativeX);
    }
}
