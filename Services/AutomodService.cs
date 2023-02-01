using Discord;
using Discord.WebSocket;
using static Ja3farBot.Services.ConfigService;

namespace Ja3farBot.Services
{
    public class AutomodService
    {
        private readonly DiscordSocketClient _client;
        public AutomodService(DiscordSocketClient client)
        {
            _client = client;
        }

        public void Initialize()
        {
            _client.MessageReceived += async (message) =>
            {
                if (message is not SocketUserMessage userMessage || message.Author.IsBot || message.Channel is not SocketGuildChannel) return;
                foreach (string phrase in Config.BannedWords) if (message.Content.ToLower().Contains(phrase))
                    {
                        await LogActionAsync(message, "Banned word");
                        await message.DeleteAsync();
                        return;
                    }
            };
            _client.MessageReceived += async (message) =>
            {
                if (message is not SocketUserMessage userMessage || message.Author.IsBot || message.Channel is not SocketGuildChannel) return;
                if (message.MentionedUsers.Count > Config.MaxMentions)
                {
                    await LogActionAsync(message, "Mass mentions");
                    await message.DeleteAsync();
                }
            };
        }

        private async Task LogActionAsync(SocketMessage message, string reason)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Automod action")
                .WithColor(221, 95, 83)
                .WithAuthor(message.Author)
                .WithDescription($"Message sent in {MentionUtils.MentionChannel(message.Channel.Id)}.")
                .AddField("Message", message.Content)
                .AddField("Reason", reason)
                .WithFooter($"Message ID: {message.Id}")
                .WithCurrentTimestamp();
            await _client.Guilds.First().GetTextChannel(552923852796198922).SendMessageAsync(embed: embed.Build());
        }
    }
}
