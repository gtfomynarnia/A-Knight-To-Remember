using UnityEngine;

namespace AKTR.Features.BossEncounter
{
    [CreateAssetMenu(menuName = "AKTR/Boss/Boss Encounter Config")]
    public class BossEncounterConfigSO : ScriptableObject
    {
        [Header("Enemy Names Per Tier")]
        [SerializeField]
        private string[] _enemyNames = new string[]
        {
            "Giant",
            "Hydra",
            "Green Dragon",
            "Red Dragon"
        };

        [Header("Award Pool Per Tier (base credits)")]
        [SerializeField]
        private int[] _awardPools = new int[]
        {
            50,
            100,
            200,
            500,
        };

        [Header("Per-Reel Award Value Per Tier")]
        [SerializeField]
        private int[] _reelAwardValues = new int[]
        {
            10,
            20,
            40,
            100
        };

        [Header("Combat Result Multipliers")]
        [SerializeField] private float _criticalHitMultiplier = 2.0f;
        [SerializeField] private float _hitMultiplier = 1.0f;
        [SerializeField] private float _glancingBlowMultiplier = 0.5f;

        [Header("Scatter Count Required")]
        [SerializeField] private int _scatterCounterRequired = 3;

        public string GetEnemyName(int tier) => _enemyNames[tier - 1];
        public int GetAwardPool(int tier) => _awardPools[tier - 1];
        public int GetReelAwardValues(int tier) => _reelAwardValues[tier - 1];
        public float CriticalHitMultiplier => _criticalHitMultiplier;
        public float HitMultiplier => _hitMultiplier;
        public float GlancingBlowMultiplier => _glancingBlowMultiplier;
        public int ScatterCountRequired => _scatterCounterRequired;
    }
}
