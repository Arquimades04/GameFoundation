using System;
using System.Collections.Generic;

namespace Foundation.Systems.Game
{
    internal sealed class TypeEventChannel<T> : IEventChannel<T> where T : struct
    {
        #region PRIVATE PROPERTIES
        private readonly struct Mutation
        {
            public readonly Action<T> Handler;
            public readonly bool Add;

            public Mutation(Action<T> handler, bool add)
            {
                Handler = handler;
                Add = add;
            }
        }

        private readonly List<Action<T>> handlers = new List<Action<T>>();
        private readonly List<Mutation> pending = new List<Mutation>();
        private readonly Queue<T> queueEvents = new Queue<T>();

        private IEventBus eventBus;
        private int dispatchDepth;

        private string channelName;
        #endregion

        #region PUBLIC METHODS
        public TypeEventChannel(string channelName)
        {
            this.channelName = channelName;
            GameEntryPoint.TryResolve<IEventBus>(out var resolvedEventBus);
            eventBus = resolvedEventBus;
        }

        public void Clear()
        {
            handlers.Clear();
            pending.Clear();
            queueEvents.Clear();
            dispatchDepth = 0;
        }

        public void Subscribe(Action<T> handler)
        {
            if (handler == null) return;

            if (dispatchDepth > 0)
            {
                pending.Add(new Mutation(handler, true));
            }
            else if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        public void Unsubscribe(Action<T> handler)
        {
            if (handler == null)
                return;

            if (dispatchDepth > 0)
                pending.Add(new Mutation(handler, false));
            else
                handlers.Remove(handler);
        }

        public void DispatchQueued()
        {
            while (queueEvents.Count > 0)
                Publish(queueEvents.Dequeue());
        }

        public void Enqueue(T eventData)
        {
            queueEvents.Enqueue(eventData);
        }

        public void Publish(T eventData)
        {
            dispatchDepth++;
            try
            {
                foreach (var handler in handlers)
                {
                    Action<T> action = handler;

                    try
                    {
                        action.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        if (eventBus != null) eventBus.RaiseChannelError(channelName, "Error occurred while handling event: " + ex.Message);
                    }
                }
            }
            finally
            {
                dispatchDepth--;
                if (dispatchDepth == 0) ApplyPendingMutations();
            }
        }
        
        #endregion

        #region PRIVATE METHODS
        private void ApplyPendingMutations()
        {
            for (int i = 0; i < pending.Count; i++)
            {
                Mutation mutation = pending[i];
                if (mutation.Add)
                {
                    if (!handlers.Contains(mutation.Handler))
                        handlers.Add(mutation.Handler);
                }
                else
                {
                    handlers.Remove(mutation.Handler);
                }
            }

            pending.Clear();
        }
        #endregion


    }
}
