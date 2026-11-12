using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation.Systems.Game
{
    /// <summary>
    /// Controls the global gameplay pause state and manages pause requests from
    /// independent systems.
    /// </summary>
    public class PauseController : MonoBehaviour, IGameService
    {
        #region PUBLIC PROPERTIES
        /// <summary>
        /// Gets whether the game is currently paused.
        /// </summary>
        public bool IsPaused => pauseRequests.Count > 0;

        ///<summary>
        /// Gets the number of active pause tokens.
        /// </summary>
        public int PauseTokenCount => pauseRequests.Count;
        #endregion

        #region PRIVATE PROPERTIES
        // Maintains the active pause requests.
        // Each request is represented by a unique PauseToken instance
        // and symbolizes a single system's desire to pause the game.
        private readonly HashSet<PauseToken> pauseRequests = new HashSet<PauseToken>();
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION
        private DebugController debugController;
        private bool initialized;
        public void Initialize(DebugController debug)
        {
            if (initialized) throw new InvalidOperationException("PauseController is already initialized.");

            debugController = debug;
            initialized = true;

            debugController.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "PauseController Initialized", "PauseController");
        }

        public void Shutdown()
        {
            if (!initialized) return;

            initialized = false;

            debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "PauseController Shutdown", "PauseController");
            debugController = null;
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Creates a pause request owned by the caller.
        /// Gameplay remains paused until the returned token is released.
        /// </summary>
        public PauseToken RequestPause(string source = "Unknown")
        {
            var token = new PauseToken(this) { Source = source };
            pauseRequests.Add(token);
            return token;
        }
        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Releases a pause request owned by a specific token.
        /// </summary>
        internal void ReleasePauseToken(PauseToken token)
        {
            pauseRequests.Remove(token);
        }
        #endregion

        #region NESTED TYPES
        /// <summary>
        /// Represents ownership of a single pause request.
        /// </summary>
        public sealed class PauseToken
        {
            public string Source { get; set; } = "Unknown";
            private readonly PauseController owner;
            private bool released;

            internal PauseToken(PauseController owner)
            {
                this.owner = owner;
            }

            /// <summary>
            /// Gets whether this token has been released and is no longer valid.
            /// </summary>
            public bool IsReleased => released;

            /// <summary>
            /// Releases this token.
            /// Subsequent calls to <see cref="Release"/> will have no effect.
            /// </summary>
            public void Release()
            {
                if (released) return;

                released = true;
                owner?.ReleasePauseToken(this);
            }
        }
        #endregion
    }
}

