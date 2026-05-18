using UnityEngine;

namespace AKTR.Features.Fame
{
    [CreateAssetMenu(menuName = "AKTR/Fame/Fame Config")]
    public class FameConfigSO : ScriptableObject
    {
        [Header("Fame Per Spin")]
        [SerializeField] private int _famePerSpin = 5;
        [SerializeField] private int _bonusFamePerCredit = 1;

        [Header("Tier Thresholds")]
        [SerializeField] private int _tier2Threshold = 500;
        [SerializeField] private int _tier3Threshold = 2000;
        [SerializeField] private int _tier4Threshold = 10000;

        [Header("Tier Names")]
        [SerializeField]
        private string[] _tierNames = new string[]
        {
            "Unknown Knight",
            "Recognized Hero",
            "Celebrated Champion",
            "Knight of Legend"
        };

        public int FamePerSpin => _famePerSpin;
        public int BonusFamePerCredit => _bonusFamePerCredit;
        public string[] TierNames => _tierNames;

        public int GetThresholdForTier(int tier)
        {
            return tier switch
            {
                2 => _tier2Threshold,
                3 => _tier3Threshold,
                4 => _tier4Threshold,
                _ => int.MaxValue
            };
        }

        public int GetTierForFame(int fame)
        {
            if (fame >= _tier4Threshold) return 4;
            if (fame >= _tier3Threshold) return 3;
            if (fame >= _tier2Threshold) return 2;
            return 1;
        }

        public int GetFameToNextTier(int currentFame, int currentTier)
        {
            if (currentTier >= 4) return 0;
            return GetThresholdForTier(currentTier + 1) - currentFame;
        }
    }
}
