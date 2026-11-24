using UnityEngine;
using System.Collections.Generic;
using System;

namespace Foundation.Systems.Game
{

    public class EventBus : IEventBus
    {
        #region PUBLIC PROPERTIES
        #endregion

        #region PRIVATE PROPERTIES
        /// <summary>
        /// Represents a channel for a specific type of event, allowing for subscription and publication of events.
        /// </summary>
        private interface IEventChannel
        {
            void Clear();
            void DispatchQueued();
        }

        private sealed class EventChannel<T> : IEventChannel where T : struct
        {
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
            private readonly Queue<T> queuedEvents = new Queue<T>();
            private DebugController debugController;
            private int depth;

            public EventChannel(DebugController debug)
            {
                debugController = debug;
            }

            public void Clear()
            {
                handlers.Clear();
                pending.Clear();
                depth = 0;
            }

            public void Subscribe(Action<T> handler)
            {
                if (handler == null) return;
                if (depth > 0) pending.Add(new Mutation(handler, true));
                else if (!handlers.Contains(handler)) handlers.Add(handler);
            }

            public void Unsubscribe(Action<T> handler)
            {
                if (handler == null) return;
                if (depth > 0) pending.Add(new Mutation(handler, false));
                else handlers.Remove(handler);
            }

            public void Publish(T evt)
            {
                depth++;
                try
                {
                    for (int i = 0; i < handlers.Count; i++)
                    {
                        Action<T> handler = handlers[i];

                        try
                        {
                            handler(evt);
                        }
                        catch (Exception ex)
                        {
                            debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, $"Subscriber '{handler.Method.Name}' failed for {typeof(T).Name}: {ex}", "EventBus");
                        }
                    }
                }
                finally
                {
                    depth--;
                    if (depth == 0 && pending.Count > 0) ApplyPending();
                }
            }

            public void Enqueue(T EventData) => queuedEvents.Enqueue(EventData);

            public void DispatchQueued()
            {
                while (queuedEvents.Count > 0) Publish(queuedEvents.Dequeue());
            }

            private void ApplyPending()
            {
                for (int i = 0; i < pending.Count; i++)
                {
                    Mutation m = pending[i];
                    if (m.Add)
                    {
                        if (!handlers.Contains(m.Handler)) handlers.Add(m.Handler);
                    }
                    else
                    {
                        handlers.Remove(m.Handler);
                    }
                }

                pending.Clear();
            }
        }



        /// <summary>
        /// Stores the event channels for different event types.
        /// </summary>
        private readonly Dictionary<Type, IEventChannel> eventChannels = new Dictionary<Type, IEventChannel>();
        private readonly List<IEventChannel> channelOrder = new List<IEventChannel>();
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION
        private DebugController debugController;
        private bool initialized;
        public void Initialize(DebugController debug)
        {
            if (initialized) throw new InvalidOperationException("EventBus is already initialized.");

            debugController = debug;
            initialized = true;

            debugController.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "EventBus Initialized", "EventBus");
        }

        public void Shutdown()
        {
            if (!initialized) return;

            initialized = false;

            foreach (var channel in eventChannels.Values) channel.Clear();
            eventChannels.Clear();

            debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "EventBus Shutdown", "EventBus");
            debugController = null;
        }
        #endregion

        #region PUBLIC METHODS
        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (!initialized) return;

            if (handler == null) Debug.LogError("EventBus: Attempted to subscribe with a null handler.");

            GetOrCreateChannel<T>().Subscribe(handler);

        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (!initialized) return;

            if (handler == null) Debug.LogError("EventBus: Attempted to unsubscribe with a null handler.");

            GetOrCreateChannel<T>().Unsubscribe(handler);
        }

        public void Publish<T>(T eventData) where T : struct
        {
            if (!initialized) return;

            GetOrCreateChannel<T>().Publish(eventData);
        }

        public void PublishQueued<T>(T eventData) where T : struct
        {
            if (!initialized) return;

            GetOrCreateChannel<T>().Enqueue(eventData);
        }

        public void DispatchQueued()
        {
            if (!initialized) return;

            int count = channelOrder.Count;
            for (int i = 0; i < count; i++)
                channelOrder[i].DispatchQueued();
        }

        #endregion

        #region PRIVATE METHODS
        private EventChannel<T> GetOrCreateChannel<T>() where T : struct
        {
            IEventChannel targetChannel;

            if (eventChannels.TryGetValue(typeof(T), out targetChannel)) return (EventChannel<T>)targetChannel;

            EventChannel<T> newChannel = new EventChannel<T>(debugController);
            eventChannels.Add(typeof(T), newChannel);
            channelOrder.Add(newChannel);
            return newChannel;
        }
        #endregion
    }

    #region NESTED TYPES
    /// <summary>
    /// Provides a centralized event management system for subscribing, unsubscribing, and publishing events within the game.
    /// </summary>
    public interface IEventBus : IGameService
    {
        void Subscribe<T>(System.Action<T> handler) where T : struct;
        void Unsubscribe<T>(System.Action<T> handler) where T : struct;
        void Publish<T>(T eventData) where T : struct;
        void PublishQueued<T>(T eventData) where T : struct;
        void DispatchQueued();
    }
    #endregion
}
