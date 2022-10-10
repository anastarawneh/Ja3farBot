using Dapper;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Ja3farBot.Services;
using MySql.Data.MySqlClient;

namespace Ja3farBot.Modules.Modals
{
    public class User : InteractionModuleBase<SocketInteractionContext>
    {
        [ModalInteraction("modals:user:modapplication1")]
        public async Task ModApplicationModalOne(ModApplicationModalOne modal)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Moderation Application")
                .WithColor(114, 137, 218)
                .WithFooter($"User ID: {Context.User.Id}")
                .AddField("First of all, how old are you?", modal.Age)
                .AddField("What is the timezone of your current place of residence?", modal.Timezone)
                .AddField("Tell us a little about yourself.", modal.AboutMe)
                .AddField("How many hours a day do you average on Discord?", modal.Activity);
            RestUserMessage message = await Context.Guild.GetTextChannel(646771451759951883).SendMessageAsync(embed: embed.Build());
            ComponentBuilder component = new ComponentBuilder()
                .WithButton("Continue", $"buttons:user:modapplication2:{message.Id}")
                .WithButton("Cancel", $"buttons:user:quitmodapplication:{message.Id}");
            await RespondAsync("Click here to continue your application or cancel it.", ephemeral: true, components: component.Build());
        }
        [ModalInteraction("modals:user:modapplication2:*")]
        public async Task ModApplicationModalTwo(ulong messageID, ModApplicationModalTwo modal)
        {
            IUserMessage message = (IUserMessage)await Context.Guild.GetTextChannel(646771451759951883).GetMessageAsync(messageID);
            EmbedBuilder embed = message.Embeds.First().ToEmbedBuilder()
                .AddField("What do you think you could bring to the server as a moderator?", modal.Potential)
                .AddField("Do you have any previous moderation experiences on or off Discord?", modal.Experience)
                .AddField("Do you have any general suggestions to improve the server?", modal.Suggestions);
            await message.ModifyAsync(x => x.Embed = embed.Build());
            await RespondAsync("Your application has been recorded. Thank you for your time.", ephemeral: true);
        }

        [ModalInteraction("modals:user:report:*:*_*_*")]
        public async Task ReportModal(bool anonymous, ulong userID, ulong user2ID, ulong user3ID, ReportModal modal)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("User report")
                .WithDescription($"User: {(anonymous ? "`Anonymous`" : Context.User.Mention)}")
                .AddField("Offending user", $"{Context.Guild.GetUser(userID).DisplayName}#{Context.Guild.GetUser(userID).Discriminator} ({MentionUtils.MentionUser(userID)})")
                .WithFooter($"User ID: {Context.User.Id}")
                .WithCurrentTimestamp();
            if (user2ID != 0) embed.AddField("Offending user", $"{Context.Guild.GetUser(user2ID).DisplayName}#{Context.Guild.GetUser(user2ID).Discriminator} ({MentionUtils.MentionUser(user2ID)})");
            if (user3ID != 0) embed.AddField("Offending user", $"{Context.Guild.GetUser(user3ID).DisplayName}#{Context.Guild.GetUser(user3ID).Discriminator} ({MentionUtils.MentionUser(user3ID)})");
            embed.AddField("Offense(s)", modal.Messages)
                .AddField("Notes", modal.Notes != "" ? modal.Notes : "None");
            await Context.Guild.GetTextChannel(552929708959072294).SendMessageAsync(MentionUtils.MentionRole(550851398821216272), embed: embed.Build());
            await RespondAsync("Thank you for your report. We'll look into your case as soon as possible.", ephemeral: true);
        }

        [ModalInteraction("modals:user:verification")]
        public async Task VerificationModal(VerificationModal modal)
        {
            await DeferAsync();

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Verification Entry")
                .WithAuthor(Context.User)
                .WithColor(66, 134, 244)
                .AddField("Where do you come from?", modal.Location)
                .AddField("Where did you find our server?", modal.Source)
                .WithFooter($"User ID: {Context.User.Id}");
            ComponentBuilder components = new ComponentBuilder()
                .WithButton("Accept", "buttons:moderation:acceptverification", ButtonStyle.Success)
                .WithButton("Deny", "buttons:moderation:denyverification", ButtonStyle.Danger)
                .WithButton("Kick", "buttons:moderation:kickverification", ButtonStyle.Danger);
            RestUserMessage message = await Context.Guild.GetTextChannel(973650806605754389).SendMessageAsync(embed: embed.Build(), components: components.Build());

            await FollowupAsync("Thank you! You will be verified shortly.", ephemeral: true);

            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("INSERT INTO verification (userid, messageid) VALUES (@userid, @messageid)", new { userid = Context.User.Id, messageid = message.Id });
        }
    }


    public class ModApplicationModalOne : IModal
    {
        public string Title => "Mod Application Form Page 1";

        [InputLabel("How old are you?"), ModalTextInput("modals:user:modapplication:age", TextInputStyle.Short, maxLength: 2)]
        public int Age { get; set; }

        [InputLabel("What is your timezone?"), ModalTextInput("modals:user:modapplication:timezone", TextInputStyle.Short, maxLength: 5)]
        public string Timezone { get; set; }

        [InputLabel("Tell us a little about yourself."), ModalTextInput("modals:user:modapplication:aboutme", TextInputStyle.Paragraph)]
        public string AboutMe { get; set; }

        [InputLabel("How many hours a day do you spend on Discord?"), ModalTextInput("modals:user:modapplication:activity", TextInputStyle.Short)]
        public string Activity { get; set; }
    }
    public class ModApplicationModalTwo : IModal
    {
        public string Title => "Mod Application Form Page 2";

        [InputLabel("Question 5"), ModalTextInput("modals:user:modapplication:potential", TextInputStyle.Paragraph, "What do you think you could bring to the server as a moderator?")]
        public string Potential { get; set; }

        [InputLabel("Question 6"), ModalTextInput("modals:user:modapplication:experience", TextInputStyle.Paragraph, "Do you have any previous moderation experiences on or off Discord?")]
        public string Experience { get; set; }

        [InputLabel("Question 7"), ModalTextInput("modals:user:modapplication:suggestions", TextInputStyle.Paragraph, "Do you have any general suggestions to improve the server?")]
        public string Suggestions { get; set; }
    }

    public class ReportModal : IModal
    {
        public string Title => "Report a user";

        [InputLabel("Please input offending message links or IDs.")]
        [ModalTextInput("modals:user:report:messages", TextInputStyle.Paragraph)]
        public string Messages { get; set; }

        [InputLabel("Please input any other notes.")]
        [ModalTextInput("modals:user:report:notes", TextInputStyle.Paragraph)]
        [RequiredInput(false)]
        public string Notes { get; set; }
    }

    public class VerificationModal : IModal
    {
        public string Title => "Verification Form";
        [InputLabel("Where do you come from?")]
        [ModalTextInput("modals:user:verification:location", TextInputStyle.Short)]
        public string Location { get; set; }
        [InputLabel("Where did you find our server?")]
        [ModalTextInput("modals:user:verification:source", TextInputStyle.Paragraph, "If you found us through another user, please mention them.")]
        public string Source { get; set; }
    }
}
