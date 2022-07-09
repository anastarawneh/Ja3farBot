using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Ja3farBot.Services;
using static Ja3farBot.Services.ConfigService.Config;

namespace Ja3farBot.Modules.Buttons
{
    public class Owner : InteractionModuleBase<SocketInteractionContext>
    {
        public ConfigService Config { get; set; }
        public RoleSelectionService RoleSelector { get; set; }

        [ComponentInteraction("buttons:owner:test")]
        public async Task TestButton()
        {
            try
            {
                await RespondAsync($"CustomId is {((SocketMessageComponent)Context.Interaction).Data.CustomId}.", ephemeral: true);
            }
            catch (Exception ex)
            {
                LogService.Error("Test Button", ex.Message);
            }
        }

        [ComponentInteraction("buttons:owner:refreshpanel")]
        public async Task RefreshPanelButton()
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Ja3farBot Control Panel")
                .WithColor(new(11, 250, 131));
            ComponentBuilder components = new ComponentBuilder()
                .WithButton("Refresh Panel", "buttons:owner:refreshpanel", ButtonStyle.Success)
                .AddRow(new ActionRowBuilder()
                    .AddComponent(new ButtonBuilder("Reload Config", "buttons:owner:reloadconfig", ButtonStyle.Primary).Build())
                    .AddComponent(new ButtonBuilder("Recreate Role Selector", "buttons:owner:recreateroleselector", ButtonStyle.Danger).Build())
                    )
                .AddRow(new ActionRowBuilder()
                    .AddComponent(new ButtonBuilder("Send Announcement", "buttons:owner:sendannouncement", ButtonStyle.Primary).Build())
                );
            await Context.Channel.ModifyMessageAsync(944996638995406888, message =>
            {
                message.Embed = embed.Build();
                message.Components = components.Build();
            });
            await RespondAsync("Panel refreshed.", ephemeral: true);
        }

        [ComponentInteraction("buttons:owner:sendannouncement")]
        public async Task SendAnnouncementButton()
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle(Announcement.Title)
                .WithColor(114, 137, 218)
                .WithDescription(Announcement.Description)
                .WithFooter("Please leave any feedback about this update in #feedback.");
            foreach (EmbedFieldBuilder field in Announcement.Fields)
                embed.AddField(field);
            ComponentBuilder component = new ComponentBuilder()
                .WithButton("@Announcement Pings", "buttons:owner:announce:announcementpings")
                .WithButton("@everyone", "buttons:owner:announce:everyone");
            await RespondAsync(embed: embed.Build(), components: component.Build(), ephemeral: true);
        }

        [ComponentInteraction("buttons:owner:announce:*")]
        public async Task AnnonceButton()
        {
            SocketMessageComponent button = (SocketMessageComponent)Context.Interaction;
            string mention = button.Data.CustomId.Split(":")[^1] switch
            {
                "announcementpings" => MentionUtils.MentionRole(982014377798545438),
                "everyone" => "@everyone",
                _ => null
            };
            Embed embed = button.Message.Embeds.First();
            await Context.Guild.GetTextChannel(554012056374738954).SendMessageAsync(mention, embed: embed);
            await RespondAsync("Announcement made.", ephemeral: true);
        }

        [ComponentInteraction("buttons:owner:recreateroleselector")]
        public async Task ReloadRoleSelectorButton()
        {
            await DeferAsync();

            SocketTextChannel channel = Context.Guild.GetTextChannel(555432146987122691);
            await channel.DeleteMessageAsync(RoleSelector.GetMessageID("general"));
            await channel.DeleteMessageAsync(RoleSelector.GetMessageID("color"));
            await channel.DeleteMessageAsync(RoleSelector.GetMessageID("notification"));

            await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("General Roles")
                .WithColor(114, 137, 218)
                .AddField("There are no general roles available at the moment.", "Check back later for another event.")
                .Build());
            await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("Color Roles")
                .WithColor(114, 137, 218)
                .AddField("There are no general roles available at the moment.", "Check back later for another event.")
                .WithFooter("Note: if there are multiple roles in this list, you will only be able to select one.")
                .Build());
            await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("Notification Roles")
                .WithColor(114, 137, 218)
                .AddField("There are no notification roles available at the moment.", "Check back later for another event.")
                .Build());

            await FollowupAsync("Recreated #role-selection.", ephemeral: true);
        }

        [ComponentInteraction("buttons:owner:reloadconfig")]
        public async Task ReloadConfigButton()
        {
            Config.ReadConfig(Config.GetFilePath("config.yml"));
            await RespondAsync("Reloaded the configuration file.", ephemeral: true);
        }
    }
}
