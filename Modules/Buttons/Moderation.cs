﻿using Dapper;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Ja3farBot.Services;
using MySql.Data.MySqlClient;

namespace Ja3farBot.Modules.Buttons
{
    public class Moderation : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("buttons:moderation:acceptverification")]
        public async Task AcceptVerificationButton()
        {
            await DeferAsync();
            SocketMessageComponent button = (SocketMessageComponent)Context.Interaction;
            await button.Message.ModifyAsync(x =>
            {
                x.Components = new ComponentBuilder()
                    .WithButton("Accept", "buttons:moderation:acceptverification", ButtonStyle.Success, disabled: true)
                    .Build();
            });
            SocketGuildUser user = Context.Guild.GetUser(ulong.Parse(button.Message.Embeds.First().Footer.Value.Text.Replace("User ID: ", "")));
            await user.RemoveRoleAsync(705470408522072086);
            await Context.Guild.GetTextChannel(550848069160271903).SendMessageAsync($"{user.Mention} has joined! Say hello everyone!");
            await FollowupAsync("User verified.", ephemeral: true);

            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("UPDATE verification SET verified=1 WHERE userid=@userid", new { userid = user.Id });
        }

        [ComponentInteraction("buttons:moderation:denyverification")]
        public async Task DenyVerificationButton()
        {
            await DeferAsync();
            SocketMessageComponent button = (SocketMessageComponent)Context.Interaction;
            await button.Message.ModifyAsync(x =>
            {
                x.Components = new ComponentBuilder()
                    .WithButton("Deny", "buttons:moderation:denyverification", ButtonStyle.Danger, disabled: true)
                    .Build();
            });
            SocketGuildUser user = Context.Guild.GetUser(ulong.Parse(button.Message.Embeds.First().Footer.Value.Text.Replace("User ID: ", "")));
            await user.SendMessageAsync("Your verification has been denied. Please resend the verification form correctly.");
            await FollowupAsync("User denied.", ephemeral: true);

            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("UPDATE verification SET messageid=0 WHERE userid=@userid", new { userid = user.Id });
        }

        [ComponentInteraction("buttons:moderation:kickverification")]
        public async Task KickVerificationButton()
        {
            await DeferAsync();
            SocketMessageComponent button = (SocketMessageComponent)Context.Interaction;
            await button.Message.ModifyAsync(x =>
            {
                x.Components = new ComponentBuilder()
                    .WithButton("Kick", "buttons:moderation:kickverification", ButtonStyle.Danger, disabled: true)
                    .Build();
            });
            SocketGuildUser user = Context.Guild.GetUser(ulong.Parse(button.Message.Embeds.First().Footer.Value.Text.Replace("User ID: ", "")));
            await user.KickAsync($"Failed verification. Mod ID: {Context.User.Id}");
            await FollowupAsync("User kicked.", ephemeral: true);

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("User Kicked")
                .WithAuthor(Context.User)
                .WithColor(255, 0, 0)
                .AddField("User", user)
                .AddField("Reason", "Failed verification.")
                .WithFooter($"User ID: {user.Id}")
                .WithCurrentTimestamp();
            await Context.Guild.GetTextChannel(552929708959072294).SendMessageAsync(embed: embed.Build());

            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("DELETE FROM verification WHERE userid=@userid", new { userid = user.Id });
        }
    }
}
