using UnityEngine;
using UnityEngine.UIElements;
using AKTR.Core.Events;
using AKTR.Managers;
using System.Collections;

namespace AKTR.Presentation
{
    public class  BossEncounterUIController : MonoBehaviour
    {
        // Events
        [SerializeField] private bossTriggeredEventSO _onBossTriggered;
        [SerializeField] private ReelBattleResultEventSO _onReelBattleResult;
        [SerializeField] private GameStateEventSO _onGameStateChanged;
        [SerializeField] private OnReelAnimCompleteSO _onReelAnimComplete;

        // Pending Data Variables
        private BossData _pendingBossData;
        private bool _hasPendingBoss;

        // UI References
        private VisualElement _bossOverlay;
        private Label _enemyNameLabel;
        private Label _awardLabel;
        private Label _phaseLabel;
        private Label _totalLabel;
        private Label _promptLabel;
        private bool _isGoldDragon;
        private Label[] _resultTypeLabels = new Label[5];
        private Label[] _resultCreditLabels = new Label[5];

        // State
        private int _runningTotal;
        private int _currentPhase;

        // Lifecycle
        private void OnEnable()
        {
            _onBossTriggered.Register(HandleBossTriggered);
            _onReelBattleResult.Register(HandleReelBattleResult);
            _onGameStateChanged.Register(HandleGameStateChanged);
            _onReelAnimComplete.Register(HandleReelAnimComplete);
        }

        private void OnDisable()
        {
            _onBossTriggered.Unregister(HandleBossTriggered);
            _onReelBattleResult.Unregister(HandleReelBattleResult);
            _onGameStateChanged.Unregister(HandleGameStateChanged);
            _onReelAnimComplete.Unregister(HandleReelAnimComplete);
        }

        private void Start()
        {
            StartCoroutine(BindAfterDelay());
        }

        private void BindElements(VisualElement root)
        {
            _bossOverlay = root.Q<VisualElement>("boss-overlay");
            _enemyNameLabel = root.Q<Label>("boss-enemy-name-label");
            _awardLabel = root.Q<Label>("boss-award-label");
            _totalLabel = root.Q<Label>("boss-total-label");
            _promptLabel = root.Q<Label>("boss-prompt-label");
            _phaseLabel = root.Q<Label>("boss-phase-label");

            for (int i = 0; i < 5; i++)
            {
                _resultTypeLabels[i] = root.Q<Label>($"boss-result-type-{i}");
                _resultCreditLabels[i] = root.Q<Label>($"boss-result-credits-{i}");
            }
        }

        // Handlers
        private void HandleBossTriggered(BossData data)
        {
            _isGoldDragon = data.IsGoldDragon;
            _runningTotal = 0;
            _currentPhase = 1;

            _enemyNameLabel.text = data.IsGoldDragon ? "THE GOLD DRAGON" : $"THE {data.EnemyName.ToUpper()}";
            _awardLabel.text = $"Award Pool: {data.AwardPool} credits";
            _phaseLabel.text = "PHASE 1";
            _totalLabel.text = "TOTAL: 0 credits";
            _promptLabel.text = "Press STOP to strike!";

            ResetReelResults();
            _hasPendingBoss = true;
        }

        private void HandleReelBattleResult(ReelBattleResult result)
        {
            int index = result.ReelIndex;
            if (index < 0 || index >= 5) return;

            // set result type label and style
            string resultText = result.ResultType switch
            {
                "Critical Hit" => "CRITICAL!",
                "Hit" => "HIT",
                "Glancing Blow" => "GLANCING",
                _ => result.ResultType
            };

            _resultTypeLabels[index].text = resultText;
            _resultTypeLabels[index].RemoveFromClassList("boss-result-type--crit");
            _resultTypeLabels[index].RemoveFromClassList("boss-result-type--hit");
            _resultTypeLabels[index].RemoveFromClassList("boss-result-type--glancing");

            string styleClass = result.ResultType switch
            {
                "Critical Hit" => "boss-result-type--crit",
                "Hit" => "boss-result-type--hit",
                "Glancing Blow" => "boss-result-type--glancing",
                _ => ""
            };

            if (!string.IsNullOrEmpty(styleClass))
                _resultTypeLabels[index].AddToClassList(styleClass);

            _resultCreditLabels[index].text = $"+{result.CreditsAwarded}";

            _runningTotal += result.CreditsAwarded;
            _totalLabel.text = $"TOTAL: {_runningTotal} credits";
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.BossReelWaiting)
            {
                _promptLabel.text = "Press STOP to strike!";
            }

            if (state == GameManager.GameState.BossComplete)
            {
                if (_isGoldDragon && _currentPhase == 1)
                {
                    _phaseLabel.text = "PHASE 2 - THE DRAGON LIVES!";
                    _promptLabel.text = "The battle rages on...";
                    ResetReelResults();
                    _currentPhase = 2;
                }
                else
                {
                    _promptLabel.text = $"Victory! {_runningTotal} credits won!";
                }
            }

            if (state == GameManager.GameState.Paying)
            {
                HideOverlay();
            }

            if (state == GameManager.GameState.Idle && _bossOverlay != null)
            {
                HideOverlay();
            }
        }

        private void HandleReelAnimComplete()
        {
            if (_hasPendingBoss)
            {
                ShowOverlay();
                _hasPendingBoss = false;
            }
        }

        // Helpers
        private void ShowOverlay()
        {
            if (_bossOverlay == null) return;
            _bossOverlay.AddToClassList("boss-overlay--visible");
        }

        private void HideOverlay()
        {
            if (_bossOverlay == null) return;
            _bossOverlay.RemoveFromClassList("boss-overlay--visible");
        }

        private void ResetReelResults()
        {
            for (int i = 0; i < 5; i++)
            {
                if (_resultTypeLabels[i] != null)
                {
                    _resultTypeLabels[i].text = "-";
                    _resultTypeLabels[i].RemoveFromClassList("boss-result-type--crit");
                    _resultTypeLabels[i].RemoveFromClassList("boss-result-type--hit");
                    _resultTypeLabels[i].RemoveFromClassList("boss-result-type--glancing");
                }
                if (_resultCreditLabels[i] != null)
                    _resultCreditLabels[i].text = "-";
            }
        }

        private IEnumerator BindAfterDelay()
        {
            yield return null;
            yield return null;

            var root = GetComponent<UIDocument>().rootVisualElement;
            BindElements(root);
            HideOverlay();
        }
    }
}