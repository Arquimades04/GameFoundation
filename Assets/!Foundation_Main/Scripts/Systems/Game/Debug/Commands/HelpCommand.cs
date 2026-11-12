using UnityEngine;

namespace Foundation.Systems.Game
{
    /// <summary>
    /// Returns a list of all available debug commands and their descriptions when executed.
    /// </summary>
    public class HelpCommand : IDebugCommand
    {
        public string CommandName => "help";

        public string Description => "Displays a list of available debug commands.";

        public string Usage => "help";

        public DebugCommandResult Execute(string[] args, DebugCommandContext context)
        {
            var commandRegistry = context.CommandRegistry;
            var commands = commandRegistry.Commands;

            if (commands.Count == 0)
            {
                return DebugCommandResult.FailureResult("No debug commands are currently registered.");
            }

            string helpMessage = "Available Debug Commands:\n";
            foreach (var command in commands)
            {
                helpMessage += $"- {command.CommandName}: {command.Description}\n";
            }

            return DebugCommandResult.SuccessResult(helpMessage);
        }
    }
}
