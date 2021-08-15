﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// Deals with the functionality of loading, adding and removing "Custom Commands"
/// "Custom Commands" (short: CC) are a way of adding new commands without touching a single line of code
/// Moderators can add a new Custom Command using a Discord command
/// These "Custom Commands" will only display a specified text as a callback when someone calls them
/// </summary>
public class CustomCommandsService : BaseCommandModule
{
    private static readonly List<CustomCommand> Commands = new List<CustomCommand>();
    internal static DiscordClient DiscordClient { get; set; }

    [Command("newcc")]
    [Aliases("createcc", "addcc", "ccadd", "cccreate")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task CreateCommand(CommandContext ctx, params string[] names)
    {
        foreach (var name in names)
        {
            if (DiscordClient.GetCommandsNext().RegisteredCommands.ContainsKey(name)) // Check if there is a command with one of the names already
            {
                await ErrorCallback(ctx, name);
                return;
            }
            
            foreach (var cmd in Commands)
            {
                if (cmd.Names.Contains(name)) // Check if there is already a CC with one of the names
                {
                    await ErrorCallback(ctx, name);
                    return;
                }
            }
        }

        string content = await WaitForContent(ctx, names[0]);
        CustomCommand command = new CustomCommand(names, content);
        await WriteToFile(command);
    }

    [Command("delcc")]
    [Aliases("deletecc", "removecc")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task DeleteCommand(CommandContext ctx, string name)
    {
        if (File.Exists(name))
        {
            File.Delete(name);
            await ctx.RespondAsync($"CC {name} successfully deleted!");
        }
    }

    [Command("editcc")]
    [Aliases("ccedit")]
    [RequireRoles(RoleCheckMode.Any, "Mod", "Owner")] // Restrict access to users with the "Mod" or "Owner" role only
    public async Task EditCommand(CommandContext ctx, string name)
    {
        if (File.Exists(name))
        {
            string filePath = UtilityFunctions.ConstructPath(name, ".txt");
            string content = await WaitForContent(ctx, filePath);
            List<string> oldLines = new List<string>();
            using (StreamReader sr = File.OpenText(filePath))
            {
                string l;
                string line = string.Empty;
                while ((l = await sr.ReadLineAsync()) != null)
                {
                    line += l;
                }
                oldLines.Add(line);
            }

            if (oldLines.Count >= 2)
                oldLines[1] = content;
            else
            {
                await ctx.RespondAsync("An error has occured!");
                return;
            }
            
            await using (StreamWriter sw = File.AppendText(filePath))
            {
                foreach(string s in oldLines)
                    await sw.WriteLineAsync(s);
            }

            if (TryGetCommand(name, out CustomCommand command))
                command.EditCommand(name);
        }
        else
        {
            await ctx.RespondAsync("There is no Custom Command with this name! Please don't use an alias, use the original name!");
        }
    }

    internal static async Task LoadCustomCommands()
    {
        foreach (string fileName in Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory))
        {
            using (StreamReader sr = File.OpenText(fileName))
            {
                string names = await sr.ReadLineAsync();
                if (string.IsNullOrEmpty(names))
                    continue;

                string content = string.Empty;
                string c;
                while ((c = await sr.ReadLineAsync()) != null)
                {
                    content += c;
                }

                CustomCommand cmd = new CustomCommand(names.Split(','), content);
                Commands.Add(cmd);
            }
        }
    }

    internal static async Task CommandError(CommandsNextExtension extension, CommandErrorEventArgs args)
    {
        if (args.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException)
        {
            if (TryGetCommand(args.Context.Command.Name, out CustomCommand command))
            {
                await command.ExecuteCommand(args.Context);
            }
        }
    }

    private async Task WriteToFile(CustomCommand command)
    {
        if (!File.Exists(command.FilePath))
        {
            await using (StreamWriter sw = File.AppendText(command.FilePath))
            {
                await sw.WriteLineAsync(string.Join(',', command.Names));
                await sw.WriteLineAsync(command.Content);
            }
        }
    }

    private async Task ErrorCallback(CommandContext ctx, string name)
    {
        await ctx.RespondAsync($"There is already a command containing the alias {name}");
    }

    private async Task<string> WaitForContent(CommandContext ctx, string name)
    {
        await ctx.RespondAsync($"Please input the content of the CC {name} in one single message. Your next message will count as the content.");
        string content = string.Empty;
        await ctx.Message.GetNextMessageAsync(m =>
        {
            content = m.Content;
            return true;
        });
        
        return content;
    }

    private static bool TryGetCommand(string name, out CustomCommand command)
    {
        command = GetCommandByName(name);
        return command != null;
    }

    private static CustomCommand GetCommandByName(string name)
    {
        return Commands.FirstOrDefault(cc => cc.Names.Contains(name));
    }
}