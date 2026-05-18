using UnityEngine;
using AKTR.Core.Events;
using AKTR.Core;

namespace AKTR.Managers
{
    public class AudioManager : MonoBehaviour
    {
        // Configuration
        [SerializeField] private AudioConfigSO _audioConfig;

        // Events
        [SerializeField] private GameStateEventSO _onGameStateChanged;
        [SerializeField] private WinEvaluatedEventSO _onWinEvaluated;
        [SerializeField] private IntEventSO _onCreditsAwarded;
        [SerializeField] private FameChangedEventSO _onFameChanged;
        [SerializeField] private SwordMeterEventSO _onSwordMeterFull;
        [SerializeField] private bossTriggeredEventSO _onBossTriggered;
        [SerializeField] private ReelBattleResultEventSO _onReelBattleResult;
        [SerializeField] private OnReelAnimCompleteSO _onReelAnimComplete;
        [SerializeField] private SpinCompleteEventSO _onSpinComplete;
        [SerializeField] private ReelStoppedEventSO _onReelStopped;

        // Audio Sources
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private AudioSource _reelSource;

        // State
        private int _currentMusicTier = 1;
        private bool _inBossEncounter;
        private int _pendingCredits;

        // Lifecycle
        private void Awake()
        {
            // create three audio sources on this GameObject
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = 0.6f;

            _reelSource = gameObject.AddComponent<AudioSource>();
            _reelSource.playOnAwake = false;
            _reelSource.loop = true;
            _reelSource.volume = 0.8f;
        }

        private void OnEnable()
        {
            _onGameStateChanged.Register(HandleGameStateChanged);
            _onWinEvaluated.Register(HandleWinEvaluated);
            _onCreditsAwarded.Register(HandleCreditsAwarded);
            _onFameChanged.Register(HandleFameChanged);
            _onSwordMeterFull.Register(HandleSwordMeterFull);
            _onBossTriggered.Register(HandleBossTriggered);
            _onReelBattleResult.Register(HandleReelBattleResult);
            _onReelAnimComplete.Register(HandleReelAnimComplete);
            _onReelStopped.Register(HandleReelStopped);
        }

        private void OnDisable()
        {
            _onGameStateChanged.Unregister(HandleGameStateChanged);
            _onWinEvaluated.Unregister(HandleWinEvaluated);
            _onCreditsAwarded.Unregister(HandleCreditsAwarded);
            _onFameChanged.Unregister(HandleFameChanged);
            _onSwordMeterFull.Unregister(HandleSwordMeterFull);
            _onBossTriggered.Unregister(HandleBossTriggered);
            _onReelBattleResult.Unregister(HandleReelBattleResult);
            _onReelAnimComplete.Unregister(HandleReelAnimComplete);
            _onReelStopped.Unregister(HandleReelStopped);
        }

        private void Start()
        {
            PlayMusic(_audioConfig.musicTier1);
        }

        // Handlers
        private void HandleGameStateChanged(GameManager.GameState state)
        {
            switch (state)
            {
                case GameManager.GameState.Spinning:
                    _inBossEncounter = false;
                    PlayReelSpin();
                    break;

                case GameManager.GameState.BossEncounter:
                    _inBossEncounter = true;
                    PlayMusic(_audioConfig.musicBoss);
                    break;

                case GameManager.GameState.Paying:
                    if (_inBossEncounter)
                    {
                        PlayMusic(GetMusicForTier(_currentMusicTier));
                        _inBossEncounter = false;
                    }
                    break;
            }
        }

        private void HandleReelAnimComplete()
        {
            StopReelSpin();
            PlayWinSound(_pendingCredits);
            _pendingCredits = 0;
        }

        private void HandleWinEvaluated(WinResult result)
        {
            // store for use after reels stop
        }

        private void HandleCreditsAwarded(int credits)
        {
            _pendingCredits = credits;
        }

        private void HandleFameChanged(FameData data)
        {
            _currentMusicTier = data.Tier;

            if (data.TierChanged && !_inBossEncounter)
            {
                PlaySFX(_audioConfig.tierUp);
                PlayMusic(GetMusicForTier(data.Tier));
            }
        }

        private void HandleSwordMeterFull()
        {
            PlaySFX(_audioConfig.swordPoweredUp);
        }

        private void HandleBossTriggered(BossData data)
        {
            Debug.Log("Boss triggered audio fired");
            PlaySFX(_audioConfig.bossTriggered);
        }

        private void HandleReelBattleResult(ReelBattleResult result)
        {
            AudioClip clip = result.ResultType switch
            {
                "Critical Hit" => _audioConfig.bossCriticalHit,
                "Hit" => _audioConfig.bossHit,
                "Glancing Blow" => _audioConfig.bossGlancingBlow,
                _ => _audioConfig.bossHit
            };

            PlaySFX(clip);
        }

        private void HandleReelStopped()
        {
            // Temp Debug
            Debug.Log("HandleReelStopped fired");

            PlaySFX(_audioConfig.reelStop);
        }

        // Audio Helpers
        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip) return;

            _musicSource.clip = clip;
            _musicSource.Play();
        }

        private void PlayReelSpin()
        {
            if (_audioConfig.reelSpin == null) return;
            _reelSource.clip = _audioConfig.reelSpin;
            _reelSource.Play();
        }

        private void StopReelSpin()
        {
            _reelSource.Stop();
        }

        private AudioClip GetMusicForTier(int tier)
        {
            return tier switch
            {
                1 => _audioConfig.musicTier1,
                2 => _audioConfig.musicTier2,
                3 => _audioConfig.musicTier3,
                4 => _audioConfig.musicTier4,
                _ => _audioConfig.musicTier1
            };
        }

        private void PlayWinSound(int credits)
        {
            if (credits <= 0)
            {
                PlaySFX(_audioConfig.noWin);
                return;
            }

            var winCalc = FindAnyObjectByType<WinCalculator>();
            int megaThreshold = winCalc != null ? winCalc.MegaWinThreshold : 200;
            int bigThreshold = winCalc != null ? winCalc.BigWinThreshold : 50;

            if (credits >= megaThreshold)
                PlaySFX(_audioConfig.megaWin);
            else if (credits >= bigThreshold)
                PlaySFX(_audioConfig.bigWin);
            else
                PlaySFX(_audioConfig.smallWin);
        }

        public void PlayBetSound(bool increased)
        {
            PlaySFX(increased ? _audioConfig.raiseBet : _audioConfig.lowerBet);
        }
    }
}


