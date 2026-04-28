using UnityEngine;

namespace VoxelRoad.Game
{
    /// <summary>청크 단위 난이도 변동값.</summary>
    public enum ChunkDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>Step 9: 거리(MaxZ) 기반 base × 청크 단위 variance 곱셈으로
    /// 최종 난이도 multiplier를 산출. 합리적 범위(인간 반응 한계)로 clamp.</summary>
    public static class BalanceCurve
    {
        // 거리 곡선 임계값
        private const int LearningEndZ = 50;
        private const int RampEndZ = 150;
        private const int PlateauStartZ = 300;
        private const float BaseStart = 1.0f;
        private const float BaseRamp = 1.3f;
        private const float BasePlateau = 1.6f;

        // Variance multiplier
        private const float EasyMul = 0.7f;
        private const float NormalMul = 1.0f;
        private const float HardMul = 1.3f;

        // 추첨 확률 (누적)
        private const float EasyProb = 0.30f;
        private const float NormalProb = 0.80f; // Easy + Normal

        // 합리적 범위 가드
        private const float FinalMin = 0.7f;
        private const float FinalMax = 1.6f;

        /// <summary>거리(MaxZ) 기반 base multiplier. 시작 구간은 학습용 1.0 고정,
        /// 점진 증가 후 후반 plateau로 인간 한계 보호.</summary>
        public static float DistanceBase(int maxZ)
        {
            if (maxZ < LearningEndZ) return BaseStart;
            if (maxZ < RampEndZ)
                return Mathf.Lerp(BaseStart, BaseRamp, (maxZ - LearningEndZ) / (float)(RampEndZ - LearningEndZ));
            if (maxZ < PlateauStartZ)
                return Mathf.Lerp(BaseRamp, BasePlateau, (maxZ - RampEndZ) / (float)(PlateauStartZ - RampEndZ));
            return BasePlateau;
        }

        /// <summary>청크 단위 variance 추첨 (Easy 30% / Normal 50% / Hard 20%).</summary>
        public static ChunkDifficulty PickRandomDifficulty()
        {
            float r = Random.value;
            if (r < EasyProb) return ChunkDifficulty.Easy;
            if (r < NormalProb) return ChunkDifficulty.Normal;
            return ChunkDifficulty.Hard;
        }

        public static float Variance(ChunkDifficulty diff) => diff switch
        {
            ChunkDifficulty.Easy => EasyMul,
            ChunkDifficulty.Normal => NormalMul,
            ChunkDifficulty.Hard => HardMul,
            _ => NormalMul,
        };

        /// <summary>최종 multiplier = clamp(distanceBase × variance, 0.7, 1.6).
        /// 곱이 polynomial 폭발(1.6 × 1.3 = 2.08)을 막아 회피 불가능 청크 차단.</summary>
        public static float Combined(int maxZ, ChunkDifficulty diff)
        {
            float raw = DistanceBase(maxZ) * Variance(diff);
            return Mathf.Clamp(raw, FinalMin, FinalMax);
        }
    }
}
