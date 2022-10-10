using Dapper;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Ja3farBot.Services;
using Ja3farBot.Util;
using MySql.Data.MySqlClient;
using static Ja3farBot.Util.MySqlDatatypes;

namespace Ja3farBot.Modules.Commands
{
    public class Moderation : InteractionModuleBase<SocketInteractionContext>
    {
        public DiscordSocketClient Client { get; set; }
        public SocketTextChannel ModLog { get { return (SocketTextChannel)Client.GetChannel(552929708959072294); } }

        [SlashCommand("ban", "Bans a user")]
        public async Task BanCommand(SocketGuildUser user, string reason = null)
        {
            await user.BanAsync(0, reason);
            await RespondAsync($"{user.Mention} has been banned.{(reason != null ? $" | Reason: `{reason}`" : null)}");

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("User Banned")
                .WithAuthor(Context.User)
                .WithColor(255, 0, 0)
                .AddField("User", user)
                .AddField("Reason", reason ?? "None")
                .WithFooter($"User ID: {user.Id}")
                .WithCurrentTimestamp();
            await ModLog.SendMessageAsync(embed: embed.Build());

            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("DELETE FROM verification WHERE userid=@userid", new { userid = user.Id });
        }

        [SlashCommand("kick", "Kicks a user")]
        public async Task KickCommand(SocketGuildUser user, string reason = null)
        {
            await user.KickAsync(reason);
            await RespondAsync($"{user.Mention} has been kicked.{(reason != null ? $" | Reason: `{reason}`" : null)}");

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("User Kicked")
                .WithAuthor(Context.User)
                .WithColor(255, 0, 0)
                .AddField("User", user)
                .AddField("Reason", reason != "" ? reason : "None")
                .WithFooter($"User ID: {user.Id}")
                .WithCurrentTimestamp();
            await ModLog.SendMessageAsync(embed: embed.Build());

            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("DELETE FROM verification WHERE userid=@userid", new { userid = user.Id });
        }

        [SlashCommand("mute", "Mutes a user")]
        public async Task MuteCommand(SocketGuildUser user, string duration, string reason = null)
        {
            if (user.IsTimedOut())
            {
                await RespondAsync($"{user.Mention} is already muted.", ephemeral: true);
                return;
            }
            TimeSpan timeSpan = duration[^1] switch
            {
                'd' => TimeSpan.FromDays(int.Parse(duration.Replace("d", ""))),
                'h' => TimeSpan.FromHours(int.Parse(duration.Replace("h", ""))),
                'm' => TimeSpan.FromMinutes(int.Parse(duration.Replace("m", ""))),
                's' => TimeSpan.FromSeconds(int.Parse(duration.Replace("s", ""))),
                _ => TimeSpan.Zero
            };
            if (timeSpan == TimeSpan.Zero)
            {
                await RespondAsync("Please enter the duration in this format: `_d`, `_h`, `_m` or `_s`.", ephemeral: true);
                return;
            }
            await user.SetTimeOutAsync(timeSpan, new() { AuditLogReason = reason });
            DateTimeOffset unmute = DateTimeOffset.Now + timeSpan;
            await RespondAsync($"Muted {user.Mention} until <t:{unmute.ToUnixTimeSeconds()}>.{(reason != null ? $" | Reason: `{reason}`" : null)}");

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("User Muted")
                .WithAuthor(Context.User)
                .WithColor(255, 0, 0)
                .AddField("User", user, true)
                .AddField("Ends at", $"<t:{unmute.ToUnixTimeSeconds()}>", true)
                .AddField("Reason", reason ?? "None")
                .WithFooter($"User ID: {user.Id}")
                .WithCurrentTimestamp();
            await ModLog.SendMessageAsync(embed: embed.Build());
        }

        [SlashCommand("unmute", "Unmutes a user")]
        public async Task UnmuteCommand(SocketGuildUser user, string reason = null)
        {
            if (!user.IsTimedOut())
            {
                await RespondAsync($"{user.Mention} is not muted.", ephemeral: true);
                return;
            }
            await user.RemoveTimeOutAsync(new() { AuditLogReason = reason });
            await RespondAsync($"Unmuted {user.Mention}.{(reason != null ? $" | Reason: `{reason}`" : null)}");

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("User Unmuted")
                .WithAuthor(Context.User)
                .WithColor(0, 255, 0)
                .AddField("User", user, true)
                .AddField("Reason", reason ?? "None")
                .WithFooter($"User ID: {user.Id}")
                .WithCurrentTimestamp();
            await ModLog.SendMessageAsync(embed: embed.Build());
        }

        [Group("warning", "Manages warnings")]
        public class WarnCommand : InteractionModuleBase<SocketInteractionContext>
        {
            public DiscordSocketClient Client { get; set; }
            public SocketTextChannel ModLog { get { return (SocketTextChannel)Client.GetChannel(552929708959072294); } }

            [SlashCommand("add", "Warns a user")]
            public async Task AddWarningCommand(SocketGuildUser user, string reason)
            {
                await RespondAsync($"{user.Mention} warned for `{reason}`. Total warnings: `{user.GetWarnings().Count() + 1}`");
                IUserMessage response = await Context.Interaction.GetOriginalResponseAsync();
                await user.AddWarningAsync(response, reason, Context.User.Id);
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Warning issued")
                    .WithAuthor(Context.User)
                    .WithColor(66, 134, 244)
                    .WithDescription($"[Link to message]({response.GetJumpUrl()})")
                    .AddField("User", user)
                    .AddField("Reason", reason)
                    .WithCurrentTimestamp()
                    .WithFooter($"User ID: {user.Id}");
                await ModLog.SendMessageAsync(embed: embed.Build());
            }

            [SlashCommand("get", "Gets a user's warnings")]
            public async Task GetWarningsCommand(SocketGuildUser user)
            {
                IEnumerable<Warning> warnings = user.GetWarnings();
                int warningCount = warnings.Count();
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Warning list")
                    .WithAuthor(user)
                    .WithColor(114, 137, 218);
                switch (warningCount)
                {
                    case 1:
                        embed.WithDescription($"User has 1 warning.");
                        break;
                    default:
                        embed.WithDescription($"User has {warningCount} warnings.");
                        break;
                }
                foreach (Warning warning in warnings)
                {
                    long timestamp = warning.Timestamp;
                    if (warning.WarningID > 40) timestamp /= 1000;
                    string link = Context.Guild.GetTextChannel(warning.ChannelID).GetMessageAsync(warning.MessageID).Result.GetJumpUrl();
                    string field =
                        $"Mod: <@{warning.ModID}>\n" +
                        $"Reason: {warning.Reason}\n" +
                        $"Timestamp: <t:{timestamp}>\n" +
                        $"[Link to message]({link})";
                    embed.AddField($"WarningID: {warning.WarningID}", field);
                }
                await RespondAsync(embed: embed.Build());
            }
        }

        [Group("suggestion", "Manages suggestions")]
        public class SuggestionCommand : InteractionModuleBase<SocketInteractionContext>
        {
            public SuggestionService Suggestions { get; set; }
            
            [SlashCommand("approve", "Approves a suggestion")]
            public async Task ApproveSuggestionCommand(int number, string reason = "No reason given")
            {
                if (number < 7)
                {
                    await RespondAsync("Due to some stupid mistake, our suggestions start at 7.", ephemeral: true);
                    return;
                }
                await RespondAsync((await Suggestions.ApproveAsync(number, Context.User, reason)).Value);
            }

            [SlashCommand("deny", "Denies a suggestion")]
            public async Task DenySuggestionCommand(int number, string reason = "No reason given")
            {
                if (number < 7)
                {
                    await RespondAsync("Due to some stupid mistake, our suggestions start at 7.", ephemeral: true);
                    return;
                }
                await RespondAsync((await Suggestions.DenyAsync(number, Context.User, reason)).Value);
            }

            [SlashCommand("implemented", "Marks a suggestion as implemented")]
            public async Task ImplementedSuggestionCommand(int number, string reason = "No reason given")
            {
                if (number < 7)
                {
                    await RespondAsync("Due to some stupid mistake, our suggestions start at 7.", ephemeral: true);
                    return;
                }
                await RespondAsync((await Suggestions.ImplementedAsync(number, Context.User, reason)).Value);
            }

            [SlashCommand("consider", "Considers a suggestion")]
            public async Task ConsiderSuggestionCommand(int number, string reason = "No reason given")
            {
                if (number < 7)
                {
                    await RespondAsync("Due to some stupid mistake, our suggestions start at 7.", ephemeral: true);
                    return;
                }
                await RespondAsync((await Suggestions.ConsiderAsync(number, Context.User, reason)).Value);
            }
        }
    }
}
