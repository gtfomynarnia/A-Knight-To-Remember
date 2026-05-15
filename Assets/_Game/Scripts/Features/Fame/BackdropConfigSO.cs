using UnityEngine;

namespace AKTR.Features.Fame
{
    [CreateAssetMenu(menuName = "AKTR/Fame/Backdrop Config")]
    public class BackdropConfigSO : ScriptableObject
    {
        [SerializeField] private Sprite[] _tierBackdrops = new Sprite[4];

        public Sprite GetBackdropForTier(int tier)
        {
            int index = Mathf.Clamp(tier - 1, 0, _tierBackdrops.Length - 1);
            return _tierBackdrops[index];
        }
    }
}
