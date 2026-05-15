using JetBrains.Annotations;
using UnityEngine.Rendering.Universal.Internal;
using AKTR.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace AKTR.Core.Events
{
    public readonly struct SpinResult
    {
        public readonly SymbolDefinitionSO[,] Grid;
        public readonly bool HasSwordSymbol;

        public SpinResult(SymbolDefinitionSO[,] grid, bool hasSword)
        {
            Grid = grid;
            HasSwordSymbol = hasSword;
        }
    }

    public readonly struct WinResult
    {
        public readonly int BaseCredits;
        public readonly int MultipliedCredits;
        public readonly bool IsBigWin;
        public readonly bool IsMegaWin;
        public readonly List<WinLine> WinLines;
        
        public bool HasWin => BaseCredits > 0;

        public WinResult(int baseCredits, int multiplied, bool bigWin, bool megaWin, List<WinLine> winLines)
        {
            BaseCredits = baseCredits;
            MultipliedCredits = multiplied;
            IsBigWin = bigWin;
            IsMegaWin = megaWin;
            WinLines = winLines;
        }
    }

    public readonly struct FameData
    {
        public readonly int CurrentFame;
        public readonly int FameToNextTier;
        public readonly int Tier;
        public readonly bool TierChanged;

        public FameData(int current, int toNext, int tier, bool tierChanged)
        {
            CurrentFame = current;
            FameToNextTier = toNext;
            Tier = tier;
            TierChanged = tierChanged;
        }
    }


    public readonly struct BossData
    {
        public readonly int Tier;
        public readonly bool IsGoldDragon;
        public readonly int AwardPool;
        public readonly string EnemyName;

        public BossData(int tier, bool isGoldDragon, int awardPool, string enemyName)
        {
            Tier = tier;
            IsGoldDragon = isGoldDragon;
            AwardPool = awardPool;
            EnemyName = enemyName;
        }
    }

    public readonly struct ReelBattleResult
    {
        public readonly int ReelIndex;
        public readonly string ResultType;
        public readonly int CreditsAwarded;

        public ReelBattleResult(int reelIndex, string resultType, int creditsAwarded)
        {
            ReelIndex = reelIndex;
            ResultType = resultType;
            CreditsAwarded = creditsAwarded;
        }
    }

    public readonly struct WinLine
    {
        public readonly int SymbolId;
        public readonly string SymbolName;
        public readonly int ReelCount;
        public readonly int[] SymbolsPerReel;
        public readonly int StartReel;

        public WinLine(int symbolId, string symbolName, int reelCount, int[] symbolsPerReel, int startReel)
        {
            SymbolId = symbolId;
            SymbolName = symbolName;
            ReelCount = reelCount;
            SymbolsPerReel = symbolsPerReel;
            StartReel = startReel;
        }
    }

}
