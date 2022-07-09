using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Ja3farBot.Util;
using static Ja3farBot.Util.Datatypes;

namespace Ja3farBot.Services
{
    public class EventHandlerService
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly LogService _log;
        public EventHandlerService(IServiceProvider services, DiscordSocketClient client, InteractionService interactions, LogService log)
        {
            _services = services;
            _client = client;
            _interactions = interactions;
            _log = log;
        }
        private IReadOnlyCollection<RestInviteMetadata> _invites;

        public async Task InitializeAsync()
        {
            // General Events
            _client.Ready += async () =>
            {
                await _interactions.RegisterCommandsToGuildAsync(550848068640309259);

                _invites = await _client.Guilds.First().GetInvitesAsync();
            };

            _client.InteractionCreated += async (interaction) =>
            {
                IResult result = await _interactions.ExecuteCommandAsync(new SocketInteractionContext(_client, interaction), _services);
                if (!result.IsSuccess)
                {
                    _log.InteractionError(interaction, result);
                }
            };

            _client.ReactionAdded += StarboardProcessor;
            _client.ReactionRemoved += StarboardProcessor;

            _client.InviteCreated += async (x) => _invites = await _client.Guilds.First().GetInvitesAsync();
            _client.InviteDeleted += async (x, y) => _invites = await _client.Guilds.First().GetInvitesAsync();

            // Logging Events
            ITextChannel logChannel = (ITextChannel)await _client.GetChannelAsync(671023048035663912);
            Color blurple = new(66, 134, 244);
            Color green = new(83, 221, 172);
            Color red = new(221, 95, 83);
            _client.ChannelCreated += async (channel) => {
                bool cancel = channel is not SocketGuildChannel guildChannel || guildChannel.Name.Contains("'s VC") || guildChannel.Guild.GetCategoryChannel(552923152595025930).Channels.Contains(channel);
                if (cancel) return;

                guildChannel = (SocketGuildChannel)channel;
                string channelType = GetChannelType(guildChannel);
                string category = "";
                foreach (SocketCategoryChannel categoryChannel in guildChannel.Guild.CategoryChannels)
                {
                    if (categoryChannel.Channels.Contains(channel)) category = categoryChannel.Name;
                }
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(green)
                    .WithTitle($"{(channelType != "" ? channelType : "Channel")} Created")
                    .AddField("Name", guildChannel.Name)
                    .WithCurrentTimestamp()
                    .WithFooter($"Channel ID: {channel.Id}");
                if (category != "") embed.AddField("Category", category);

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.ChannelDestroyed += async (channel) =>
            {
                bool cancel = channel is not SocketGuildChannel guildChannel || guildChannel.Name.Contains("'s VC");
                if (cancel) return;

                guildChannel = (SocketGuildChannel)channel;
                string channelType = GetChannelType(guildChannel);
                string category = "";
                foreach (SocketCategoryChannel categoryChannel in guildChannel.Guild.CategoryChannels)
                {
                    if (categoryChannel.Channels.Contains(channel)) category = categoryChannel.Name;
                }
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(red)
                    .WithTitle($"{(channelType != "" ? channelType : "Channel")} Deleted")
                    .AddField("Name", guildChannel.Name)
                    .WithCurrentTimestamp()
                    .WithFooter($"Channel ID: {channel.Id}");
                if (category != "") embed.AddField("Category", category);

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.ChannelUpdated += async (channel1, channel2) =>
            {
                bool cancel = 
                    channel1 is not IGuildChannel guildChannel1 || 
                    channel2 is not IGuildChannel guildChannel2 ||
                    guildChannel1.Name == guildChannel2.Name;
                if (cancel) return;

                guildChannel1 = (IGuildChannel)channel1;
                guildChannel2 = (IGuildChannel)channel2;
                string channelType = GetChannelType(guildChannel2);
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(blurple)
                    .WithTitle($"{(channelType != "" ? channelType : "Channel")} Renamed")
                    .AddField("Before", guildChannel1.Name)
                    .AddField("After", guildChannel2.Name)
                    .WithCurrentTimestamp()
                    .WithFooter($"Channel ID: {channel1.Id}");

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.GuildMemberUpdated += async (Cacheable<SocketGuildUser, ulong> user1Cacheable, SocketGuildUser user2) =>
            {
                bool cancel = user1Cacheable.Value.Nickname == user2.Nickname;
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(blurple)
                    .WithAuthor(user2)
                    .WithTitle("Nickname changed")
                    .AddField("Before", user1Cacheable.Value.Nickname == null || user1Cacheable.Value.Nickname == null ? "None" : user1Cacheable.Value.Nickname)
                    .AddField("After", user2.Nickname == null || user2.Nickname == null ? "None" : user2.Nickname)
                    .WithCurrentTimestamp()
                    .WithFooter($"User ID: {user2.Id}");

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.GuildMemberUpdated += async (Cacheable<SocketGuildUser, ulong> user1Cacheable, SocketGuildUser user2) =>
            {
                bool cancel = user1Cacheable.Value.Roles.Count >= user2.Roles.Count;
                if (cancel) return;

                SocketRole role = user2.Roles.Except(user1Cacheable.Value.Roles).First();
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(blurple)
                    .WithAuthor(user2)
                    .WithTitle("Role Added")
                    .AddField("Role", role.Mention)
                    .WithCurrentTimestamp()
                    .WithFooter($"User ID: {user2.Id}");

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.GuildMemberUpdated += async (Cacheable<SocketGuildUser, ulong> user1Cacheable, SocketGuildUser user2) =>
            {
                bool cancel = user1Cacheable.Value.Roles.Count <= user2.Roles.Count;
                if (cancel) return;

                SocketRole role = user1Cacheable.Value.Roles.Except(user2.Roles).First();
                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(blurple)
                    .WithAuthor(user2)
                    .WithTitle("Role Removed")
                    .AddField("Role", role.Mention)
                    .WithCurrentTimestamp()
                    .WithFooter($"User ID: {user2.Id}");

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.MessageDeleted += async (messageCacheable, channelCacheable) =>
            {
                bool cancel = !channelCacheable.HasValue ||
                    !messageCacheable.HasValue ||
                    channelCacheable.Value is IDMChannel ||
                    messageCacheable.Value.Author.IsBot ||
                    new List<ulong> { 553926270417633281, 705496248995676242 }.Contains(channelCacheable.Id);
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(red)
                    .WithAuthor(messageCacheable.Value.Author)
                    .WithTitle($"Message deleted in #{channelCacheable.Value.Name}")
                    .AddField("Message", (messageCacheable.Value.Content == "" || messageCacheable.Value.Content == null ? "None" : messageCacheable.Value.Content))
                    .WithCurrentTimestamp()
                    .WithFooter($"Message ID: {messageCacheable.Id}");
                if (messageCacheable.Value.Attachments.Count > 0)
                {
                    string links = "";
                    foreach (var attachment in messageCacheable.Value.Attachments) links += attachment.Url + "\n";
                    embed.AddField("Attachments", links);
                }

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.MessagesBulkDeleted += async (messageCacheableList, channelCacheable) =>
            {
                bool cancel = channelCacheable.Id == 705471081062072483;
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(red)
                    .WithTitle($"Messages bulk deleted in #{channelCacheable.Value.Name}")
                    .WithCurrentTimestamp();
                IEnumerable<IMessage> ordered = messageCacheableList.Reverse().Select(x => x.GetOrDownloadAsync().Result);
                string messages = "";
                foreach (IMessage message in ordered) if (message.Content != null) messages += $"[{MentionUtils.MentionUser(message.Author.Id)}]: {message.Content}\n";
                if (messages.Length > 1024) await logChannel.SendMessageAsync($"**Messages bulk deleted in #{channelCacheable.Value.Name}**\n{messages}");
                else
                {
                    embed.AddField("Messages", messages);
                    await logChannel.SendMessageAsync("", false, embed.Build());
                }
            };
            _client.MessageUpdated += async (message1Cacheable, message2, channel) =>
            {
                bool cancel = channel is IDMChannel ||
                    message1Cacheable.Value.Author.IsBot ||
                    new List<ulong> { 553926270417633281 }.Contains(channel.Id) ||
                    message1Cacheable.GetOrDownloadAsync().Result == null ||
                    message1Cacheable.Value.Content == null ||
                    message2.Content == null ||
                    message1Cacheable.Value.Content == message2.Content;
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(blurple)
                    .WithAuthor(message2.Author)
                    .WithTitle($"Message edited in #{channel.Name}")
                    .WithDescription($"[Link to message]({message2.GetJumpUrl()})")
                    .AddField("Before", message1Cacheable.Value.Content)
                    .AddField("After", message2.Content)
                    .WithCurrentTimestamp()
                    .WithFooter($"Message ID: {message2.Id}");

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.RoleCreated += async (role) =>
            {
                bool cancel = false;
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(green)
                    .WithTitle("Role created")
                    .WithDescription(MentionUtils.MentionRole(role.Id))
                    .AddField("Mentionable", role.IsMentionable.ToString().CapitalizeFirstLetter(), true)
                    .AddField("Hoisted", role.IsHoisted.ToString().CapitalizeFirstLetter(), true)
                    .WithCurrentTimestamp()
                    .WithFooter($"Role ID: {role.Id}");
                if (role.Color != new Color(0, 0, 0)) embed.WithColor(role.Color);

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.RoleDeleted += async (role) =>
            {
                bool cancel = false;
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(red)
                    .WithTitle("Role deleted")
                    .WithDescription(MentionUtils.MentionRole(role.Id))
                    .AddField("Mentionable", role.IsMentionable.ToString().CapitalizeFirstLetter(), true)
                    .AddField("Hoisted", role.IsHoisted.ToString().CapitalizeFirstLetter(), true)
                    .AddField("Created", Utils.GetDuration(role.CreatedAt.DateTime.ToLocalTime(), DateTime.Now.ToLocalTime()).ToString())
                    .WithCurrentTimestamp()
                    .WithFooter($"Role ID: {role.Id}");

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.RoleUpdated += async (role1, role2) =>
            {
                bool cancel = !(role1.Name != role2.Name && role1.Color == role2.Color) &&
                    !(role1.Name == role2.Name && role1.Color != role2.Color) &&
                    !(role1.Name != role2.Name && role1.Color != role2.Color);
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithFooter($"Role ID: {role2.Id}");

                if (role1.Name != role2.Name && role1.Color == role2.Color)
                {
                    embed.WithColor(blurple)
                        .WithTitle("Role renamed")
                        .AddField("Before", role1.Name, true)
                        .AddField("After", role2.Name, true);
                }
                else if (role1.Name == role2.Name && role1.Color != role2.Color)
                {
                    embed.WithColor(role2.Color)
                        .WithTitle("Role recolored")
                        .AddField("Before", role1.Color.GetRGB(), true)
                        .AddField("After", role2.Color.GetRGB(), true);
                }
                else if (role1.Name != role2.Name && role1.Color != role2.Color)
                {
                    embed.WithColor(role2.Color)
                        .WithTitle("Role updated")
                        .AddField("Before", $"**Name:** {role1.Name}\n**Color:** {role1.Color.GetRGB()}", true)
                        .AddField("After", $"**Name:** {role2.Name}\n**Color:** {role2.Color.GetRGB()}", true);
                }

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.UserJoined += async (user) =>
            {
                bool cancel = false;
                if (cancel) return;

                IReadOnlyCollection<RestInviteMetadata> currentInvites = await user.Guild.GetInvitesAsync();
                RestInviteMetadata invite = _invites.ExceptBy(currentInvites.Select(x => $"{x.Code}{x.Uses}"), x => $"{x.Code}{x.Uses}").First();

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(green)
                    .WithAuthor(user)
                    .WithTitle($"{(user.IsBot ? "Bot" : "User")} joined")
                    .AddField("Invite link", invite.Code, true)
                    .AddField("Invited by", invite.Inviter.Mention, true)
                    .WithCurrentTimestamp()
                    .WithFooter($"User ID: {user.Id} | Member count: {user.Guild.MemberCount}");
                if (!user.IsBot) embed.AddField("Account created", Utils.GetDuration(user.CreatedAt.DateTime.ToLocalTime(), DateTime.Now.ToLocalTime()).ToString());

                await logChannel.SendMessageAsync("", false, embed.Build());

                _invites = await _client.Guilds.First().GetInvitesAsync();
            };
            _client.UserLeft += async (guild, user) =>
            {
                bool cancel = false;
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(red)
                    .WithAuthor(user)
                    .WithTitle($"{(user.IsBot ? "Bot" : "User")} left")
                    .AddField("Joined", !((SocketGuildUser)user).JoinedAt.HasValue ? "Unknown" : Utils.GetDuration(((SocketGuildUser)user).JoinedAt.Value.DateTime.ToLocalTime(), DateTime.Now.ToLocalTime()).ToString())
                    .WithCurrentTimestamp()
                    .WithFooter($"User ID: {user.Id} | Member count: {guild.MemberCount}");
                string roles = "";
                foreach (SocketRole role in ((SocketGuildUser)user).Roles)
                    if (role != ((SocketGuildUser)user).Roles.ToList()[0]) roles += MentionUtils.MentionRole(role.Id) + "\n";
                embed.AddField("Roles", roles == "" ? "None" : roles);

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
            _client.UserUpdated += async (user1, user2) =>
            {
                bool cancel = (user1.Username == user2.Username &&
                    user1.Discriminator == user2.Discriminator) ||
                    user1.MutualGuilds.Count < 1;
                if (cancel) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(blurple)
                    .WithAuthor(user2)
                    .WithTitle("Username changed")
                    .AddField("Before", $"{user1.Username}#{user1.Discriminator}")
                    .AddField("After", $"{user2.Username}#{user2.Discriminator}")
                    .WithCurrentTimestamp()
                    .WithFooter($"User ID: {user1.Id}");

                await logChannel.SendMessageAsync("", false, embed.Build());
            };
        }

        private static string GetChannelType(IChannel channel)
        {
            if (channel is ITextChannel) return "Text Channel";
            else if (channel is IVoiceChannel) return "Voice Channel";
            else if (channel is ICategoryChannel) return "Category Channel";
            else return "";
        }

        private async Task StarboardProcessor(Cacheable<IUserMessage, ulong> messageCacheable, Cacheable<IMessageChannel, ulong> channelCacheable, SocketReaction reaction)
        {
            if (reaction.Emote.Name != "🌟") return;
            IMessageChannel channel = await channelCacheable.GetOrDownloadAsync();
            if (new List<ulong> {
                554012056374738954
            }.Contains(channel.Id)) return;
            IUserMessage message = await messageCacheable.GetOrDownloadAsync();
            if (message.Attachments.Count > 1 || message.Author == reaction.User.Value)
            {
                await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                return;
            }
            StarboardMessage starboardMessage = new()
            {
                Message = message,
                Channel = (SocketTextChannel)channel,
                Author = message.Author,
                Stars = message.Reactions.Where(x => x.Key.Name == "🌟").Any() ? message.Reactions.First(x => x.Key.Name == "🌟").Value.ReactionCount : 0
            };
            await starboardMessage.UpdateAsync();
        }
    }
}
