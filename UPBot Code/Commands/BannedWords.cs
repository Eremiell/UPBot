﻿using System.IO;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.EventArgs;
/// <summary>
/// Command used to handle the words considered as banned.
/// The actual check is done using an event from the DiscordClient and not a specific command, of course
/// author: CPU
/// </summary>
public class BannedWords : BaseCommandModule {

  private static List<BannedWord> bannedWords = null;
  readonly static Regex valid = new Regex(@"^[a-zA-Z0-9]+$");
  readonly static Regex letters = new Regex(@"[a-zA-Z0-9]");
  private const string directoryName = "Restrictions";

  public static void Init() {
    bannedWords = new List<BannedWord>();
    string path = Utils.ConstructPath(directoryName, "BannedWords", ".txt");
    if (!File.Exists(path)) return;
    string[] all = File.ReadAllLines(path);
    foreach (string line in all) {
      BannedWord word = new BannedWord(line);
      if (word.word == null) continue;
      bannedWords.Add(word);
    }
    bannedWords.Sort((a, b) => { return a.word.CompareTo(b.word); });
  }

  [Command("bannedwords")]
  [Description("To handle banned words. It can be done only by Mods and Helpers")]
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task BannedWordsCommand(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    await ctx.Channel.SendMessageAsync("Use the commands `list`, `add`, and `remove` to handle banned words.");
  }

  [Command("bannedwords")]
  [Description("To handle banned words. It can be done only by Mods and Helpers")]
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task BannedWordsCommand(CommandContext ctx, [Description("Command to use (list, add, remove)")] string command) {
    Utils.LogUserCommand(ctx);
    await HandleListOfBannedWords(ctx, command);
  }

  [Command("bannedwords")]
  [Description("To handle banned words. It can be done only by Mods and Helpers")]
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task BannedWordsCommand(CommandContext ctx, [Description("Command to use (list, add, remove)")] string command, [Description("The word to add or remove (not used when listing)")] string word) {
    Utils.LogUserCommand(ctx);
    Task<DiscordMessage> msg = HandleAddRemoveOfBannedWords(ctx, command, word).Result;
    Utils.DeleteDelayed(30, msg.Result).Wait();
    await Utils.DeleteDelayed(10, ctx.Message);
  }

  private async Task<Task<DiscordMessage>> HandleListOfBannedWords(CommandContext ctx, string command) {
    try {
      if (command.ToLowerInvariant() != "list") return ctx.Channel.SendMessageAsync("Use: list, add, or remove.");
      else if (bannedWords == null || bannedWords.Count == 0) return ctx.Channel.SendMessageAsync("There are no banned words I am aware of.");
      else {
        string message = "I have " + bannedWords.Count + " banned word" + (bannedWords.Count == 1 ? "" : "s") + ":\n";
        for (int i = 0; i < bannedWords.Count; i++) {
          message += bannedWords[i].word + " (" + GetUserName(bannedWords[i].creator, ctx) + " " + bannedWords[i].date.ToString("yyyy/MM/dd") + ")";
          if (i < bannedWords.Count - 1) message += ",\n";
        }
        await Task.Delay(10);
        return ctx.Channel.SendMessageAsync(message);
      }
    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("BannedWords.List", ex));
    }
  }

  private async Task<Task<DiscordMessage>> HandleAddRemoveOfBannedWords(CommandContext ctx, string command, string word) {
    try {
      await Task.Delay(10);
      if (command.ToLowerInvariant() == "add") {
        word = word.Trim(' ', '\r', '\n').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(word) || !valid.IsMatch(word)) return ctx.Channel.SendMessageAsync("Not a valid word");
        if (bannedWords == null) bannedWords = new List<BannedWord>();
        // Do we have it?
        foreach (BannedWord bw in bannedWords) {
          if (bw.word.Equals(word)) {
            await ctx.Message.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.KO));
            return ctx.Channel.SendMessageAsync("The word \"" + word + "\" is already in the list.");
          }
        }
        BannedWord w = new BannedWord(word, ctx.Message.Author.Id);
        bannedWords.Add(w);
        bannedWords.Sort((a, b) => { return a.word.CompareTo(b.word); });
        SaveWord(w);

        await ctx.Message.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.OK));
        return ctx.Channel.SendMessageAsync("The word \"" + word + "\" has been added.");
      }
      else if (command.ToLowerInvariant() == "remove") {
        word = word.Trim(' ', '\r', '\n').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(word) || !Regex.IsMatch(word, @"^[a-zA-Z0-9]+$")) return ctx.Channel.SendMessageAsync("Not a valid word");
        if (bannedWords == null) bannedWords = new List<BannedWord>();
        // Do we have it?
        BannedWord found = null;
        foreach (BannedWord bw in bannedWords) {
          if (bw.word.Equals(word)) {
            found = bw;
            break;
          }
        }
        if (found == null) {
          await ctx.Message.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.KO));
          return ctx.Channel.SendMessageAsync("The word \"" + word + "\" is not in the list.");
        }
        bannedWords.Remove(found);
        SaveList();
        await ctx.Message.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.OK));
        return ctx.Channel.SendMessageAsync("The word \"" + word + "\" has been removed.");
      }
      else return ctx.Channel.SendMessageAsync("Use: add or remove and then the word.");
    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("BannedWords.Handle", ex));
    }
  }

  void SaveWord(BannedWord w) {
    string path = Utils.ConstructPath(directoryName, "BannedWords", ".txt");
    if (!File.Exists(path)) File.CreateText(path);
    try {
      using (StreamWriter sw = File.AppendText(path)) {
        sw.Write(w.ToString());
        sw.FlushAsync();
      }
    } catch (Exception e) {
      Utils.Log(e.Message);
    }
  }

  void SaveList() {
    string path = Utils.ConstructPath(directoryName, "BannedWords", ".txt");
    if (File.Exists(path)) {
      try {
        File.Delete(path);
      } catch (Exception e) {
        Utils.Log(e.Message);
        return;
      }
    }
    try {
      using (StreamWriter sw = File.CreateText(path)) {
        foreach (BannedWord w in bannedWords) {
          sw.Write(w.ToString());
          sw.FlushAsync();
        }
      }
    } catch (Exception e) {
      Utils.Log(e.Message);
    }
  }

  class BannedWord {
    public string word;
    public ulong creator = 0;
    public DateTime date = DateTime.MinValue;

    public BannedWord(string w, ulong id) {
      word = w;
      creator = id;
      date = DateTime.Now;
    }

    public BannedWord(string line) {
      string[] parts = line.Trim(' ', '\r', '\n').Split('\t');
      if (parts.Length != 3) return;
      word = parts[0].Trim(' ', '\r', '\n', '\t').ToLowerInvariant();
      ulong.TryParse(parts[1].Trim(' ', '\r', '\n', '\t'), out creator);
      DateTime.TryParse(parts[2].Trim(' ', '\r', '\n', '\t'), out date);
    }

    public override string ToString() {
      return word + "\t" + creator + "\t" + date + "\n";
    }
  }

  Dictionary<ulong, string> knownUsers;

  string GetUserName(ulong userId, CommandContext ctx) {
    if (knownUsers == null) knownUsers = new Dictionary<ulong, string>();
    if (knownUsers.ContainsKey(userId)) return knownUsers[userId];
    var task = GetUserNameFromDiscord(userId, ctx);
    if (!task.Wait(TimeSpan.FromSeconds(5))) {
      return userId.ToString();
    }
    DiscordUser result = task.Result;
    knownUsers[userId] = result.Username;
    return result.Username;
  }

  async Task<DiscordUser> GetUserNameFromDiscord(ulong userId, CommandContext ctx) {
    return await ctx.Client.GetUserAsync(userId);
  }

  internal static async Task CheckMessage(DiscordClient client, MessageCreateEventArgs args) {
    try {
      // Who is the author? If the bot or a mod then ignore
      if (args.Author.Equals(client.CurrentUser)) return;
      DiscordUser user = args.Author;
      DiscordGuild guild = await client.GetGuildAsync((ulong)args.Message.Channel.GuildId);
      DiscordMember member;
      try {
        member = await guild.GetMemberAsync(user.Id);
      } catch (Exception) {
        return;
      }
      foreach (DiscordRole role in member.Roles) {
        if (role.Id == 831050318171078718ul /* Helper */ || role.Id == 830901743624650783ul /* Mod */ || role.Id == 830901562960117780ul /* Owner */) return;
      }

      string msg = args.Message.Content.ToLowerInvariant();
      foreach (BannedWord w in bannedWords) {
        int pos = msg.IndexOf(w.word);
        if (pos == -1) continue;
        if (pos > 0 && letters.IsMatch(msg[pos - 1].ToString())) continue;
        if (pos + w.word.Length < msg.Length && letters.IsMatch(msg[pos + w.word.Length].ToString())) continue;

        Utils.Log("Removed word \"" + w.word + "\" from " + user.Username + " in: " + msg);
        DiscordMessage warning = await args.Message.Channel.SendMessageAsync("Moderate your language, " + user.Mention + ".");
        await args.Message.DeleteAsync("Bad words: " + w.word);
        Utils.DeleteDelayed(10000, warning).Wait();
        return;
      }
    } catch (Exception ex) {
      await args.Message.RespondAsync(Utils.GenerateErrorAnswer("BannedWords.CheckMessage", ex));
    }
  }


  /*
   list -> show them as DM
  remove name -> removes one
  add name -> adds one
   
  file format <word>\t<id of who added>\t<timestamp>\n
   */
}
