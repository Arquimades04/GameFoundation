using UnityEngine;

namespace Foundation.Systems.Game
{
    internal interface IEventChannel
    {
        void Clear();
        void DispatchQueued();
    }

    internal interface IEventChannel<T> : IEventChannel where T : struct
    {
        void Subscribe(System.Action<T> handler);
        void Unsubscribe(System.Action<T> handler);
        void Publish(T eventData);
        void Enqueue(T eventData);
    }
}
