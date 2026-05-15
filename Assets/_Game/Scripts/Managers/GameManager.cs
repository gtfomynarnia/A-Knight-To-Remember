using UnityEngine;
using AKTR.Core.Events;
using System.Collections;
using AKTR.Presentation;

namespace AKTR.Managers
{
    public class GameManager : MonoBehaviour
    {
        // Variables
        private GameHUDController _hudController;

        // Events
        [SerializeField] private SwordMeterEventSO _onSwordMeterFull;
        [SerializeField] private GameStateEventSO _onGameStateChanged;

        // State Definition
        public enum GameState
        {
            Idle,
            Spinning,
            Evaluating,
            Paying,
            BossEncounter,
            BossReelWaiting,
            BossReelRevealing,
            BossComplete,
            GameOver
        }

        // Current State
        public GameState CurrentState { get; private set; }

        // Singleton
        public static GameManager Instance { get; private set; }

        // Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            CurrentState = GameState.Idle;
            _onGameStateChanged.Raise(CurrentState);
            _hudController = FindAnyObjectByType<GameHUDController>();
            if (_hudController != null)
                _hudController.OnSpinAnimationComplete += OnAnimationComplete;
            Debug.Log($"GameState -> {CurrentState}");
        }

        // State Machine
        public void TransitionTo(GameState newState)
        {
            if (!IsValidTransition(CurrentState, newState))
            {
                Debug.LogWarning($"Invalid state transition: {CurrentState} -> {newState}");
                return;
            }

            CurrentState = newState;
            Debug.Log($"GameState -> {CurrentState}");
            _onGameStateChanged.Raise(CurrentState);
        }

        private bool IsValidTransition(GameState from, GameState to)
        {
            return (from, to) switch
            {
                // Normal Transitions
                (GameState.Idle, GameState.Spinning) => true,
                (GameState.Spinning, GameState.Evaluating) => true,
                (GameState.Evaluating, GameState.Paying) => true,
                (GameState.Evaluating, GameState.Idle) => true,
                (GameState.Paying, GameState.Idle) => true,
                // Boss Transitions
                (GameState.Evaluating, GameState.BossEncounter) => true,
                (GameState.BossEncounter, GameState.BossReelWaiting) => true,
                (GameState.BossReelWaiting, GameState.BossReelRevealing) => true,
                (GameState.BossReelRevealing, GameState.BossReelWaiting) => true,
                (GameState.BossReelRevealing, GameState.BossComplete) => true,
                (GameState.BossComplete, GameState.BossEncounter) => true,
                (GameState.BossComplete, GameState.Paying) => true,
                (GameState.BossEncounter, GameState.Paying) => true,
                // Game Over Transition
                (_, GameState.GameOver) => true,
                _ => false
            };
        }

        private void OnEnable()
        {
            _onGameStateChanged.Register(HandleGameStateChanged);
        }

        private void OnDisable()
        {
            _onGameStateChanged.Register(HandleGameStateChanged);
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (state == GameState.Evaluating)
            {
                StartCoroutine(TransitionToPaying());
            }

            if (state == GameState.Paying)
            {
                StartCoroutine(TransitionToIdle());
            }
        }

        private IEnumerator TransitionToIdle()
        {
            yield return new WaitForSeconds(2);
            TransitionTo(GameState.Idle);
        }

        private IEnumerator TransitionToPaying()
        {
            yield return new WaitForSeconds(0.1f);

            if (CurrentState == GameState.Evaluating)
            {
                TransitionTo(GameState.Paying);
            }
        }

        private void Update()
        {
            // Spin the Reels
            if (Input.GetKeyUp(KeyCode.Space) && CurrentState == GameState.Idle)
            {
                TransitionTo(GameState.Spinning);
            }

            // Toggle Sword Meter Multiplier
            if (Input.GetKeyUp(KeyCode.M))
            {
                _onSwordMeterFull.Raise();
                Debug.Log("TEST: Sword meter manually triggered.");
            }
        }

        private void OnAnimationComplete()
        {
            if (CurrentState == GameState.Spinning)
            {
                TransitionTo(GameState.Evaluating);
            }
        }

        private void OnDestroy()
        {
            if (_hudController != null)
                _hudController.OnSpinAnimationComplete -= OnAnimationComplete;
        }
    }
}
