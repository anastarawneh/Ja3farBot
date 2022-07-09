using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Ja3farBot.Services;
using static Ja3farBot.Util.FileDatatypes;

namespace Ja3farBot.Modules.Commands
{
    public class Owner : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("test", "Test command, do not touch!")]
        public async Task TestCommand()
        {
            await RespondAsync("Shhhh!", ephemeral: true);
        }

        [Group("roles", "Manage the roles in #role-selection")]
        public class RolesCommand : InteractionModuleBase<SocketInteractionContext>
        {
            public RoleSelectionService RoleSelector { get; set; }
            
            [SlashCommand("load", "Loads a specific role")]
            public async Task LoadRoleCommand(string roleId)
            {
                RoleSetting roleSetting = RoleSelector.GetRoleSetting(roleId);
                SocketTextChannel channel = Context.Guild.GetTextChannel(555432146987122691);
                RestUserMessage message = (RestUserMessage)await channel.GetMessageAsync(roleSetting.ID);
                EmbedBuilder embed = message.Embeds.First().ToEmbedBuilder();
                if (embed.Fields.First().Name.Contains("There are no")) embed.Fields.Clear();
                SocketRole role = Context.Guild.GetRole(roleSetting.RoleID);
                if (embed.Fields.Select(x => x.Name).Contains(role.Name))
                {
                    await RespondAsync("Role already loaded.", ephemeral: true);
                    return;
                }
                embed.AddField(role.Name, $"Click the \"{roleSetting.Emote} {role.Name}\" button to toggle this role.");
                ComponentBuilder components = new();
                foreach (string field in embed.Fields.Select(x => x.Name))
                {
                    SocketRole role_ = Context.Guild.Roles.First(x => x.Name == field);
                    RoleSetting x = RoleSelector.GetRoleSetting(role_.Id);
                    components.WithButton(role_.Name, $"buttons:user:togglerole:{role_.Id}", emote: new Emoji(x.Emoji));
                }
                await message.ModifyAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = components.Build();
                });

                await RespondAsync($"Loaded.", ephemeral: true);
            }

            [SlashCommand("unload", "Unloads a specific role")]
            public async Task UnloadRoleCommand(string roleId)
            {
                RoleSetting roleSetting = RoleSelector.GetRoleSetting(roleId);
                SocketTextChannel channel = Context.Guild.GetTextChannel(555432146987122691);
                RestUserMessage message = (RestUserMessage)await channel.GetMessageAsync(roleSetting.ID);
                EmbedBuilder embed = message.Embeds.First().ToEmbedBuilder();
                SocketRole role = Context.Guild.GetRole(roleSetting.RoleID);
                if (embed.Fields.First().Name.Contains("There are no") || !embed.Fields.Select(x => x.Name).Contains(role.Name))
                {
                    await RespondAsync("Role is not loaded.", ephemeral: true);
                    return;
                }
                embed.Fields.RemoveAll(x => x.Name == role.Name);
                ComponentBuilder components = new();
                foreach (string field in embed.Fields.Select(x => x.Name))
                {
                    SocketRole role_ = Context.Guild.Roles.First(x => x.Name == field);
                    RoleSetting x = RoleSelector.GetRoleSetting(role_.Id);
                    components.WithButton(role_.Name, $"buttons:user:togglerole:{role_.Id}", emote: new Emoji(x.Emoji));
                }
                if (!embed.Fields.Any()) embed.AddField($"There are no {roleSetting.Group.ToLower()} roles available at the moment.", "Check back later for another event.");
                await message.ModifyAsync(x =>
                {
                    x.Embed = embed.Build();
                    x.Components = components.Build();
                });

                await RespondAsync($"Unloaded.", ephemeral: true);
            }
        }

        [SlashCommand("say", "Sends this message in another channel")]
        public async Task SayCommand(string message, SocketTextChannel channel)
        {
            await channel.SendMessageAsync(message);
            await RespondAsync("Message sent.", ephemeral: true);
        }
    }
}
