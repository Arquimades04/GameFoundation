using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Foundation.Systems.Game
{
    public class DebugCommandRegistry : IGameService
    {
        #region PUBLIC PARAMETERS
        /// <summary>
        /// Provides a read-only collection of all registered debug commands.
        /// </summary>
        public IReadOnlyCollection<IDebugCommand> Commands => commandDictionary.Values;
        #endregion

        #region PRIVATE PARAMETERS
        /// Internal dictionary mapping command names to their corresponding debug command implementations.
        private readonly Dictionary<string, IDebugCommand> commandDictionary = new Dictionary<string, IDebugCommand>();
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION
        private DebugController debugController;
        private bool initialized;
        public void Initialize(DebugController debug)
        {
            if (initialized) throw new InvalidOperationException("DebugCommandRegistry is already initialized.");

            debugController = debug;
            initialized = true;

            RegisterAllCommands();

            debugController.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "DebugCommandRegistry Initialized", "DebugCommandRegistry");
        }

        public void Shutdown()
        {
            if (!initialized) return;

            initialized = false;

            debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "DebugCommandRegistry Shutdown", "DebugCommandRegistry");
            debugController = null;
        }
        #endregion

        #region PUBLIC METHODS
        public void RegisterAllCommands()
        {
            bool success = true;

            IDebugCommand[] commandsToRegister =
            {
                new HelpCommand(),
            };


            foreach (IDebugCommand command in commandsToRegister)
            {
                string name = command?.CommandName ?? "<null>";
                if (!RegisterCommand(command))
                {
                    success = false;
                    debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, $"Failed to register command '{name}'", "DebugCommandRegistry");
                }
            }

            if (!success)
            {
                debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, $"Failed to register some commands: {string.Join(", ", commandsToRegister.Where(c => !commandDictionary.ContainsKey(c.CommandName)).Select(c => c.CommandName))}", "DebugCommandRegistry");
            }
        }

        public bool RegisterCommand(IDebugCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.CommandName))
            {
                debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "Attempted to register a null or unnamed command.", "DebugCommandRegistry");
                return false;
            }

            return commandDictionary.TryAdd(command.CommandName, command);
        }

        public bool UnregisterCommand(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "Attempted to unregister a command with an invalid name.", "DebugCommandRegistry");
                return false;
            }

            return commandDictionary.Remove(commandName);
        }

        public bool TryGetCommand(string commandName, out IDebugCommand command)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                command = null;
                return false;
            }

            return commandDictionary.TryGetValue(commandName, out command);
        }

        public DebugCommandResult ExecuteCommand(string commandLine, DebugCommandContext context)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return DebugCommandResult.FailureResult("No command was entered.");

            string[] tokens = commandLine.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0) return DebugCommandResult.FailureResult("No command was entered.");

            if (!TryGetCommand(tokens[0], out IDebugCommand command))
            {
                return DebugCommandResult.FailureResult($"Command '{tokens[0]}' not found.");
            }


            string[] arguments = new string[tokens.Length - 1];

            Array.Copy(
                tokens,
                1,
                arguments,
                0,
                arguments.Length);

            try
            {
                return command.Execute(arguments, context);
            }
            catch (Exception)
            {
                debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, $"Error executing command '{tokens[0]}'", "DebugCommandRegistry");
                return DebugCommandResult.FailureResult($"Error executing command '{tokens[0]}'");
            }
        }
        #endregion
    }
}
