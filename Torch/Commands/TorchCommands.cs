﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using Torch;
using Torch.Commands.Permissions;
using Torch.Managers;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class TorchCommands : CommandModule
    {
#if DEBUG
        [Command("fixit")]
        public void Fixit()
        {
            Environment.Exit(0);
        }

        [Command("dbgcmd")]
        public void Dbgcmd()
        {
            var commandManager = ((PluginManager)Context.Torch.Plugins).Commands;
            Console.WriteLine(commandManager.Commands.GetTreeString());
        }
#endif
        [Command("crash", "Causes the server to crash for testing purposes")]
        [Permission(MyPromoteLevel.Admin)]
        public void Crash()
        {
            throw new Exception("Crash triggered by Torch command");
        }

        [Command("help", "Displays help for a command")]
        public void Help()
        {
            var commandManager = ((TorchBase)Context.Torch).Commands;
            commandManager.Commands.GetNode(Context.Args, out CommandTree.CommandNode node);

            if (node != null)
            {
                var command = node.Command;
                var children = node.Subcommands.Select(x => x.Key);

                var sb = new StringBuilder();

                if (command != null)
                {
                    sb.AppendLine($"Syntax: {command.SyntaxHelp}");
                    sb.Append(command.HelpText);
                }

                if (node.Subcommands.Count() != 0)
                    sb.Append($"\nSubcommands: {string.Join(", ", children)}");

                Context.Respond(sb.ToString());
            }
            else
            {
                var topNodeNames = commandManager.Commands.Root.Select(x => x.Key);
                Context.Respond($"Top level commands: {string.Join(", ", topNodeNames)}");
            }
        }

        [Command("ver", "Shows the running Torch version.")]
        public void Version()
        {
            var ver = Context.Torch.TorchVersion;
            Context.Respond($"Torch version: {ver}");
        }

        [Command("plugins", "Lists the currently loaded plugins.")]
        public void Plugins()
        {
            var plugins = Context.Torch.Plugins.Select(p => p.Name);
            Context.Respond($"Loaded plugins: {string.Join(", ", plugins)}");
        }

        [Command("stop", "Stops the server.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Stop()
        {
            Context.Respond("Stopping server.");
            Context.Torch.Stop();
        }
    }
}
