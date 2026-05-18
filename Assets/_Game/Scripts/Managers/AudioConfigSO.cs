using UnityEngine;

namespace AKTR.Managers
{
    [CreateAssetMenu(menuName = "AKTR/Audio Config")]
    public class AudioConfigSO : ScriptableObject
    {
        [Header("Reel Sounds")]
        public AudioClip reelSpin;
        public AudioClip reelStop;

        [Header("Win Sounds")]
        public AudioClip smallWin;
        public AudioClip bigWin;
        public AudioClip megaWin;
        public AudioClip noWin;

        [Header("Sword Meter")]
        public AudioClip swordCharge;
        public AudioClip swordPoweredUp;

        [Header("Fame")]
        public AudioClip tierUp;

        [Header("Boss Encounter")]
        public AudioClip bossTriggered;
        public AudioClip bossCriticalHit;
        public AudioClip bossHit;
        public AudioClip bossGlancingBlow;
        public AudioClip bossDefeated;

        [Header("Music")]
        public AudioClip musicTier1;
        public AudioClip musicTier2;
        public AudioClip musicTier3;
        public AudioClip musicTier4;
        public AudioClip musicBoss;

        [Header("Betting")]
        public AudioClip raiseBet;
        public AudioClip lowerBet;
    }
}
