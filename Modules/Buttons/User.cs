using Dapper;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Ja3farBot.Modules.Modals;
using Ja3farBot.Services;
using MySql.Data.MySqlClient;
using static Ja3farBot.Util.FileDatatypes;

namespace Ja3farBot.Modules.Buttons
{
    public class User : InteractionModuleBase<SocketInteractionContext>
    {
        public RoleSelectionService RoleSelector { get; set; }

        [ComponentInteraction("buttons:user:modapplication2:*")]
        public async Task ContinueModApplicationButton(ulong messageID)
        {
            await RespondWithModalAsync<ModApplicationModalTwo>($"modals:user:modapplication2:{messageID}");
            await ModifyOriginalResponseAsync(x => x.Components = new ComponentBuilder().Build());
        }

        [ComponentInteraction("buttons:user:quitmodapplication:*")]
        public async Task QuitModApplicationButton(ulong messageID)
        {
            await RespondAsync("Your application has been canceled. Thank you for your time.", ephemeral: true);
            await ModifyOriginalResponseAsync(x => x.Components = new ComponentBuilder().Build());
            await Context.Guild.GetTextChannel(553926270417633281).DeleteMessageAsync(messageID);
        }

        [ComponentInteraction("buttons:user:togglerole:*")]
        public async Task ToggleRoleButton()
        {
            SocketMessageComponent button = (SocketMessageComponent)Context.Interaction;
            ulong roleID = ulong.Parse(button.Data.CustomId.Split(":")[^1]);
            RoleSetting roleSetting = RoleSelector.GetRoleSetting(roleID);
            SocketGuildUser user = (SocketGuildUser)Context.User;
            if (!user.Roles.Select(x => x.Id).Contains(roleID))
            {
                await user.AddRoleAsync(roleID);
                await RespondAsync($"Role {MentionUtils.MentionRole(roleID)} added.", ephemeral: true);
            }
            else
            {
                await user.RemoveRoleAsync(roleID);
                await RespondAsync($"Role {MentionUtils.MentionRole(roleID)} removed.", ephemeral: true);
            }
        }

        [ComponentInteraction("buttons:user:verify")]
        public async Task VerifyButton()
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            if (await connection.ExecuteScalarAsync<bool>("SELECT messageid FROM verification WHERE userid=@userid", new { userid = Context.User.Id }))
            {
                await RespondAsync("Your previous verification form is pending, please wait.", ephemeral: true);
                return;
            }
            await RespondWithModalAsync<VerificationModal>("modals:user:verification");
        }
    }
}
