using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;
using AKTR.Core.Events;
using AKTR.Managers;
using System.Collections.Generic;

namespace AKTR.Presentation
{
    public class GameFeedbackControllers : MonoBehaviour
    {
        // Events
        [SerializeField] private GameStateEventSO _onGameStateChanged;
        [SerializeField] private WinEvaluatedEventSO _onWinEvaluated;
        [SerializeField] private FameChangedEventSO _onFameChanged;
        [SerializeField] private SwordMeterEventSO _onSwordMeterFull;
        [SerializeField] private bossTriggeredEventSO _onBossTriggered;
        [SerializeField] private ReelBattleResultEventSO _onReelBattleResult;
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private IntEventSO _onCreditsAwarded;
        [SerializeField] private OnReelAnimCompleteSO _onReelAnimComplete;

        // Pending Data Variables
        private string _pendingMain;
        private string _pendingSub;
        private string _pendingStyle;
        private bool _hasPendingMessage;
        private int _spinCount;
        private int _pendingMessageSpinCount;

        // UI References
        private Label _mainLabel;
        private Label _subLabel;
        private VisualElement _feedbackPanel;
        private GameHUDController _hudController;

        // Script References
        private AKTR.Features.SwordMeter.SwordMeter _swordMeterSystem;
        private AKTR.Core.WinCalculator _winCalculator;

        // State 
        private int _swordCharge;
        private int _currentBossReel;
        private bool _inBossEncounter;
        private string _currentBossName;
        private bool _bossTriggeredThisSpin;
        private Queue<(string main, string sub, string style)> _messageQueue = new Queue<(string, string, string)>();
        private bool _isShowingQueue;
        private Coroutine _queueCoroutine;
        private string _pendingWinDetails;

        // Lifecycle
        private void OnEnable()
        {
            _onGameStateChanged.Register(HandleGameStateChanged);
            _onWinEvaluated.Register(HandleWinEvaluated);
            _onFameChanged.Register(HandleFameChanged);
            _onSwordMeterFull.Register(HandleSwordMeterFull);
            _onBossTriggered.Register(HandleBossTriggered);
            _onReelBattleResult.Register(HandleReelBattleResult);
            _onCreditsAwarded.Register(HandleCreditsAwarded);
            _onReelAnimComplete.Register(HandleReelAnimComplete);
        }

        private void OnDisable()
        {
            _onGameStateChanged.Unregister(HandleGameStateChanged);
            _onWinEvaluated.Unregister(HandleWinEvaluated);
            _onFameChanged.Unregister(HandleFameChanged);
            _onSwordMeterFull.Unregister(HandleSwordMeterFull);
            _onBossTriggered.Unregister(HandleBossTriggered);
            _onReelBattleResult.Unregister(HandleReelBattleResult);
            _onCreditsAwarded.Unregister(HandleCreditsAwarded);
            _onReelAnimComplete.Unregister(HandleReelAnimComplete);
        }

        private void Start()
        {
            var uIDocument = GetComponent<UIDocument>();
            uIDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnUIReady);
            _swordMeterSystem = FindAnyObjectByType<AKTR.Features.SwordMeter.SwordMeter>();
            _hudController = FindAnyObjectByType<GameHUDController>();
            _winCalculator = FindAnyObjectByType<AKTR.Core.WinCalculator>();
        }

        private void OnUIReady(GeometryChangedEvent evt)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.UnregisterCallback<GeometryChangedEvent>(OnUIReady);

            _feedbackPanel = root.Q<VisualElement>("feedback-panel");
            _mainLabel = root.Q<Label>("feedback-main-label");
            _subLabel = root.Q<Label>("feedback-sub-label");

            SetMessage("Press SPIN to play", "", "");
        }

        // Handlers
        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (_mainLabel == null) return;

            switch (state)
            {
                case GameManager.GameState.Idle:
                    _bossTriggeredThisSpin = false;
                    if (!_inBossEncounter && !_isShowingQueue)
                        SetMessage("Press SPIN to play", "", "");
                    break;

                case GameManager.GameState.Spinning:
                    _spinCount++;
                    _bossTriggeredThisSpin = false;
                    _inBossEncounter = false;
                    _pendingWinDetails = "";
                    _pendingMain = "";
                    _pendingSub = "";
                    _pendingStyle = "";
                    _hasPendingMessage = false;
                    _messageQueue.Clear();
                    _isShowingQueue = false;
                    if (_queueCoroutine != null) StopCoroutine(_queueCoroutine);
                    SetMessage("Spinning...", "", "");
                    break;

                case GameManager.GameState.Evaluating:
                    break;

                case GameManager.GameState.Paying:
                    _bossTriggeredThisSpin = false;
                    _inBossEncounter = false;
                    break;

                case GameManager.GameState.BossEncounter:
                    _bossTriggeredThisSpin = true;
                    break;

                case GameManager.GameState.BossReelWaiting:
                    _currentBossReel++;
                    _messageQueue.Clear();
                    _isShowingQueue = false;
                    if (_queueCoroutine != null) StopCoroutine(_queueCoroutine);
                    SetMessage($"Press STOP to strike!", $"Reel {_currentBossReel} of 5", "feedback-main--boss");
                    break;

                case GameManager.GameState.BossComplete:
                    SetMessage("Phase Complete!", "", "feedback-main--boss");
                    break;
            }
        }

        private void HandleWinEvaluated(WinResult result)
        {
            if (_mainLabel == null) return;
            if (_bossTriggeredThisSpin) return;

            _pendingWinDetails = "";

            if (result.WinLines == null || result.WinLines.Count == 0)
            {
                return;
            }

            var parts = new System.Text.StringBuilder();

            for (int i = 0; i < result.WinLines.Count; i++)
            {
                var line = result.WinLines[i];
                if (i > 0) parts.Append("  ·  ");
                parts.Append($"{line.SymbolName} x{line.ReelCount} reels");
            }

            _pendingWinDetails = parts.ToString();
        }

        private void HandleFameChanged(FameData data)
        {
            if (_mainLabel == null) return;
            if (!data.TierChanged) return;

            string tierName = data.Tier switch
            {
                2 => "Recognized Hero",
                3 => "Celebrated Champion",
                4 => "Knight of Legend",
                _ => ""
            };

            QueueMessageForAfterReels($"TIER UP!", $"You are now a {tierName}", "feedback-main--tier");
        }

        private void HandleCreditsAwarded(int credits)
        {
            if (_mainLabel == null) return;
            if (_bossTriggeredThisSpin) return;

            StartCoroutine(ShowWinMessageNextFrame(credits));
        }

        private void HandleSwordMeterFull()
        {
            if (_mainLabel == null) return;
            _swordCharge = 0;
            QueueMessageForAfterReels("SWORD POWERED UP!","3x multiplier active!","feedback-main--sword");
        }

        private void HandleBossTriggered(BossData data)
        {
            if (_mainLabel == null) return;

            _bossTriggeredThisSpin = true;
            _inBossEncounter = true;
            _currentBossReel = 0;
            _currentBossName = data.IsGoldDragon? "THE GOLD DRAGON APPEARS!": $"Face the {data.EnemyName}!";

            _messageQueue.Clear();
            _isShowingQueue = false;
            if (_queueCoroutine != null) StopCoroutine(_queueCoroutine);

            QueueMessageForAfterReels("BOSS ENCOUNTER!", _currentBossName, "feedback-main--boss");
        }

        private void HandleReelBattleResult(ReelBattleResult result)
        {
            if (_mainLabel == null) return;

            string emoji = result.ResultType switch
            {
                "Critical Hit" => "CRITICAL HIT!",
                "Hit" => "HIT!",
                "Glancing Blow" => "Glancing Blow",
                _ => result.ResultType
            };

            QueueMessageForAfterReels(emoji, $"+{result.CreditsAwarded} credits from reel {result.ReelIndex + 1}", result.ResultType == "Critical Hit" ? "feedback-main--win" : "feedback-main--boss");
        }

        private void HandleReelAnimComplete()
        {
            if (_hasPendingMessage && _pendingMessageSpinCount == _spinCount)
            {
                EnqueueMessage(_pendingMain, _pendingSub, _pendingStyle);
                _hasPendingMessage = false;
            }
        }

        // Helpers
        private void SetMessage(string main, string sub, string styleClass)
        {
            if (_mainLabel == null) return;

            _mainLabel.RemoveFromClassList("feedback-main--win");
            _mainLabel.RemoveFromClassList("feedback-main--boss");
            _mainLabel.RemoveFromClassList("feedback-main--tier");
            _mainLabel.RemoveFromClassList("feedback-main--sword");

            _mainLabel.text = main;
            _subLabel.text = sub;

            if (!string.IsNullOrEmpty(styleClass))
                _mainLabel.AddToClassList(styleClass);
        }

        private void EnqueueMessage(string main, string sub, string style)
        {
            _messageQueue.Enqueue((main, sub, style));

            if (_queueCoroutine != null) StopCoroutine(_queueCoroutine);
            _queueCoroutine = StartCoroutine(ShowMessageQueue());
        }

        private IEnumerator ShowMessageQueue()
        {
            _isShowingQueue = true;

            while (_messageQueue.Count > 0)
            {
                var msg = _messageQueue.Dequeue();
                SetMessage(msg.main, msg.sub, msg.style);
                yield return new WaitForSeconds(2f);
            }

            _isShowingQueue = false;

            if (GameManager.Instance.CurrentState == GameManager.GameState.Idle && !_inBossEncounter)
                SetMessage("Press SPIN to play", "", "");
        }

        private IEnumerator ShowWinMessageNextFrame(int credits)
        {
            int megaThreshold = _winCalculator != null ? _winCalculator.MegaWinThreshold : 200;
            int bigThreshold = _winCalculator != null ? _winCalculator.BigWinThreshold : 50;

            while (_hudController != null && !_hudController.IsSpinAnimationComplete)
                yield return null;

            yield return new WaitForSeconds(0.1f);

            if (_swordMeterSystem != null && !_swordMeterSystem.IsPoweredUp && _swordMeterSystem.CurrentCharge > 0)
            {
                EnqueueMessage("Sword Charged!", $"{_swordMeterSystem.CurrentCharge}/{_swordMeterSystem.ChargeRequired} charge", "feedback-main--sword");
            }

            if (credits <= 0)
            {
                EnqueueMessage("No win this spin", "", "");
                yield break;
            }

            if (credits >= megaThreshold)
            {
                EnqueueMessage("MEGA WIN!", $"{credits} credits", "feedback-main--win");
                yield break;
            }

            if (credits >= bigThreshold)
            {
                EnqueueMessage("BIG WIN!", $"{credits} credits", "feedback-main--win");
                yield break;
            }

            EnqueueMessage(
                $"YOU WON! {credits} credits",
                _pendingWinDetails,
                "feedback-main--win"
            );
        }

        private void QueueMessageForAfterReels(string main, string sub, string style)
        {
            _pendingMain = main;
            _pendingSub = sub;
            _pendingStyle = style;
            _hasPendingMessage = true;
            _pendingMessageSpinCount = _spinCount;
        }
    }
}

