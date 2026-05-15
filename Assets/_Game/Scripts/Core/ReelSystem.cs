using UnityEngine;
using System.Collections;
using AKTR.Core.Events;
using AKTR.Managers;

namespace AKTR.Core
{
    public class ReelSystem : MonoBehaviour
    {
        // Reel Configuration
        [SerializeField] private ReelStripsSO[] _reelStrips;
        [SerializeField] private float _spinDuration = 2f;
        [SerializeField] private float _stopDelay = 0.3f;

        // Events
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private GameStateEventSO _onGameStateChanged;

        // State
        private int[] _reelPositions = new int[5];
        private SymbolDefinitionSO[,] _currentGrid = new SymbolDefinitionSO[5, 3];
        private bool _isSpinning;

        // Lifecycle
        private void OnEnable()
        {
            _onGameStateChanged.Register(HandleGameStateChanged);
        }

        private void OnDisable()
        {
            _onGameStateChanged.Unregister(HandleGameStateChanged);
        }

        // State Listener
        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.Spinning)
            {
                StartCoroutine(SpinRoutine());
            }
        }

        // Spin Logic
        private IEnumerator SpinRoutine()
        {
            _isSpinning = true;

            DetermineResults();

            for (int reel = 0; reel <5; reel++)
            {
                int reelIndex = reel;
                StartCoroutine(StopReelAfterDelay(reelIndex, _spinDuration + reelIndex * _stopDelay));
            }

            yield return new WaitForSeconds(_spinDuration + 4 * _stopDelay + 0.1f);

            _isSpinning = false;

            bool hasSword = CheckForSword();
            var result = new SpinResult(_currentGrid, hasSword);
            

            for (int reel = 0; reel < 5; reel++)
            {
                string row = "";
                for (int row2 = 0; row2 < 3; row2++)
                {
                    row += _currentGrid[reel, row2]?.SymbolName + " | ";
                }
                /* Shows the result of each reel in the Console
                Debug.Log($"Reel {reel}: {row}");
                */
            }

            GameManager.Instance.TransitionTo(GameManager.GameState.Evaluating);
            _onSpinComplete.Raise(result);
        }

        private IEnumerator StopReelAfterDelay(int reelIndex, float delay)
        {
            yield return new WaitForSeconds(delay);
            LandReel(reelIndex);
        }

        private void DetermineResults()
        {
            for (int reel = 0; reel < 5; reel++)
            {
                _reelPositions[reel] = Random.Range(0, _reelStrips[reel].Length);
            }
        }

        private void LandReel(int reelIndex)
        {
            ReelStripsSO strip = _reelStrips[reelIndex];
            int stopPosition = _reelPositions[reelIndex];

            for (int row = 0; row <3; row++)
            {
                _currentGrid[reelIndex, row] = strip.GetSymbolAt(stopPosition + row);
            }
        }

        private bool CheckForSword()
        {
            foreach (var symbol in _currentGrid)
            {
                if (symbol != null && symbol.IsSword)
                    return true;
            }
            return false;
        }

        // Public Access
        public SymbolDefinitionSO GetSymbol(int reel, int row)
        {
            return _currentGrid[reel, row];
        }

    }
}
