using UnityEngine;


namespace AKTR.Core
{
    [CreateAssetMenu(menuName = "AKTR/Reels/Reel Strip")]
    public class ReelStripsSO : ScriptableObject
    {
        [SerializeField] private SymbolDefinitionSO[] _symbols;

        public SymbolDefinitionSO[] Symbols => _symbols;

        public int Length => _symbols.Length;

        public SymbolDefinitionSO GetSymbolAt(int index)
        {
            return _symbols[index % _symbols.Length];
        }
    }
}

