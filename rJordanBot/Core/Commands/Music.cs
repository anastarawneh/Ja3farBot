using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using rJordanBot.Core.Methods;
using rJordanBot.Core.Preconditions;
using rJordanBot.Resources.Services;
using System.Threading.Tasks;

namespace rJordanBot.Core.Commands
{
    public class Music : InteractiveBase<SocketCommandContext>
    {
        private MusicService _musicService;

        public Music(MusicService musicService)
        {
            _musicService = musicService;
        }

        [Command("join")]
        [RequireBotChannel]
        public async Task Join()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Constants.IConversions.GuildUser(Context);
            if (bot.VoiceChannel != null)
            {
                await ReplyAsync($":x: Already connected to {bot.VoiceChannel.Name}.");
                return;
            }

            await ReplyAsync($":ok: Connected to {user.VoiceChannel.Name}.");
            await _musicService.ConnectAsync(user.VoiceChannel, Context.Channel as SocketTextChannel);
        }

        [Command("leave"), Alias("dc")]
        [RequireBotChannel]
        public async Task Leave()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Constants.IConversions.GuildUser(Context);
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            await ReplyAsync($":eject: Disconnected from {bot.VoiceChannel}.");
            await _musicService.LeaveAsync(bot.VoiceChannel);
        }

        [Command("play"), Alias("p")]
        [RequireBotChannel]
        public async Task Play([Remainder] string query = null)
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (query == null)
            {
                await ReplyAsync(":x: Please specify the search query for the track to be played.");
                return;
            }
            SocketGuildUser bot = Constants.IConversions.GuildUser(Context);
            if (bot.VoiceChannel != null && user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            string result = _musicService.PlayAsync(query, Context.Guild, bot, user.VoiceChannel, Context.Channel as SocketTextChannel).Result;
            await ReplyAsync(result);
        }

        [Command("stop")]
        [RequireBotChannel]
        public async Task Stop()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Constants.IConversions.GuildUser(Context);
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            string result = await _musicService.StopAsync();
            await ReplyAsync(result);
        }

        [Command("skip")]
        [RequireBotChannel]
        public async Task Skip()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Constants.IConversions.GuildUser(Context);
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            string result = _musicService.Skip();
            await ReplyAsync(result);
        }

        [Command("pause")]
        [RequireBotChannel]
        public async Task Pause()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Constants.IConversions.GuildUser(Context);
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            string result = _musicService.PauseAsync().Result;
            await ReplyAsync(result);
        }

        [Command("queue"), Alias("q")]
        [RequireBotChannel]
        public async Task Queue(int page = 1)
        {
            if (Context.Channel is IDMChannel) return;
            await ReplyAsync(_musicService.Queue(page, Context.User));
        }

        [Command("loop"), Alias("l")]
        [RequireBotChannel]
        public async Task Loop()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Context.Guild.CurrentUser;
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            await ReplyAsync(_musicService.Loop());
        }

        [Command("seek")]
        [RequireBotChannel]
        public async Task Seek(string duration = null)
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Context.Guild.CurrentUser;
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            if (duration == null)
            {
                await ReplyAsync(":x: Please specify a position. `m:s`");
                return;
            }

            await ReplyAsync(_musicService.SeekAsync(duration).Result);
        }

        [Command("remove")]
        [RequireBotChannel]
        public async Task Remove(int index = 0)
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Context.Guild.CurrentUser;
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            if (index == 0)
            {
                await ReplyAsync(":x: Please enter the track's position in the queue.");
            }

            await ReplyAsync(_musicService.Remove(index));
        }

        [Command("qloop")]
        [RequireBotChannel]
        public async Task qLoop()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Context.Guild.CurrentUser;
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            await ReplyAsync(_musicService.qLoop());
        }

        [Command("shuffle")]
        [RequireBotChannel]
        public async Task Shuffle()
        {
            if (Context.Channel is IDMChannel) return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            if (user.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            SocketGuildUser bot = Context.Guild.CurrentUser;
            if (bot.VoiceChannel == null)
            {
                await ReplyAsync(":x: Not connected to a voice channel.");
                return;
            }
            if (user.VoiceChannel != bot.VoiceChannel)
            {
                await ReplyAsync(":x: Not connected to the same voice channel.");
                return;
            }

            await ReplyAsync(_musicService.Shuffle());
        }
    }
}
