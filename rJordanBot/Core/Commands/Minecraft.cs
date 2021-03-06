using Discord.Addons.Interactive;
using Discord.Commands;
using MulticraftLib;
using rJordanBot.Core.Preconditions;
using System.Linq;
using System.Threading.Tasks;

namespace rJordanBot.Core.Commands
{
    public class Minecraft : InteractiveBase
    {
        [Command("list")]
        [MinecraftCommand]
        public async Task List()
        {
            getServerStatus.apiData.apiPlayer[] players = Multicraft.GetServerStatus().Data.Players;
            if (players.Count() == 0) await ReplyAsync("There are 0 players online.");
            else if (players.Count() == 1) await ReplyAsync($"There is 1 player online:\n- {players.First().Name}");
            else
            {
                string response = $"There are {players.Count()} players online:";
                foreach (getServerStatus.apiData.apiPlayer player in players) response += $"\n- {player.Name}";
                await ReplyAsync(response);
            }
        }

        [Command("start")]
        [RequireOwner]
        [MinecraftCommand]
        public async Task Start()
        {
            string status = Multicraft.GetServerStatus().Data.Status;
            if (status == "online")
            {
                await ReplyAsync(":x: Server is already online.");
                return;
            }
            await ReplyAsync(await Multicraft.StartServer());
        }
    }
}
