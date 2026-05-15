using UnityEngine;
using System.Collections;
using AKTR.Core;
using AKTR.Core.Events;
using AKTR.Managers;

namespace AKTR.Features.BossEncounter
{
    public class BossEncounterSystem : MonoBehaviour
    {
        // Configuration
        [SerializeField] private BossEncounterConfigSO _config;

        // Events
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private bossTriggeredEventSO _onBossTriggered;
        [SerializeField] private ReelBattleResultEventSO _onReelBattleResult;
        [SerializeField] private WinEvaluatedEventSO _onWinEvaluated;
        [SerializeField] private GameStateEventSO _onGameStateChanged;
        [SerializeField] private FameChangedEventSO _onFameChanged;

        // State
        private int _currentFameTier = 1;
        private int _currentReelIndex;
        private int _totalCreditsThisPhase;
        private int _totalCreditsAllPhases;
        private bool _isGoldDragon;
        private bool _inBossEncounter;
        private int _currentPhase;

        // Lifecycle
        private void OnEnable()
        {
            _onSpinComplete.Register(HandleSpinComplete);
            _onGameStateChanged.Register(HandleGameStateChanged);
            _onFameChanged.Register(HandleFameChanged);
        }

        private void OnDisable()
        {
            _onSpinComplete.Unregister(HandleSpinComplete);
            _onGameStateChanged.Unregister(HandleGameStateChanged);
            _onFameChanged.Unregister(HandleFameChanged);
        }

        // Listeners
        private void HandleFameChanged(FameData data)
        {
            _currentFameTier = data.Tier;
        }

        private void HandleSpinComplete(SpinResult result)
        {
            if (_inBossEncounter)
            {
                HandleBattleReelStop();
                return;
            }

            int scatterCount = CountScatters(result.Grid);
            bool isGoldDragon = CheckGoldDragon(result.Grid);

            if (isGoldDragon && _currentFameTier >= 3)
            {
                StartBossEncounter(true);
                return;
            }

            if (scatterCount >= _config.ScatterCountRequired || isGoldDragon)
            {
                StartBossEncounter(false);
            }
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.BossReelWaiting)
            {
                Debug.Log($"Boss: Awaiting player input for reel {_currentReelIndex + 1}/5 ");
            }
        }

        // Boss Encounter

        public void Update()
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.BossReelWaiting)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    HandleBattleReelStop();
                }
            }
        }
        private void StartBossEncounter(bool isGoldDragon)
        {
            _isGoldDragon = isGoldDragon;
            _inBossEncounter = true;
            _currentReelIndex = 0;
            _totalCreditsThisPhase = 0;
            _totalCreditsAllPhases = 0;
            _currentPhase = 1;

            string enemyName = _config.GetEnemyName(_currentFameTier);
            int awardPool = _config.GetAwardPool(_currentFameTier);

            var bossData = new BossData(_currentFameTier, isGoldDragon, awardPool, enemyName);
            _onBossTriggered.Raise(bossData);

            Debug.Log($"BOSS ENCOUNTER! Enemy: {enemyName} | Gold Dragon: {isGoldDragon} | Award Pool: {awardPool}");

            GameManager.Instance.TransitionTo(GameManager.GameState.BossEncounter);
            StartCoroutine(DelayedBossStart());
        }

        private void HandleBattleReelStop()
        {
            GameManager.Instance.TransitionTo(GameManager.GameState.BossReelRevealing);

            int reelAward = _config.GetReelAwardValues(_currentFameTier);
            string resultType = GetRandomCombatResult();
            float multiplier = GetMultiplier(resultType);
            int creditsThisReel = Mathf.RoundToInt(reelAward * multiplier);

            _totalCreditsThisPhase += creditsThisReel;

            var battleResult = new ReelBattleResult(_currentReelIndex, resultType, creditsThisReel);
            _onReelBattleResult.Raise(battleResult);

            Debug.Log($"Reel {_currentReelIndex + 1}: {resultType} - {creditsThisReel} credits");

            _currentReelIndex++;

            StartCoroutine(NextReelOrComplete());
        }

        private IEnumerator NextReelOrComplete()
        {
            yield return new WaitForSeconds(1f);

            if (_currentReelIndex < 5)
            {
                GameManager.Instance.TransitionTo(GameManager.GameState.BossReelWaiting);
            }
            else
            {
                CompletePhase();
            }
        }

        private IEnumerator DelayedBossStart()
        {
            yield return new WaitForSeconds(3f);
            GameManager.Instance.TransitionTo(GameManager.GameState.BossReelWaiting);
        }
        private void CompletePhase()
        {
            _totalCreditsAllPhases += _totalCreditsThisPhase;

            Debug.Log($"Phase {_currentPhase} complete. Credits this phase: {_totalCreditsThisPhase}");

            GameManager.Instance.TransitionTo(GameManager.GameState.BossComplete);

            if (_isGoldDragon && _currentPhase == 1)
            {
                Debug.Log("Gold Dragon survives! Phase 2 begins.");
                _currentPhase = 2;
                _currentReelIndex = 0;
                _totalCreditsThisPhase = 0;
                GameManager.Instance.TransitionTo(GameManager.GameState.BossEncounter);
                GameManager.Instance.TransitionTo(GameManager.GameState.BossReelWaiting);
            }
            else
            {
                EndBossEncounter();
            }
        }

        private void EndBossEncounter()
        {
            _inBossEncounter = false;
            Debug.Log($"Boss defeated! Total credits: {_totalCreditsAllPhases}");

            bool isMegaWin = _isGoldDragon;
            bool isBigWin = _totalCreditsAllPhases >= 100;

            var result = new WinResult(
                _totalCreditsAllPhases,
                _totalCreditsAllPhases,
                isBigWin,
                isMegaWin,
                null
            );

            _onWinEvaluated.Raise(result);
            GameManager.Instance.TransitionTo(GameManager.GameState.Paying);
        }

        public void TriggerBossReelStop()
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.BossReelWaiting)
            {
                HandleBattleReelStop();
            }
        }

        // Helpers
        private int CountScatters(SymbolDefinitionSO[,] grid)
        {
            int count = 0;
            foreach (var symbol in grid)
            {
                if( symbol != null && symbol.IsScatter)
                {
                    count++;
                }
            }
            return count;
        }

        private bool CheckGoldDragon(SymbolDefinitionSO[,] grid)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int startReel = 0; startReel <= 2; startReel++)
                {
                    if (grid[startReel, row] != null &&
                        grid[startReel +1, row] != null &&
                        grid[startReel + 2, row] != null &&
                        grid[startReel, row].IsGoldDragon &&
                        grid[startReel + 1, row].IsGoldDragon &&
                        grid[startReel + 2, row].IsGoldDragon)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private string GetRandomCombatResult()
        {
            float roll = Random.value;
            if (roll < 0.2f) return "Critical Hit";
            if (roll < 0.7f) return "Hit";
            return "Glancing Blow";
        }

        private float GetMultiplier(string resultType)
        {
            return resultType switch
            {
                "Critical Hit" => _config.CriticalHitMultiplier,
                "Hit" => _config.HitMultiplier,
                "Glancing Blow" => _config.GlancingBlowMultiplier,
                _ => 1f
            };
        }
    }

    
}
