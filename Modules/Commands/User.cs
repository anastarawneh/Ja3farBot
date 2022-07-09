using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Ja3farBot.Modules.Modals;
using Ja3farBot.Services;
using Ja3farBot.Util;

namespace Ja3farBot.Modules.Commands
{
    public class User : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("apply", "Send a mod application")]
        public async Task ApplyCommand()
        {
            await RespondWithModalAsync<ModApplicationModalOne>("modals:user:modapplication1");
        }
        
        [Group("customvc", "Custom Voice Channel commands")]
        public class CustomVCCommand : InteractionModuleBase<SocketInteractionContext>
        {
            public CustomVCService CustomVCs { get; set; }

            [SlashCommand("create", "Creates a custom Voice Channel")]
            public async Task CreateCustomVCCommand(int slots = 5, int bitrate = 64000)
            {
                if (await CustomVCs.HasCustomVC(Context.User))
                {
                    await RespondAsync("You already have a custom Voice Channel.", ephemeral: true);
                    return;
                }
                await CustomVCs.CreateCustomVC(Context.User, slots, bitrate);
                await RespondAsync("Custom Voice Channel created. Load it with `/customvc load`.", ephemeral: true);
            }
            
            [SlashCommand("load", "Loads your custom Voice Channel")]
            public async Task LoadCustomVCCommand()
            {
                await RespondAsync(await CustomVCs.Load(await CustomVCs.GetOrCreateCustomVC(Context.User)), ephemeral: true);
            }

            [SlashCommand("unload", "Unloads your custom Voice Channel")]
            public async Task UnloadCustomVCCommand()
            {
                await RespondAsync(await CustomVCs.Unload(await CustomVCs.GetOrCreateCustomVC(Context.User)), ephemeral: true);
            }

            [SlashCommand("modify", "Modified your custom Voice Channel settings")]
            public async Task ModifyCustomVCCommand(int slots = -1, int bitrate = -1)
            {
                if (slots == -1 && bitrate == -1)
                {
                    await RespondAsync("You must set one property to modify.", ephemeral: true);
                    return;
                }
                if (slots != -1 && (slots < 1 || slots > 99))
                {
                    await RespondAsync("Slots must be between 1 and 99.", ephemeral: true);
                    return;
                }
                if (bitrate != -1 && (bitrate < 8000 || bitrate > 128000))
                {
                    await RespondAsync("Bitrate must be between 8000 and 128000.", ephemeral: true);
                    return;
                }
                await RespondAsync(await CustomVCs.ModifyCustomVC(await CustomVCs.GetOrCreateCustomVC(Context.User), slots, bitrate), ephemeral: true);
            }
        }

        [Group("event", "Event commands")]
        public class EventCommand : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("create", "Creates an event")]
            public async Task CreateEventCommand(string title, DateTime time, string location, string notes = null)
            {
                int id = Random.Shared.Next(0, 100000);
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle($"{Context.User} is hosting an event!")
                    .WithAuthor(Context.User)
                    .WithColor(114, 137, 218)
                    .WithFooter($"React if you're going! | ID: {id}")
                    .AddField("Event", title)
                    .AddField("Time", $"<t:{new DateTimeOffset(time).ToUnixTimeSeconds()}>")
                    .AddField("Location", location)
                    .AddField("Notes", notes ?? "None");
                IUserMessage message = await Context.Guild.GetTextChannel(643845902871560202).SendMessageAsync(MentionUtils.MentionRole(644974763692785664), embed: embed.Build());
                await message.AddReactionAsync(new Emoji("✅"));
                await RespondAsync($"Event posted in {MentionUtils.MentionChannel(643845902871560202)} with ID `{id}`.", ephemeral: true);
            }

            [SlashCommand("edit", "Edits an event")]
            public async Task EditEventCommand(int id, string title = null, DateTime? time = null, string location = null, string notes = null)
            {
                IEnumerable<IMessage> messages = (await Context.Guild.GetTextChannel(643845902871560202).GetMessagesAsync(100).FlattenAsync()).Where(x => x.Embeds.First().Footer.Value.Text.Contains($"ID: {id}") && x.Embeds.First().Author.Value.Name == $"{Context.User.Username}#{Context.User.Discriminator}");
                if (!messages.Any())
                {
                    await RespondAsync("Invalid ID, or you are not the author of the event.", ephemeral: true);
                    return;
                }
                IUserMessage message = (IUserMessage)messages.First();
                EmbedBuilder embed = message.Embeds.First().ToEmbedBuilder();
                if (title != null) embed.Fields.First(x => x.Name == "Event").Value = title;
                if (time != null) embed.Fields.First(x => x.Name == "Time").Value = $"<t:{new DateTimeOffset(time.Value).ToUnixTimeSeconds()}>";
                if (location != null) embed.Fields.First(x => x.Name == "Location").Value = location;
                if (notes != null) embed.Fields.First(x => x.Name == "Notes").Value = notes;
                await message.ModifyAsync(x => x.Embed = embed.Build());
                await RespondAsync("Event modified.", ephemeral: true);
            }

            [SlashCommand("delete", "Deletes an event")]
            public async Task DeleteEvent(int id)
            {
                IEnumerable<IMessage> messages = (await Context.Guild.GetTextChannel(643845902871560202).GetMessagesAsync(100).FlattenAsync()).Where(x => x.Embeds.First().Footer.Value.Text.Contains($"ID: {id}") && x.Embeds.First().Author.Value.Name == $"{Context.User.Username}#{Context.User.Discriminator}");
                if (!messages.Any())
                {
                    await RespondAsync("Invalid ID, or you are not the author of the event.", ephemeral: true);
                    return;
                }
                await messages.First().DeleteAsync();
                await RespondAsync("Event deleted.", ephemeral: true);
            }

            [SlashCommand("tos", "Events Terms of Service")]
            public async Task EventTOSCommand()
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Events Terms of Service")
                    .WithDescription("You must be of **16 years of age or older** to participate in events held outside this Discord server. You must also be of **18 years of age or older** to participate in events that are marked 18+. The server administration (Owner and Moderators) is not responsible for any activities that go on during the event. The server administration is not responsible for anyone who doesn’t comply with the Terms above.")
                    .WithFooter("When you use the event system, you agree to the Terms mentioned above.");
                await RespondAsync(embed: embed.Build());
            }
        }

        [SlashCommand("report", "Report a user")]
        public async Task ReportCommand(SocketGuildUser user, bool anonymous, SocketGuildUser user2 = null, SocketGuildUser user3 = null)
        {
            await RespondWithModalAsync<ReportModal>($"modals:user:report:{anonymous}:{user.Id}_{(user2 != null ? user2.Id : 0)}_{(user3 != null ? user3.Id : 0)}");
        }

        [SlashCommand("socials", "Set your socials")]
        public async Task SocialsCommand(string twitter = null, string instagram = null, string snapchat = null)
        {
            await ((SocketGuildUser)Context.User).SetSocialsAsync(twitter, instagram, snapchat);
            await RespondAsync("Socials set.", ephemeral: true);
        }
        
        public SuggestionService Suggestions { get; set; }
        [SlashCommand("suggest", "Make a server suggestion")]
        public async Task SuggestCommand(string suggestion)
        {
            await DeferAsync();
            await Suggestions.AddSuggestionAsync(Context.User, suggestion);
            await FollowupAsync("Thank you for your suggestion!");
        }
    }
}
