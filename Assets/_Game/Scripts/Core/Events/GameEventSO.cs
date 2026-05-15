using System;
using UnityEngine;

namespace AKTR.Core.Events
{
    [CreateAssetMenu(menuName = "Scriptable Objects/GameEventSO")]
    public class GameEventSO : ScriptableObject
    {
        private event Action _onRaised;

        public void Raise()
        {
            _onRaised?.Invoke();
        }

        public void Register(Action listener)
        {
            _onRaised += listener;
        }

        public void Unregister(Action listener)
        {
            _onRaised -= listener;
        }
    }
}

