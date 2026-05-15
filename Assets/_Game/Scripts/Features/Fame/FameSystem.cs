using UnityEngine;
using AKTR.Core.Events;
using AKTR.Managers;

namespace AKTR.Features.Fame
{
    public class FameSystem : MonoBehaviour
    {
        // Configuration
        [SerializeField] private FameConfigSO _fameConfig;

        // Events
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private IntEventSO _onCreditsAwarded;
        [SerializeField] private FameChangedEventSO _onFameChanged;
        [SerializeField] private GameStateEventSO _onGameStateChanged;

        // State
        private int _currentFame;
        private int _currentTier = 1;
        private int _pendingFame;

        private void Awake()
        {
            _currentTier = 1;
        }

        public int CurrentFame => _currentFame;
        public int CurrentTier => _currentTier;
        public int PendingFame => _pendingFame;

        // Lifecycle
        private void OnEnable()
        {
            _onSpinComplete.Register(HandleSpinComplete);
            _onCreditsAwarded.Register(HandleCreditsAwarded);
            _onGameStateChanged.Register(HandleGameStateChanged);
        }

        private void OnDisable()
        {
            _onSpinComplete.Unregister(HandleSpinComplete);
            _onCreditsAwarded.Unregister(HandleCreditsAwarded);
            _onGameStateChanged.Unregister(HandleGameStateChanged);
        }

        // Handlers
        private void HandleSpinComplete(SpinResult result)
        {
            _pendingFame = _fameConfig.FamePerSpin;
        }

        private void HandleCreditsAwarded(int credits)
        {
            if (credits > 0)
            {
                _pendingFame += credits * _fameConfig.BonusFamePerCredit;
            }

            AddFame(_pendingFame);
            _pendingFame = 0;
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.GameOver)
            {
                ResetSession();
            }
        }

        // Fame Logic
        private void AddFame(int amount)
        {
            _currentFame += amount;

            int newTier = _fameConfig.GetTierForFame(_currentFame);
            bool tierChanged = newTier > _currentTier;
            _currentTier = newTier;
            _currentTier = Mathf.Clamp(_currentTier, 1, 4);

            int fameToNext = _fameConfig.GetFameToNextTier(_currentFame, _currentTier);

            var fameData = new FameData(_currentFame, fameToNext, _currentTier, tierChanged);
            _onFameChanged.Raise(fameData);

            Debug.Log($"Fame: {_currentFame} | Tier {_currentTier}: {_fameConfig.TierNames[_currentTier - 1]} | {fameToNext} to next tier.");

            if (tierChanged)
            {
                Debug.Log($"TIER UP! Now: {_fameConfig.TierNames[_currentTier -1]}");
            }
        }

        private void ResetSession()
        {
            _currentTier = 1;
            _currentFame = 0;

            var fameData = new FameData(0, _fameConfig.GetThresholdForTier(2), 1, false);
            _onFameChanged.Raise(fameData);

            Debug.Log("Session reset. Fame cleared.");
        }
    }
}

            
