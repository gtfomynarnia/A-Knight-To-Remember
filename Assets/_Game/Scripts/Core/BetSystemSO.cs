using UnityEngine;

namespace AKTR.Core
{
    [CreateAssetMenu(menuName = "AKTR/Bet System")]
    public class BetSystemSO : ScriptableObject
    {
        [SerializeField] private float[] _betIncrements = { 0.50f, 1.00f, 2.00f, 5.00f, 10.00f, 25.00f, 50.00f, 100.00f };
        [SerializeField] private int _currentBetIndex = 1;

        public float CurrentBet => _betIncrements[_currentBetIndex];
        public float MaxBet => _betIncrements[_betIncrements.Length - 1];
        public float[] BetIncrements => _betIncrements;

        public void IncreaseBet()
        {
            if (_currentBetIndex < _betIncrements.Length -1)
                _currentBetIndex++;
        }

        public void DecreaseBet()
        {
            if (_currentBetIndex > 0)
                _currentBetIndex--;
        }

        public void SetMaxBet()
        {
            _currentBetIndex = _betIncrements.Length - 1;
        }

        public void Reset()
        {
            _currentBetIndex = 1;
        }
    }
}


