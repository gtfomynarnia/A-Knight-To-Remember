using UnityEngine;
using System.Collections.Generic;
using AKTR.Core.Events;
using AKTR.Managers;

namespace AKTR.Core
{
    public class PaylineEvaluator : MonoBehaviour
    {
        // Configuration
        [SerializeField] private SymbolDefinitionSO[] _allSymbols;
        [SerializeField] private int[] _payTable;
        [SerializeField] private BetSystemSO _betSystem;

        // Events
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private WinEvaluatedEventSO _onWinEvaluated;

        // Constants
        private const int ReelCount = 5;
        private const int RowCount = 3;
        private const int MinWinLength = 3;

        // Lifecycle
        private void OnEnable()
        {
            _onSpinComplete.Register(HandleSpinComplete);
        }

        private void OnDisable()
        {
            _onSpinComplete.Unregister(HandleSpinComplete);
        }

        // Evaluation
        private void HandleSpinComplete(SpinResult spinResult)
        {
            List<WinLine> winLines = Evaluate(spinResult.Grid);
            int baseCredits = CalculateCredits(winLines);

            var result = new WinResult(baseCredits, baseCredits, false, false, winLines);
            _onWinEvaluated.Raise(result);
        }

        private List<WinLine> Evaluate(SymbolDefinitionSO[,] grid)
        {
            var winLines = new List<WinLine>();

            foreach (var symbol in _allSymbols)
            {
                if (symbol.IsScatter)
                    continue;

                int bestReelCount = 0;
                int bestStartReel = 0;
                int[] bestSymbolsPerReel = null;

                for (int startReel = 0; startReel <= ReelCount - MinWinLength; startReel++)
                {
                    int[] symbolsPerReel = new int[ReelCount];
                    int consecutiveReels = 0;

                    for (int reel = startReel; reel < ReelCount; reel++)
                    {
                        int count = 0;
                        for (int row = 0; row < RowCount; row++)
                        {
                            if (grid[reel, row] != null && grid[reel, row].Id == symbol.Id)
                                count++;
                        }

                        if (count > 0)
                        {
                            symbolsPerReel[reel] = count;
                            consecutiveReels++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (consecutiveReels >= MinWinLength && consecutiveReels > bestReelCount)
                    {
                        bestReelCount = consecutiveReels;
                        bestStartReel = startReel;
                        bestSymbolsPerReel = symbolsPerReel;
                    }
                }

                if (bestReelCount >= MinWinLength)
                {
                    winLines.Add(new WinLine(
                        symbol.Id,
                        symbol.SymbolName,
                        bestReelCount,
                        bestSymbolsPerReel,
                        bestStartReel
                    ));
                }
            }

            return winLines;
        }

        private int CalculateCredits(List<WinLine> winLines)
        {
            int total = 0;
            foreach (var line in winLines)
            {
                total += GetPay(line);
            }
            return total;
        }

        private int GetPay(WinLine line)
        {
            var symbol = GetSymbolById(line.SymbolId);
            if (symbol == null) return 0;

            float multiplier = symbol.GetMultiplier(line.ReelCount);
            float bet = _betSystem != null ? _betSystem.CurrentBet : 0.50f;

            return Mathf.RoundToInt(bet * multiplier);
        }

        private SymbolDefinitionSO GetSymbolById(int id)
        {
            foreach (var symbol in _allSymbols)
            {
                if (symbol != null && symbol.Id == id)
                    return symbol;
            }
            return null;
        }
    }
}

