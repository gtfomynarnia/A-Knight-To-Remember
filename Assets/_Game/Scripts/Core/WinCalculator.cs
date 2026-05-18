using UnityEngine;
using AKTR.Core.Events;

namespace AKTR.Core
{
    public class WinCalculator : MonoBehaviour
    {
        // Win Tuneables
        [SerializeField] private int _bigWinThreshold = 50;
        [SerializeField] private int _megaWinThreshold = 200;

        // Configuration
        [SerializeField] private int _multiplier = 3;
        [SerializeField] private int _poweredUpSpins = 5;

        // Events 
        [SerializeField] private WinEvaluatedEventSO _onWinEvlauated;
        [SerializeField] private SwordMeterEventSO _onSwordMeterFull;
        [SerializeField] private IntEventSO _onCreditsAwarded;

        // State
        private bool _isPoweredUp;
        private int _poweredUpSpinsRemaining;

        public bool IsPoweredUp => _isPoweredUp;
        public int PoweredUpSpinsRemaining => _poweredUpSpinsRemaining;
        public int BigWinThreshold => _bigWinThreshold;
        public int MegaWinThreshold => _megaWinThreshold;

        // Lifecycle
        private void OnEnable()
        {
            _onWinEvlauated.Register(HandleWinEvaluated);
            _onSwordMeterFull.Register(HandleSwordMeterFull);
        }

        private void OnDisable()
        {
            _onWinEvlauated.Unregister(HandleWinEvaluated);
            _onSwordMeterFull.Unregister(HandleSwordMeterFull);
        }

        // Handlers
        private void HandleSwordMeterFull()
        {
            _isPoweredUp = true;
            _poweredUpSpinsRemaining += _poweredUpSpins;
            Debug.Log($"Sword powered up! {_multiplier}x multiplier for {_poweredUpSpinsRemaining} spins.");
        }

        private void HandleWinEvaluated(WinResult result)
        {
            int finalCredits = result.BaseCredits;

            if (_isPoweredUp)
            {
                finalCredits *= _multiplier;
                _poweredUpSpinsRemaining--;

                if (_poweredUpSpinsRemaining <= 0 )
                {
                    _isPoweredUp = false;
                    Debug.Log("Sword powered-up phase ended.");
                }
            }

            if (result.HasWin && result.WinLines != null)
            {
                foreach (var line in result.WinLines)
                {
                    Debug.Log($"WIN: {line.SymbolName} x{line.ReelCount} reels");
                }
            }
            else if (result.HasWin)
            {
                Debug.Log($"WIN: {finalCredits} credits awarded");
            }
            else
            {
                Debug.Log("No win this spin.");
            }

            Debug.Log($"Total awarded: {finalCredits} credits" +
                (_isPoweredUp ? $" (x{_multiplier} multiplier applied to {result.BaseCredits} base)" :
                $" ({result.BaseCredits} base)"));

            _onCreditsAwarded.Raise(finalCredits);
        }
    }
}
