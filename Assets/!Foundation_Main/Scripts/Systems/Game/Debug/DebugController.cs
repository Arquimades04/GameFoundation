using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Foundation.Systems.Game
{
    /// <summary>
    /// Provides centralized runtime diagnostics for logging, debugging,
    /// and development tooling.
    /// </summary>
    public class DebugController : MonoBehaviour, IGameService
    {
        #region PUBLIC PROPERTIES
        /// <summary>
        /// Gets whether the debug controller is currently active.
        /// </summary>
        public bool IsEnabled => debugEnabled;

        /// <summary>
        /// Gets the number of active debug messages currently stored.
        /// </summary>
        public int MessageCount => debugMessages.Count;

        /// <summary>
        /// Gets the number of active debug messages with the specified severity.
        /// </summary>
        /// <param name="severity">
        /// The severity level to filter messages by.
        /// </param>
        /// <returns>
        /// The count of messages with the specified severity.
        /// </returns>
        public int MessageSeverityCount(DebugSeverity severity) => debugMessages.Count(x => x.Severity == severity);

        /// <summary>
        /// Safely exposes the Message List
        /// </summary>
        public IReadOnlyList<DebugMessage> Messages => debugMessages;
        #endregion

        #region PRIVATE PROPERTIES
        //Controls if diagnostics are enabled or disabled.
        [SerializeField]
        private bool debugEnabled = true;

        // Maintains the active debug messages.
        private readonly List<DebugMessage> debugMessages = new List<DebugMessage>();

        //Limits the number of messages stored in the debug controller.
        private const int MaxMessageCount = 1000;
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION
        private bool initialized;
        public void Initialize()
        {
            if (initialized) throw new InvalidOperationException("DebugController is already initialized.");

            initialized = true;

            SetEnabled(true);

            Log(DebugCategory.General, DebugPersistence.RuntimeOnly, "DebugController Initialized", "DebugController");
        }

        public void Shutdown()
        {
            if (!initialized) return;

            initialized = false;

            SetEnabled(false);

            Log(DebugCategory.General, DebugPersistence.RuntimeOnly, "DebugController Shutdown", "DebugController");
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Enables or disables the debug controller.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            debugEnabled = enabled;
        }

        /// <summary>
        /// Logs an informational message to the debug controller.
        /// </summary>
        public void Log(DebugCategory category, DebugPersistence persistence, string message, string source = null)
        {
            AddMessage(
                   DebugSeverity.Info,
                   category,
                   persistence,
                   message,
                   source);
        }

        /// <summary>
        /// Logs a warning message to the debug controller.
        /// </summary>
        public void LogWarning(DebugCategory category, DebugPersistence persistence, string message, string source = null)
        {
            AddMessage(
                   DebugSeverity.Warning,
                   category,
                   persistence,
                   message,
                   source);
        }

        /// <summary>
        /// Logs an error message to the debug controller.
        /// </summary>
        public void LogError(DebugCategory category, DebugPersistence persistence, string message, string source = null)
        {
            AddMessage(
                   DebugSeverity.Error,
                   category,
                   persistence,
                   message,
                   source);

        }

        /// <summary>
        /// Logs a critical message to the debug controller.
        /// </summary>
        public void LogCritical(DebugCategory category, string message, string source = null)
        {
            AddMessage(
                   DebugSeverity.Critical,
                   category,
                   DebugPersistence.Persistent,
                   message,
                   source);
        }

        /// <summary>
        /// Completely clears the debug log
        /// </summary>
        public void ClearLog()
        {
            //TODO LOG ALL NON RUNTIME ONLY MESSAGES
            //debugMessages.Where(x => !x.Persistence.Equals(DebugPersistence.RuntimeOnly));
            debugMessages.Clear();
        }


        public List<DebugMessage> GetMessages(DebugSeverity minimumSeverity = DebugSeverity.Info, DebugCategory targetCategory = DebugCategory.All)
        {
            List<DebugMessage> result = new List<DebugMessage>();

            foreach (DebugMessage dMessage in debugMessages)
            {
                if (dMessage.Severity < minimumSeverity) continue;
                if (targetCategory != DebugCategory.All && (dMessage.Category & targetCategory) == 0) continue;

                result.Add(dMessage);
            }
            return result;
        }

        public List<DebugMessage> SearchMessages(string searchTerm, bool searchSources, DebugSeverity minimumSeverity = DebugSeverity.Info, DebugCategory targetCategory = DebugCategory.All)
        {
            List<DebugMessage> filtered = GetMessages(minimumSeverity, targetCategory);
            List<DebugMessage> result = new List<DebugMessage>();

            if (String.IsNullOrWhiteSpace(searchTerm)) return result;

            foreach (DebugMessage dMessage in filtered)
            {
                bool messageMatch = dMessage.Message.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase);

                bool sourceMatch = searchSources && dMessage.Source.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase);

                if (!messageMatch && !sourceMatch)
                    continue;

                result.Add(dMessage);
            }
            return result;
        }

        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Adds a diagnostic message to the runtime message collection.
        /// </summary>
        private void AddMessage(DebugSeverity severity, DebugCategory category, DebugPersistence persistence, string message, string source)
        {
            if (!debugEnabled) return;

            DebugMessage log = new DebugMessage(
                severity,
                category,
                persistence,
                message,
                source ?? "null"
                );

            debugMessages.Add(log);
            MessageCountCheck();
            Debug.Log(log.Timestamp.TimeOfDay + " - " + "[" + log.Severity + "] " + "[" + log.Source + "] " + log.Message + " ;");
            //TODO: CRITICAL MESSAGES SHOULD BE WRITTEN TO LOG FILE IMMEDIATELY
        }

        /// <summary>
        /// Ensures the number of stored messages does not exceed the maximum limit.
        /// Removes the oldest messages if the limit is exceeded.
        /// </summary>
        private void MessageCountCheck()
        {
            if (debugMessages.Count > MaxMessageCount)
            {
                if (debugMessages[0].Persistence.Equals(DebugPersistence.Persistent)) //TODO:WRITE TO LOG ON DELETE;
                    debugMessages.RemoveAt(0); // Remove the oldest message
            }
        }
        #endregion

        #region NESTED TYPES
        /// <summary>
        /// Describes the severity level of a debug message.
        /// </summary>
        public enum DebugSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// Defines the persistence level of a debug message.
        /// RuntimeOnly: Message exists only during the current session.
        /// Persistent: Message is saved onto Log file on request, in removal if necessary or application exit.
        /// Critical: Message is saved onto Log file immediately.
        /// </summary>
        //TODO: IMPLEMENT LOG FILE CREATION WITH LOAD AND SAVE SYSTEMS
        public enum DebugPersistence
        {
            RuntimeOnly,
            Persistent
        }

        /// <summary>
        /// Categorizes debug messages for filtering and organization.
        /// </summary>
        [System.Flags]
        public enum DebugCategory
        {
            None = 0,
            General = 1 << 0,
            Game = 1 << 1,
            Behaviour = 1 << 2,
            Pause = 1 << 3,
            Loading = 1 << 4,
            Save = 1 << 5,
            Input = 1 << 6,
            Performance = 1 << 7,
            System = 1 << 8,
            All = ~0
        }

        /// <summary>
        /// Represents a single debug message with associated metadata.
        /// Limited to runtime diagnostics and development tooling.
        /// </summary>
        public readonly struct DebugMessage
        {
            // Contains the content of the debug message.
            public string Message { get; }
            // Identifies the source or origin of the debug message.
            public string Source { get; }
            // Indicates the severity level of the debug message.
            public DebugSeverity Severity { get; }
            // Categorizes the debug message for filtering and organization.
            public DebugCategory Category { get; }
            // Defines the persistence level of the debug message.
            public DebugPersistence Persistence { get; }
            // Records the timestamp when the debug message was created.
            public System.DateTime Timestamp { get; }


            public DebugMessage(DebugSeverity severity, DebugCategory category, DebugPersistence persistence, string message, string source)
            {

                Severity = severity;
                Category = category;
                Persistence = persistence;
                Message = message;
                Timestamp = System.DateTime.Now;
                Source = source;
            }
        }
        #endregion
    }
}
