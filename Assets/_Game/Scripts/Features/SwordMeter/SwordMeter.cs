using UnityEngine;
using AKTR.Core.Events;
using AKTR.Core;

namespace AKTR.Features.SwordMeter
{
    public class SwordMeter : MonoBehaviour
    {
        // Configuration
        [SerializeField] private int _chargeRequired = 10;

        // Events
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private SwordMeterEventSO _onSwordMeterFull;

        // State
        private int _currentCharge;
        private bool _isPoweredUp;
        private int _poweredUpSpinsRemaining;

        public bool IsPoweredUp => _isPoweredUp;
        public int PoweredUpSpinsRemaining => _poweredUpSpinsRemaining;
        public int CurrentCharge => _currentCharge;
        public int ChargeRequired => _chargeRequired;
        public float FillPercent => (float)_currentCharge / _chargeRequired;

        // Lifecycle
        private void OnEnable()
        {
            _onSpinComplete.Register(HandleSpinComplete);
            _onSwordMeterFull.Register(HandleMeterFull);
        }

        private void OnDisable()
        {
            _onSpinComplete.Register(HandleSpinComplete);
            _onSwordMeterFull.Unregister(HandleMeterFull);
        }

        // Handler
        private void HandleSpinComplete(SpinResult result)
        {
            // No Swords for the current Spin
            if (!result.HasSwordSymbol)
            {
                Debug.Log("No sword symbols this spin.");
                return;
            }

            int swordsThisSpin = CountSwords(result.Grid);
            _currentCharge += swordsThisSpin;

            Debug.Log($"Sword meter: {_currentCharge}/{_chargeRequired} (+{swordsThisSpin})");

            while ( _currentCharge >= _chargeRequired )
            {
                _currentCharge -= _chargeRequired;
                _onSwordMeterFull.Raise();
                Debug.Log("Sword meter full! Multiplier triggered.");
            }
        }

        private void HandleMeterFull()
        {
            _isPoweredUp = true;
        }

        private int CountSwords(SymbolDefinitionSO[,] grid)
        {
            int count = 0;
            foreach (var symbol in grid)
            {
                if (symbol != null && symbol.IsSword)
                    count++;
            }
            return count;
        }
    }
}

