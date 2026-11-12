using System;
using UnityEngine;

namespace Foundation.Systems.Game
{
    /// <summary>
    /// Provides the runtime console interface for querying diagnostics
    /// and executing development commands.
    /// </summary>
    public class DebugConsole : MonoBehaviour, IGameService
    {
        #region PUBLIC PARAMETERS
        /// <summary>
        /// Indicates whether the debug console is currently open.
        /// </summary>
        /// <value>True if the console is open; otherwise, false.</value>
        public bool IsConsoleOpen { get; private set; }
        #endregion

        #region PRIVATE PARAMETERS
        // Enables the game to be paused when the console is opened. This is useful for debugging gameplay without interference from game events.
        private bool pauseGameOnOpen = true;

        //  View component responsible for rendering the console UI and handling user input.
        private IDebugConsoleView consoleView;

        // A token that represents the paused state of the game when the console is open. Used in tandem with the PauseController.
        private PauseController.PauseToken pauseToken;
        private PauseController pauseController;
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION
        private DebugController debugController;
        private DebugCommandRegistry commandRegistry;
        private bool initialized;

        public void Initialize(DebugController debug, DebugCommandRegistry registry, PauseController pause)
        {
            if (initialized) throw new InvalidOperationException("DebugConsole is already initialized.");

            AddCommandRegistry(registry);
            debugController = debug;
            pauseController = pause;
            initialized = true;

            debugController.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "DebugConsole Initialized", "DebugConsole");
        }
        public void Shutdown()
        {
            if (!initialized) return;

            CloseConsole();
            initialized = false;
            AddCommandRegistry(null);
            pauseController = null;

            debugController.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "DebugConsole Shutdown", "DebugConsole");
            debugController = null;
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Assigns the view component responsible for rendering the console UI and handling user input.
        /// </summary>
        public void SetConsoleView(IDebugConsoleView view)
        {
            this.consoleView = view;
        }

        /// <summary>
        /// Opens the debug console, pausing the game if configured to do so.
        /// </summary>
        public void OpenConsole()
        {
            if (this.IsConsoleOpen) return;

            this.IsConsoleOpen = true;

            // Request a Pause and signal the DebugConsole is the source.
            if (this.pauseGameOnOpen) this.pauseToken = pauseController.RequestPause("DebugConsole");

            this.consoleView?.ShowConsole();
        }

        /// <summary>
        /// Closes the debug console, resuming the game if it was paused when the console
        /// was opened.
        /// </summary>
        public void CloseConsole()
        {
            if (!this.IsConsoleOpen) return;

            this.IsConsoleOpen = false;

            // Request a Pause and signal the DebugConsole is the source.
            this.pauseToken?.Release();
            this.pauseToken = null;

            this.consoleView?.HideConsole();
        }
        /// <summary>
        /// Toggles Open/Close state
        /// </summary>
        public void ToggleConsole()
        {
            if (this.IsConsoleOpen) CloseConsole();
            else OpenConsole();
        }

        public void AddCommandRegistry(DebugCommandRegistry registry)
        {
            this.commandRegistry = registry;
        }
        #endregion
    }

    public interface IDebugConsoleView
    {
        void ShowConsole();
        void HideConsole();
    }
}
