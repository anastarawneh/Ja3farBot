using Discord;
using Discord.Rest;
using Discord.WebSocket;
using static Ja3farBot.Util.Datatypes;

namespace Ja3farBot.Services
{
    public class SuggestionService
    {
        private readonly DiscordSocketClient _client;
        private SocketTextChannel _channel;
        private SocketTextChannel _logChannel;
        public SuggestionService(DiscordSocketClient client)
        {
            _client = client;
        }

        public void Initialize()
            => _client.GuildAvailable += OnGuildLoad;

        private Task OnGuildLoad(SocketGuild guild)
        {
            _channel = guild.GetTextChannel(630084536264425474);
            _logChannel = guild.GetTextChannel(630076630181609483);
            _client.GuildAvailable -= OnGuildLoad;
            return Task.CompletedTask;
        }

        public async Task<bool> AddSuggestionAsync(SocketUser User, string Suggestion)
        {
            int count = await GetNextCountAsync();
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(User)
                .WithTitle($"Suggestion #{count}")
                .WithDescription(Suggestion)
                .WithColor(114, 137, 218);
            RestUserMessage message = await _channel.SendMessageAsync(embed: embed.Build());
            await message.AddReactionsAsync(new Emoji[]
            {
                new Emoji("⬆️"),
                new Emoji("⬇️")
            });
            return true;
        }

        public async Task<KeyValuePair<bool, string>> ApproveAsync(int Number, SocketUser Moderator, string Reason)
        {
            Suggestion suggestion = await GetSuggestionAsync(Number);
            if (suggestion.State == SuggestionState.Approved) return new(false, "Suggestion is already approved.");
            EmbedBuilder embed = suggestion.Message.Embeds.First().ToEmbedBuilder();
            if (suggestion.State != SuggestionState.Normal) embed.Fields.Clear();
            embed.WithTitle($"Suggestion #{Number} Approved")
                .AddField($"Reason from {Moderator}", Reason)
                .WithColor(75, 255, 68);
            await suggestion.Message.ModifyAsync(x => x.Embed = embed.Build());
            await _logChannel.SendMessageAsync(embed: embed.Build());
            return new(true, $"Approved suggestion #{Number}.");
        }
        public async Task<KeyValuePair<bool, string>> DenyAsync(int Number, SocketUser Moderator, string Reason)
        {
            Suggestion suggestion = await GetSuggestionAsync(Number);
            if (suggestion.State == SuggestionState.Denied) return new(false, "Suggestion is already denied.");
            EmbedBuilder embed = suggestion.Message.Embeds.First().ToEmbedBuilder();
            if (suggestion.State != SuggestionState.Normal) embed.Fields.Clear();
            embed.WithTitle($"Suggestion #{Number} Denied")
                .AddField($"Reason from {Moderator}", Reason)
                .WithColor(255, 75, 66);
            await suggestion.Message.ModifyAsync(x => x.Embed = embed.Build());
            await _logChannel.SendMessageAsync(embed: embed.Build());
            return new(true, $"Denied suggestion #{Number}.");
        }
        public async Task<KeyValuePair<bool, string>> ImplementedAsync(int Number, SocketUser Moderator, string Reason)
        {
            Suggestion suggestion = await GetSuggestionAsync(Number);
            if (suggestion.State == SuggestionState.Implemented) return new(false, "Suggestion is already implemented.");
            EmbedBuilder embed = suggestion.Message.Embeds.First().ToEmbedBuilder();
            if (suggestion.State != SuggestionState.Normal) embed.Fields.Clear();
            embed.WithTitle($"Suggestion #{Number} Implemented")
                .AddField($"Reason from {Moderator}", Reason)
                .WithColor(145, 251, 255);
            await suggestion.Message.ModifyAsync(x => x.Embed = embed.Build());
            await _logChannel.SendMessageAsync(embed: embed.Build());
            return new(true, $"Implemented suggestion #{Number}.");
        }
        public async Task<KeyValuePair<bool, string>> ConsiderAsync(int Number, SocketUser Moderator, string Reason)
        {
            Suggestion suggestion = await GetSuggestionAsync(Number);
            if (suggestion.State == SuggestionState.Considered) return new(false, "Suggestion is already considered.");
            EmbedBuilder embed = suggestion.Message.Embeds.First().ToEmbedBuilder();
            if (suggestion.State != SuggestionState.Normal) embed.Fields.Clear();
            embed.WithTitle($"Suggestion #{Number} Considered")
                .AddField($"Reason from {Moderator}", Reason)
                .WithColor(253, 255, 145);
            await suggestion.Message.ModifyAsync(x => x.Embed = embed.Build());
            await _logChannel.SendMessageAsync(embed: embed.Build());
            return new(true, $"Considered suggestion #{Number}.");
        }

        private async Task<Suggestion> GetSuggestionAsync(int number)
        {
            IUserMessage message = (IUserMessage)(await _channel.GetMessagesAsync(100).FlattenAsync()).First(x => x.Embeds.First().Title == $"Suggestion #{number}" || x.Embeds.First().Title.Contains($"Suggestion #{number} "));
            return new()
            {
                Author = message.Embeds.First().Author.Value.Name,
                Message = message,
                Number = number,
                Text = message.Embeds.First().Description,
                State = GetState(message),
                Reason = GetReason(message),
                Moderator = GetMod(message)
            };
        }
        
        public SocketTextChannel GetSuggestionChannel()
            => _channel;

        private async Task<int> GetNextCountAsync()
        {
            IMessage message = (await _channel.GetMessagesAsync(1).FlattenAsync()).First();
            return int.Parse(message.Embeds.First().Title.Split('#')[1].Split(' ')[0]) + 1;
        }

        private SuggestionState GetState(IUserMessage message)
        {
            string title = message.Embeds.First().Title;
            if (title.Contains("Approved")) return SuggestionState.Approved;
            if (title.Contains("Denied")) return SuggestionState.Denied;
            if (title.Contains("Considered")) return SuggestionState.Considered;
            if (title.Contains("Implemented")) return SuggestionState.Implemented;
            return SuggestionState.Normal;
        }

        public string GetReason(IUserMessage message)
            => message.Embeds.First().Fields.Any() ? message.Embeds.First().Fields.First().Value : "None";

        public string GetMod(IUserMessage message)
            => message.Embeds.First().Fields.Any() ? message.Embeds.First().Fields.First().Name[12..] : "None";
    }
}
