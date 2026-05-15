using System.Runtime.CompilerServices;
using UnityEngine;


namespace AKTR.Core
{
    [CreateAssetMenu(menuName = "AKTR/Symbols/Symbol Definition")]
    public class SymbolDefinitionSO : ScriptableObject
    {
        [Header("Pay Multipliers")]
        [SerializeField] private float _threeOfAKindMultiplier;
        [SerializeField] private float _fourOfAKindMultiplier;
        [SerializeField] private float _fiveOfAKindMultiplier;

        [SerializeField] private int _id;
        [SerializeField] private string _symbolName;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private bool _isSword;
        [SerializeField] private bool _isScatter;
        [SerializeField] private bool _isGoldDragon;

        public int Id => _id;
        public string SymbolName => _symbolName;
        public Sprite Sprite => _sprite;
        public bool IsSword => _isSword;
        public bool IsScatter => _isScatter;
        public bool IsGoldDragon => _isGoldDragon;

        public float GetMultiplier(int reelCount)
        {
            return reelCount switch
            {
                3 => _threeOfAKindMultiplier,
                4 => _fourOfAKindMultiplier,
                5 => _fiveOfAKindMultiplier,
                _ => 0,
            };
        }
    }
}

