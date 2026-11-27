using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation.Systems.Game
{
    public abstract class AssetEventChannel<T> : ScriptableObject where T : struct
    {
        #region PUBLIC METHODS
        public void Subscribe(Action<T> handler)
        {
            if (handler == null) return;

            if (TryResolveEventBus(out IEventBus eventBus)) eventBus.Subscribe(this, handler);
        }

        public void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;

            if (TryResolveEventBus(out IEventBus eventBus)) eventBus.Unsubscribe(this, handler);
        }

        public virtual void Raise(T eventData)
        {
            if (TryResolveEventBus(out IEventBus eventBus)) eventBus.Publish(this, eventData);
        }

        public virtual void RaiseQueued(T eventData)
        {
            if (TryResolveEventBus(out IEventBus eventBus)) eventBus.PublishQueued(this, eventData);
        }
        #endregion

        #region PRIVATE METHODS
        private bool TryResolveEventBus(out IEventBus eventBus)
        {
            if (GameEntryPoint.TryResolve<IEventBus>(out eventBus)) return true;

            Debug.LogError("Failed to resolve IEventBus.");
            return false;
        }
        #endregion
    }
}
