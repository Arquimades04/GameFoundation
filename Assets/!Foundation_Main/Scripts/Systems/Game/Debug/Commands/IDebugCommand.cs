using UnityEngine;

namespace Foundation.Systems.Game
{   /// <summary>
    /// Defines the interface for a generic executable debug command.
    /// </summary>
    public interface IDebugCommand
    {
        /// <summary>
        /// Name of the command, used for invocation in the debug console.
        /// </summary>
        string CommandName { get; }
        /// <summary>
        /// Definition of the command.
        /// </summary>
        /// <value></value>
        string Description { get; }
        /// <summary>
        /// Usage information for the command.
        /// </summary>
        string Usage { get; }

        /// <summary>
        /// Executes the command with the provided arguments and context.
        /// </summary>
        DebugCommandResult Execute(string[] args, DebugCommandContext context);
    }

    /// <summary>
    /// Presents the result of a defined debug command.
    /// </summary>
    public readonly struct DebugCommandResult
    {
        /// <summary>
        /// Indicates whether the command executed successfully.
        /// </summary>
        public bool Success { get; }
        /// <summary>
        /// Message associated with the command result.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the DebugCommandResult
        /// </summary>
        public DebugCommandResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        /// <summary>
        /// Creates a successful command result with an optional message.
        /// </summary>
        public static DebugCommandResult SuccessResult(string message = "")
        {
            return new DebugCommandResult(true, message);
        }

        /// <summary>
        /// Creates a failed command result with an optional message.
        /// </summary> 
        public static DebugCommandResult FailureResult(string message = "")
        {
            return new DebugCommandResult(false, message);
        }
    }

    /// <summary>
    /// Provides the context in which a debug command is executed, including references to relevant systems.
    /// </summary>
    public sealed class DebugCommandContext
    {
        /// <summary>
        /// Gets the instance of the DebugConsole associated with this context.
        /// </summary>
        public DebugConsole Console { get; }
        /// <summary>
        /// Gets the instance of the GameController associated with this context.
        /// </summary>
        public GameController GameController { get; }
        /// <summary>
        /// Gets the instance of the PauseController associated with this context.
        /// </summary>
        public PauseController PauseController { get; }
        /// <summary>
        /// Gets the instance of the DebugCommandRegistry associated with this context.
        /// </summary>
        public DebugCommandRegistry CommandRegistry { get; }

        /// <summary>
        /// Initializes a new instance of the DebugCommandContext with the specified console, game controller,
        /// and pause controller.
        /// </summary>
        public DebugCommandContext(DebugConsole console, GameController gameController, PauseController pauseController, DebugCommandRegistry commandRegistry)
        {
            Console = console;
            GameController = gameController;
            PauseController = pauseController;
            CommandRegistry = commandRegistry;
        }
    }
}
