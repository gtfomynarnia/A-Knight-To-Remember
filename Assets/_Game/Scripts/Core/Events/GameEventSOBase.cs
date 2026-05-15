using System;
using UnityEngine;

namespace AKTR.Core.Events
{
    public class GameEventSOBase<T> : ScriptableObject
    {
        private event Action<T> _onRaised;

        public void Raise(T value)
        {
            _onRaised?.Invoke(value);
        }

        public void Register(Action<T> listener)
        {
            _onRaised += listener;
        }

        public void Unregister(Action<T> listener)
        {
            _onRaised -= listener;
        }
    }
}

