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
        /// Stores the event channels for type-based events.
        /// </summary>
        private readonly Dictionary<Type, IEventChannel> typeEventChannel = new Dictionary<Type, IEventChannel>();

        /// <summary>
        /// Stores the event channels for asset-based events.
        /// </summary>
        private readonly Dictionary<AssetEventChannelKey, IEventChannel> assetEventChannels = new Dictionary<AssetEventChannelKey, IEventChannel>();

        /// <summary>
        /// Maintains the order of event channels for dispatching.
        /// </summary>
        private readonly List<IEventChannel> channelOrder = new List<IEventChannel>();
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION
        private DebugController debugController;
        private bool initialized;


        public void Initialize(DebugController debug)
        {
            if (initialized) return;

            debugController = debug;
            initialized = true;

            debugController.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "EventBus Initialized", "EventBus");
        }

        public void Shutdown()
        {
            if (!initialized) return;

            initialized = false;

            foreach (var channel in channelOrder) channel.Clear();

            typeEventChannel.Clear();
            assetEventChannels.Clear();
            channelOrder.Clear();

            debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "EventBus Shutdown", "EventBus");
            debugController = null;
        }
        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Subscribes to a type-based event.
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (!initialized) return;

            if (handler == null)
            {
                Debug.LogError("EventBus: Attempted to subscribe with a null handler.");
                return;
            }

            GetOrCreateChannel<T>().Subscribe(handler);
        }

        /// <summary>
        /// Subscribes to an asset-based event channel.
        /// </summary>
        public void Subscribe<T>(AssetEventChannel<T> channel, Action<T> handler) where T : struct
        {
            if (!initialized) return;

            if (channel == null)
            {
                Debug.LogError("EventBus: Attempted to subscribe to a null channel.");
                return;
            }
            if (handler == null)
            {
                Debug.LogError("EventBus: Attempted to subscribe with a null handler.");
                return;
            }

            GetOrCreateChannel<T>(channel).Subscribe(handler);
        }

        /// <summary>
        /// Unsubscribes from a type-based event.
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (!initialized) return;

            if (handler == null)
            {
                Debug.LogError("EventBus: Attempted to unsubscribe with a null handler.");
                return;
            }

            GetOrCreateChannel<T>().Unsubscribe(handler);
        }

        /// <summary>
        /// Unsubscribes from an asset-based event channel.
        /// </summary>
        public void Unsubscribe<T>(AssetEventChannel<T> channel, Action<T> handler) where T : struct
        {
            if (!initialized) return;

            if (channel == null)
            {
                Debug.LogError("EventBus: Attempted to unsubscribe from a null channel.");
                return;
            }
            if (handler == null)
            {
                Debug.LogError("EventBus: Attempted to unsubscribe with a null handler.");
                return;
            }

            GetOrCreateChannel<T>(channel).Unsubscribe(handler);
        }

        /// <summary>
        /// Publishes a type-based event immediately.
        /// </summary>
        public void Publish<T>(T eventData) where T : struct
        {
            if (!initialized) return;

            GetOrCreateChannel<T>().Publish(eventData);
        }

        /// <summary>
        /// Publishes an asset-based event channel immediately.
        /// </summary>
        public void Publish<T>(AssetEventChannel<T> channel, T eventData) where T : struct
        {
            if (!initialized) return;

            if (channel == null)
            {
                Debug.LogError("EventBus: Attempted to publish to a null channel.");
                return;
            }

            GetOrCreateChannel<T>(channel).Publish(eventData);
        }

        /// <summary>
        /// Publishes a type-based event to the queue.
        /// </summary>
        public void PublishQueued<T>(T eventData) where T : struct
        {
            if (!initialized) return;

            GetOrCreateChannel<T>().Enqueue(eventData);
        }

        /// <summary>
        /// Publishes an asset-based event channel to the queue.
        /// </summary>
        public void PublishQueued<T>(AssetEventChannel<T> channel, T eventData) where T : struct
        {
            if (!initialized) return;

            if (channel == null)
            {
                Debug.LogError("EventBus: Attempted to enqueue to a null channel.");
                return;
            }

            GetOrCreateChannel<T>(channel).Enqueue(eventData);
        }

        /// <summary>
        /// Dispatches all queued events for all channels.
        /// </summary>
        public void DispatchQueued()
        {
            if (!initialized) return;

            int count = channelOrder.Count;
            for (int i = 0; i < count; i++)
                channelOrder[i].DispatchQueued();
        }

        /// <summary>
        /// Raises an error for a specific channel.
        /// </summary>
        public void RaiseChannelError(string channel, string message)
        {
            if (!initialized) return;

            debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, $"Channel {channel} Error: {message}", "EventBus");
        }

        #endregion

        #region PRIVATE METHODS
        private TypeEventChannel<T> GetOrCreateChannel<T>() where T : struct
        {
            if (typeEventChannel.TryGetValue(typeof(T), out IEventChannel targetChannel)) return (TypeEventChannel<T>)targetChannel;

            var newChannel = new TypeEventChannel<T>(typeof(T).Name);

            typeEventChannel.Add(typeof(T), newChannel);
            channelOrder.Add(newChannel);
            return newChannel;
        }

        private TypeEventChannel<T> GetOrCreateChannel<T>(AssetEventChannel<T> assetChannel) where T : struct
        {
            var key = new AssetEventChannelKey(assetChannel, typeof(T));

            if (assetEventChannels.TryGetValue(key, out IEventChannel targetChannel)) return (TypeEventChannel<T>)targetChannel;

            TypeEventChannel<T> newChannel = new TypeEventChannel<T>(assetChannel.name);
            assetEventChannels.Add(key, newChannel);
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
        void Subscribe<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(T eventData) where T : struct;
        void PublishQueued<T>(T eventData) where T : struct;


        void Subscribe<T>(AssetEventChannel<T> channel, Action<T> handler) where T : struct;
        void Unsubscribe<T>(AssetEventChannel<T> channel, Action<T> handler) where T : struct;
        void Publish<T>(AssetEventChannel<T> channel, T eventData) where T : struct;
        void PublishQueued<T>(AssetEventChannel<T> channel, T eventData) where T : struct;

        void RaiseChannelError(string channel, string message);
        void DispatchQueued();
    }
    #endregion
}
