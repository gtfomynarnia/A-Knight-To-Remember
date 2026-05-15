using AKTR.Core;
using AKTR.Core.Events;
using AKTR.Features.BossEncounter;
using AKTR.Features.Fame;
using AKTR.Managers;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace AKTR.Presentation
{
    public class GameHUDController : MonoBehaviour
    {
        // Events
        [SerializeField] private FameChangedEventSO _onFameChanged;
        [SerializeField] private IntEventSO _onCreditsAwarded;
        [SerializeField] private SwordMeterEventSO _onSwordMeterFull;
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private GameStateEventSO _onGameStateChanged;
        [SerializeField] private WinEvaluatedEventSO _onWinEvaluated;
        [SerializeField] private OnReelAnimCompleteSO _onReelAnimComplete;
        public event System.Action OnSpinAnimationComplete;

        // Anim Pending Data
        private FameData _pendingFameData;
        private bool _hasPendingFameUpdate;
        private int _pendingCredits;
        private bool _hasPendingCredits;

        // UI References
        [SerializeField] private BackdropConfigSO _backdropConfig;
        [SerializeField] private int _swordMeterMax = 5;
        private AKTR.Features.SwordMeter.SwordMeter _swordMeterSystem;
        private AKTR.Core.WinCalculator _winCalculator;
        private Label _tierNameLabel;
        private Label _creditsLabel;
        private Label _fameTierLabel;
        private Label _fameToNextLabel;
        private VisualElement _fameProgressFill;
        private VisualElement _swordProgressFill;
        private Label _swordStatusLabel;
        private VisualElement _backdropElement;
        private Label _backdropLabel;
        private Button _spinButton;
        private Label _backdropSubLabel;
        private Coroutine _winHighlightCoroutine;
        private AKTR.Core.ReelSystem _reelSystem;
        private int _displayedCredits;
        private Coroutine _creditRollupCoroutine;

        // Reel Animation Variables
        private Coroutine _spinAnimationCoroutine;
        private bool[] _reelStopped = new bool[5];
        private float _spinCycleSpeed = 0.06f;
        private float _decelerationSpeed = 0.15f;
        private float _spinDuration = 2.2f;
        private float _stopDelay = 0.35f;
        [SerializeField] private SymbolDefinitionSO[] _allSymbols;
        public bool IsSpinAnimationComplete => AllReelsStopped();

        // Bet System Variables
        [SerializeField] private BetSystemSO _betSystem;
        private Button _betUpButton;
        private Button _betDownButton;
        private Label _betValueLabel;

        private VisualElement[] _reelColumns = new VisualElement[5];

        // State
        private int _totalCredits;

        private readonly string[] _backdropColors = new string[]
        {
            "#1a1208",
            "#0a1a10",
            "#0a0a1a",
            "#1a0808"
        };

        private readonly string[] _backdropTierNames = new string[]
        {
            "Humble Village",
            "Growing Town",
            "Fortified City",
            "Legendary Fortress"
        };

        // Lifecycle
        private void OnEnable()
        {
            _onFameChanged.Register(HandleFameChanged);
            _onCreditsAwarded.Register(HandleCreditsAwarded);
            _onSpinComplete.Register(HandleSpinComplete);
            _onGameStateChanged.Register(HandleGameStateChanged);
            _onWinEvaluated.Register(HandleWinEvaluated);
            _onReelAnimComplete.Register(HandleReelAnimComplete);
        }

        private void OnDisable()
        {
            _onFameChanged.Unregister(HandleFameChanged);
            _onCreditsAwarded.Unregister(HandleCreditsAwarded);
            _onSpinComplete.Unregister(HandleSpinComplete);
            _onGameStateChanged.Unregister(HandleGameStateChanged);
            _onWinEvaluated.Unregister(HandleWinEvaluated);
            _onReelAnimComplete.Unregister(HandleReelAnimComplete);
        }

        private void Start()
        {
            var uiDocument = GetComponent<UIDocument>();
            uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnUIReady);
        }

        private void OnUIReady(GeometryChangedEvent evt)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.UnregisterCallback<GeometryChangedEvent>(OnUIReady);
            _swordMeterSystem = FindAnyObjectByType<AKTR.Features.SwordMeter.SwordMeter>();
            _winCalculator = FindAnyObjectByType<AKTR.Core.WinCalculator>();
            _reelSystem = FindAnyObjectByType<AKTR.Core.ReelSystem>();
            BindElements(root);
            BuildReelGrid(root);
            UpdateSpinButton(GameManager.Instance.CurrentState);
            UpdateBackdrop(1);
            UpdateBetDisplay();
        }

        // Binding
        private void BindElements(VisualElement root)
        {
            _tierNameLabel = root.Q<Label>("tier-name-label");
            _creditsLabel = root.Q<Label>("credits-label");
            _fameTierLabel = root.Q<Label>("fame-tier-label");
            _fameToNextLabel = root.Q<Label>("fame-to-next-label");
            _fameProgressFill = root.Q<VisualElement>("fame-progress-fill");
            _swordProgressFill = root.Q<VisualElement>("sword-progress-fill");
            _swordStatusLabel = root.Q<Label>("sword-status-label");
            _backdropElement = root.Q<VisualElement>("backdrop");
            _backdropLabel = root.Q<Label>("backdrop-label");
            _spinButton = root.Q<Button>("spin-button");
            _backdropSubLabel = root.Q<Label>("backdrop-sub-label");
            _betUpButton = root.Q<Button>("bet-up-button");
            _betDownButton = root.Q<Button>("bet-down-button");
            _betValueLabel = root.Q<Label>("bet-value-label");

            _betUpButton.clicked += OnBetUpClicked;
            _betDownButton.clicked += OnBetDownClicked;

            for (int i = 0; i < 5; i++)
            {
                _reelColumns[i] = root.Q<VisualElement>($"reel-{i}");
            }

            _spinButton.clicked += OnSpinButtonClicked;
        }

        private void BuildReelGrid(VisualElement root)
        {
            for (int reel = 0; reel < 5; reel++)
            {
                for (int row = 0; row < 3; row++)
                {
                    var cell = new VisualElement();
                    cell.name = $"cell-{reel}-{row}";
                    cell.AddToClassList("symbol-cell");

                    var spriteElement = new VisualElement();
                    spriteElement.name = $"cell-sprite-{reel}-{row}";
                    spriteElement.AddToClassList("symbol-sprite");

                    var label = new Label("?");
                    label.name = $"cell-label-{reel}-{row}";
                    label.AddToClassList("symbol-cell-label");

                    cell.Add(spriteElement);
                    cell.Add(label);
                    _reelColumns[reel].Add(cell);
                }
            }
        }

        // Event Handlers
        private void HandleFameChanged(FameData data)
        {
            if (_tierNameLabel == null) return;
            _pendingFameData = data;
            _hasPendingFameUpdate = true;
        }

        private void HandleCreditsAwarded(int credits)
        {
            if (_creditsLabel == null) return;
            _pendingCredits = credits;
            _hasPendingCredits = true;
        }

        private void HandleSpinComplete(SpinResult result)
        {
            if (_swordProgressFill == null) return;
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (_spinButton != null)
                UpdateSpinButton(state);

            if (state == GameManager.GameState.Idle)
            {
                if (_swordStatusLabel != null)
                    _swordStatusLabel.text = "CHARGING";
            }

            if (state == GameManager.GameState.Spinning)
            {
                if (_winHighlightCoroutine != null)
                {
                    StopCoroutine(_winHighlightCoroutine);
                    _winHighlightCoroutine = null;
                }
                ClearWinHighlight(GetComponent<UIDocument>().rootVisualElement);
                StartSpinAnimation();
            }
        }

        private void HandleWinEvaluated(WinResult result)
        {
            if (!result.HasWin) return;
            StartCoroutine(WaitForReelsToStopThenHighlight(result));
        }

        private void HandleReelAnimComplete()
        {
            // Update fame after reels stop
            if (_hasPendingFameUpdate)
            {
                ApplyFameUpdate(_pendingFameData);
                _hasPendingFameUpdate = false;
            }

            // Update credits after reels stop
            if (_hasPendingCredits)
            {
                int newTotal = _totalCredits + _pendingCredits;
                if (_creditRollupCoroutine != null) StopCoroutine(_creditRollupCoroutine);
                _creditRollupCoroutine = StartCoroutine(RollupCredits(_totalCredits, newTotal));
                _totalCredits = newTotal;
                _hasPendingCredits = false;
            }

            // Update sword meter after reels stop
            if (_swordMeterSystem != null && _winCalculator != null)
            {
                if (_winCalculator.IsPoweredUp)
                {
                    _swordProgressFill.style.width = Length.Percent(100f);
                    _swordStatusLabel.text = $"3x ACTIVE · {_winCalculator.PoweredUpSpinsRemaining} SPINS LEFT";
                }
                else
                {
                    _swordProgressFill.style.width = Length.Percent(_swordMeterSystem.FillPercent * 100f);
                    _swordStatusLabel.text = _swordMeterSystem.FillPercent >= 1f ? "FULL!" : "CHARGING";
                }
            }
        }

        private void UpdateBackdrop(int tier)
        {
            if (_backdropElement == null) return;
            if (_backdropConfig == null) return;

            Sprite backdropSprite = _backdropConfig.GetBackdropForTier(tier);

            if (backdropSprite != null)
            {
                _backdropElement.style.backgroundImage = new StyleBackground(backdropSprite);
                _backdropElement.style.backgroundColor = new StyleColor(Color.clear);
            }
            else
            {
                ColorUtility.TryParseHtmlString(_backdropColors[tier - 1], out Color c);
                _backdropElement.style.backgroundColor = new StyleColor(c);
            }

            if (_backdropLabel != null)
                _backdropLabel.style.display = DisplayStyle.None;

            if (_backdropSubLabel != null)
                _backdropSubLabel.style.display = DisplayStyle.None;
        }

        private void UpdateSpinButton(GameManager.GameState state)
        {
            if (_spinButton == null) return;

            bool canSpin = state == GameManager.GameState.Idle;
            bool bossWaiting = state == GameManager.GameState.BossReelWaiting;

            _spinButton.SetEnabled(canSpin || bossWaiting);
            _spinButton.text = bossWaiting ? "STOP" : "▶";
        }

        private void OnSpinButtonClicked()
        {
            var state = GameManager.Instance.CurrentState;

            if (state == GameManager.GameState.Idle)
            {
                GameManager.Instance.TransitionTo(GameManager.GameState.Spinning);
            }
            else if (state == GameManager.GameState.BossReelWaiting)
            {
                var bossSystem = FindAnyObjectByType<BossEncounterSystem>();
                bossSystem?.TriggerBossReelStop();
            }
        }

        private void OnBetUpClicked()
        {
            if (GameManager.Instance.CurrentState != GameManager.GameState.Idle) return;
            _betSystem.IncreaseBet();
            UpdateBetDisplay();
        }

        private void OnBetDownClicked()
        {
            if (GameManager.Instance.CurrentState != GameManager.GameState.Idle) return;
            _betSystem.DecreaseBet();
            UpdateBetDisplay();
        }

        private void UpdateBetDisplay()
        {
            if (_betValueLabel != null)
                _betValueLabel.text = _betSystem.CurrentBet.ToString("F2");
        }

        // Helpers
        private string GetTierName(int tier)
        {
            return tier switch
            {
                1 => "Unknown Knight",
                2 => "Recognized Hero",
                3 => "Celebrated Champion",
                4 => "Knight of Legend",
                _ => "Unknown Knight"
            };
        }

        private float GetTierFillPercent(int fame, int tier)
        {
            if (tier >= 4) return 1f;
            int tierStart = tier == 1 ? 0 : tier == 2 ? 100 : 250;
            int tierEnd = tier == 1 ? 100 : tier == 2 ? 250 : 500;
            return Mathf.Clamp01((float)(fame - tierStart) / (tierEnd - tierStart));
        }

        private IEnumerator WaitForReelsToStopThenHighlight(WinResult result)
        {
            while (!AllReelsStopped())
                yield return null;

            yield return new WaitForSeconds(0.1f);

            if (_winHighlightCoroutine != null) StopCoroutine(_winHighlightCoroutine);
            _winHighlightCoroutine = StartCoroutine(HighlightWinningCells(result));
        }

        private IEnumerator HighlightWinningCells(WinResult result)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            var winningCells = new System.Collections.Generic.HashSet<(int reel, int row)>();
            var lastGrid = GetComponent<UIDocument>().rootVisualElement;

            foreach (var line in result.WinLines)
            {
                for (int reel = line.StartReel; reel < line.StartReel + line.ReelCount; reel++)
                {
                    for (int row = 0; row < 3; row++)
                    {
                        var spriteElement = root.Q<VisualElement>($"cell-sprite-{reel}-{row}");
                        var label = root.Q<Label>($"cell-label-{reel}-{row}");

                        // check if this cell contains the winning symbol
                        // by reading the symbol name from the label or checking the grid
                        string cellSymbolName = label?.text ?? "";

                        // if label is hidden the symbol name is on the sprite element's tooltip
                        // instead we track winning cells via the ReelSystem grid directly
                        if (_reelSystem != null)
                        {
                            var symbol = _reelSystem.GetSymbol(reel, row);
                            if (symbol != null && symbol.Id == line.SymbolId)
                            {
                                winningCells.Add((reel, row));
                            }
                        }
                    }
                }
            }

            // apply win and dim classes
            for (int reel = 0; reel < 5; reel++)
            {
                for (int row = 0; row < 3; row++)
                {
                    var cell = root.Q<VisualElement>($"cell-{reel}-{row}");
                    if (cell == null) continue;

                    if (winningCells.Contains((reel, row)))
                    {
                        cell.AddToClassList("symbol-cell--win");
                        cell.RemoveFromClassList("symbol-cell--dimmed");
                    }
                    else
                    {
                        cell.AddToClassList("symbol-cell--dimmed");
                        cell.RemoveFromClassList("symbol-cell--win");
                    }
                }
            }

            yield return new WaitForSeconds(2f);

            for (int pulse = 0; pulse < 3; pulse++)
            {
                foreach (var (reel, row) in winningCells)
                {
                    var cell = root.Q<VisualElement>($"cell-{reel}-{row}");
                    cell?.RemoveFromClassList("symbol-cell--win");
                }

                yield return new WaitForSeconds(0.2f);

                foreach (var (reel, row) in winningCells)
                {
                    var cell = root.Q<VisualElement>($"cell-{reel}-{row}");
                    cell?.AddToClassList("symbol-cell--win");
                }

                // The wait in between each pulse
                yield return new WaitForSeconds(0.33f);
            }

            ClearWinHighlight(root);
        }

        private void ClearWinHighlight(VisualElement root)
        {
            for (int reel = 0; reel < 5; reel++)
            {
                for (int row = 0; row < 3; row++)
                {
                    var cell = root.Q<VisualElement>($"cell-{reel}-{row}");
                    if (cell == null) continue;
                    cell.RemoveFromClassList("symbol-cell--win");
                    cell.RemoveFromClassList("symbol-cell--dimmed");
                }
            }
        }

        private IEnumerator RollupCredits(int fromValue, int toValue)
        {
            float duration = 3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                int displayValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, easedT));
                _creditsLabel.text = $"CREDITS: {displayValue}";
                yield return null;
            }

            _creditsLabel.text = $"CREDITS: {toValue}";
            _creditRollupCoroutine = null;
        }

        private int CountSwordsInGrid(SymbolDefinitionSO[,] grid)
        {
            int count = 0;
            foreach (var symbol in grid)
            {
                if (symbol != null && symbol.IsSword)
                    count++;
            }
            return count;
        }

        private void ApplyFameUpdate(FameData data)
        {
            _tierNameLabel.text = GetTierName(data.Tier);
            _fameTierLabel.text = $"TIER {data.Tier} · {data.CurrentFame} FAME";
            _fameToNextLabel.text = data.Tier < 4 ? $"{data.FameToNextTier} TO TIER {data.Tier + 1}" : "MAX TIER";

            float fillPercent = GetTierFillPercent(data.CurrentFame, data.Tier);
            _fameProgressFill.style.width = Length.Percent(fillPercent * 100f);

            if (data.TierChanged)
                UpdateBackdrop(data.Tier);
        }

        // Reel Spin Animation
        private void StartSpinAnimation()
        {
            if (_spinAnimationCoroutine != null)
                StopCoroutine( _spinAnimationCoroutine );

            for (int i = 0; i < 5; i++)
            {
                _reelStopped[i] = false;
            }

            _spinAnimationCoroutine = StartCoroutine(SpinAnimationRoutine());
        }

        private IEnumerator SpinAnimationRoutine()
        {
            var root = GetComponent<UIDocument>(). rootVisualElement;

            // start stop coroutines for each reel
            for (int reel = 0; reel < 5; reel++)
            {
                StartCoroutine(StopReelAfterDelay(reel, _spinDuration + reel * _stopDelay));
            }

            // keep cycling symbols on all reels until they stop
            while (!AllReelsStopped())
            {
                for (int reel = 0; reel < 5; reel++)
                {
                    if (_reelStopped[reel]) continue;
                    for (int row = 0; row < 3; row++)
                    {
                        var randomSymbol = GetRandomSymbol();
                        UpdateCell(root, reel, row, randomSymbol);
                    }
                }
                yield return new WaitForSeconds(_spinCycleSpeed);
            }

            OnSpinAnimationComplete?.Invoke();
            _onReelAnimComplete.Raise();
        }

        private IEnumerator StopReelAfterDelay(int reel, float delay)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            // wait for most of the spin duration
            yield return new WaitForSeconds(delay - 0.4f);

            // deceleration phase: slow down before stopping
            for (int i = 0; i < 3; i++)
            {
                for (int row = 0; row < 3; row++)
                {
                    var randomSymbol = GetRandomSymbol();
                    UpdateCell(root, reel, row, randomSymbol);
                }
                yield return new WaitForSeconds(_decelerationSpeed + i * 0.05f);
            }

            // land on final result
            if (_reelSystem != null)
            {
                for (int row = 0; row <3; row++)
                {
                    var symbol = _reelSystem.GetSymbol(reel, row);
                    if (symbol != null)
                        UpdateCell(root, reel, row, symbol);
                }
            }

            _reelStopped[reel] = true;
        }

        private void UpdateCell(VisualElement root, int reel, int row, SymbolDefinitionSO symbol)
        {
            var cell = root.Q<VisualElement>($"cell -{reel}-{row}");
            var spriteElement = root.Q<VisualElement>($"cell-sprite-{reel}-{row}");
            var label = root.Q<Label>($"cell-label-{reel}-{row}");

            if (symbol == null) return;

            if (spriteElement != null)
            {
                if (symbol.Sprite != null)
                {
                    spriteElement.style.backgroundImage = new StyleBackground(symbol.Sprite);
                    spriteElement.style.display = DisplayStyle.Flex;
                    if (label != null) label.style.display = DisplayStyle.None;
                }
                else
                {
                    spriteElement.style.display= DisplayStyle.None;
                    if (label != null)
                    {
                        label.style.display = DisplayStyle.Flex;
                        label.text = symbol.SymbolName;
                    }
                }
            }

            // apply symbol type classes
            if (cell != null)
            {
                cell.RemoveFromClassList("symbol-cell--sword");
                cell.RemoveFromClassList("symbol-cell--scatter");
                cell.RemoveFromClassList("symbol-cell--dragon");

                if (symbol.IsSword)
                    cell.AddToClassList("symbol-cell--sword");
                if (symbol.IsScatter)
                    cell.AddToClassList("symbol-cell--scatter");
                if (symbol.IsGoldDragon)
                    cell.AddToClassList("symbol-cell--dragon");
            }
        }

        private bool AllReelsStopped()
        {
            foreach (var stopped in _reelStopped)
                if (!stopped) return false;
            return true;
        }

        private SymbolDefinitionSO GetRandomSymbol()
        {
            if (_allSymbols == null || _allSymbols.Length == 0) return null;
            return _allSymbols[Random.Range(0, _allSymbols.Length)];
        }
    }
}
