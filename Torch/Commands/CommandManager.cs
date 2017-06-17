﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Plugins;
using Torch.Managers;
using VRage.Game.ModAPI;
using VRage.Network;

namespace Torch.Commands
{
    public class CommandManager
    {
        public char Prefix { get; set; }

        public CommandTree Commands { get; set; } = new CommandTree();
        private Logger _log = LogManager.GetLogger(nameof(CommandManager));
        private readonly ITorchBase _torch;

        public CommandManager(ITorchBase torch, char prefix = '/')
        {
            _torch = torch;
            Prefix = prefix;
            ChatManager.Instance.MessageRecieved += HandleCommand;
            RegisterCommandModule(typeof(TorchCommands));
        }

        public bool HasPermission(ulong steamId, Command command)
        {
            var userLevel = MySession.Static.GetUserPromoteLevel(steamId);
            return userLevel >= command.MinimumPromoteLevel;
        }

        public bool IsCommand(string command)
        {
            return command.Length > 1 && command[0] == Prefix;
        }

        public void RegisterCommandModule(Type moduleType, ITorchPlugin plugin = null)
        {
            if (!moduleType.IsSubclassOf(typeof(CommandModule)))
                return;

            foreach (var method in moduleType.GetMethods())
            {
                var commandAttrib = method.GetCustomAttribute<CommandAttribute>();
                if (commandAttrib == null)
                    continue;

                var command = new Command(plugin, method);
                var cmdPath = string.Join(".", command.Path);
                _log.Info($"Registering command '{cmdPath}'");

                if (!Commands.AddCommand(command))
                    _log.Error($"Command path {cmdPath} is already registered.");
            }
        }

        public void RegisterPluginCommands(ITorchPlugin plugin)
        {
            var assembly = plugin.GetType().Assembly;
            foreach (var type in assembly.ExportedTypes)
            {
                RegisterCommandModule(type, plugin);
            }
        }

        public string HandleCommandFromServer(string message)
        {
            var cmdText = new string(message.Skip(1).ToArray());
            var command = Commands.GetCommand(cmdText, out string argText);
            var cmdPath = string.Join(".", command.Path);

            var splitArgs = Regex.Matches(argText, "(\"[^\"]+\"|\\S+)").Cast<Match>().Select(x => x.ToString().Replace("\"", "")).ToList();
            _log.Trace($"Invoking {cmdPath} for server.");
            var context = new CommandContext(_torch, command.Plugin, null, argText, splitArgs);
            if (command.TryInvoke(context))
                _log.Info($"Server ran command '{message}'");
            else
                context.Respond($"Invalid Syntax: {command.SyntaxHelp}");

            return context.Response;
        }

        public void HandleCommand(ChatMsg msg, ref bool sendToOthers)
        {
            HandleCommand(msg.Text, msg.Author, ref sendToOthers);
        }

        public void HandleCommand(string message, ulong steamId, ref bool sendToOthers, bool serverConsole = false)
        {

            if (message.Length < 1 || message[0] != Prefix)
                return;

            sendToOthers = false;

            var player = _torch.Multiplayer.GetPlayerBySteamId(steamId);
            if (player == null)
            {
                _log.Error($"Command {message} invoked by nonexistant player");
                return;
            }

            var cmdText = new string(message.Skip(1).ToArray());
            var command = Commands.GetCommand(cmdText, out string argText);

            if (command != null)
            {
                var cmdPath = string.Join(".", command.Path);

                if (!HasPermission(steamId, command))
                {
                    _log.Info($"{player.DisplayName} tried to use command {cmdPath} without permission");
                    _torch.Multiplayer.SendMessage($"You need to be a {command.MinimumPromoteLevel} or higher to use that command.", playerId: player.IdentityId);
                    return;
                }

                var splitArgs = Regex.Matches(argText, "(\"[^\"]+\"|\\S+)").Cast<Match>().Select(x => x.ToString().Replace("\"", "")).ToList();
                _log.Trace($"Invoking {cmdPath} for player {player.DisplayName}");
                var context = new CommandContext(_torch, command.Plugin, player, argText, splitArgs);
                _torch.Invoke(() =>
                {
                    if (command.TryInvoke(context))
                        _log.Info($"Player {player.DisplayName} ran command '{message}'");
                    else
                        context.Respond($"Invalid Syntax: {command.SyntaxHelp}");
                });
            }
        }
    }
}
