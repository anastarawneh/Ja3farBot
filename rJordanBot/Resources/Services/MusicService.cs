using Discord;
using Discord.WebSocket;
using rJordanBot.Core.Methods;
using rJordanBot.Resources.Datatypes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Interfaces;
using Victoria.Responses.Rest;

namespace rJordanBot.Resources.Services
{
    public class MusicService
    {
        private LavaNode _lavaNode;
        private DiscordSocketClient _client;
        private LavaPlayer _player;
        private bool _alone = false;
        private bool _loop = false;
        private bool _qloop = false;

        public MusicService(LavaNode lavaNode, DiscordSocketClient client)
        {
            _lavaNode = lavaNode;
            _client = client;
        }

        public Task Initialize()
        {
            Program program = new Program();

            _client.Ready += ClientReadyAsync;
            _lavaNode.OnTrackEnded += OnTrackEnded;
            _client.UserVoiceStateUpdated += CilentVoiceStateChanged;
            _client.UserVoiceStateUpdated += BotMoved;
            return Task.CompletedTask;
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, SocketTextChannel textChannel)
        {
            await _lavaNode.JoinAsync(voiceChannel, textChannel);
            _player = _lavaNode.GetPlayer(voiceChannel.Guild);
        }

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
        {
            if (_player != null && _player.PlayerState == PlayerState.Playing) await _player.StopAsync();
            _player.Queue.Clear();
            await _lavaNode.LeaveAsync(voiceChannel);
            _loop = false;
            _qloop = false;
        }

        public async Task<string> PlayAsync(string query, IGuild guild, SocketGuildUser bot, SocketVoiceChannel vc, SocketTextChannel tc)
        {
            SearchResponse result;
            if (query.Contains("https://")) result = _lavaNode.SearchAsync(query).Result;
            else result = _lavaNode.SearchYouTubeAsync(query).Result;
            if (result.LoadStatus == LoadStatus.LoadFailed) return ":x: Search failed.";
            if (result.LoadStatus == LoadStatus.NoMatches) return ":x: No matches found.";
            PlaylistInfo playlist;

            LavaTrack track = result.Tracks.First();

            if (bot.VoiceChannel == null)
            {
                await ConnectAsync(vc, tc);
            }

            _player = _lavaNode.GetPlayer(guild);

            if (result.LoadStatus == LoadStatus.PlaylistLoaded)
            {
                playlist = result.Playlist;

                if (_player.PlayerState == PlayerState.Playing)
                {
                    foreach (LavaTrack resultTrack in result.Tracks)
                    {
                        _player.Queue.Enqueue(resultTrack);
                    }
                    return $":1234: Playlist `{playlist.Name}` loaded in queue.";
                }

                await _player.PlayAsync(track);
                foreach (LavaTrack resultTrack in result.Tracks)
                {
                    if (resultTrack != track) _player.Queue.Enqueue(resultTrack);
                }
                return $":1234: Playlist `{playlist.Name}` loaded in queue ({result.Tracks.Count()} tracks).\n" +
                        $":arrow_forward: Now playing: `{track.Title}`";
            }

            if (_player.PlayerState == PlayerState.Playing)
            {
                _player.Queue.Enqueue(track);
                return $":fast_forward: `{track.Title}` added to the queue in position {_player.Queue.Items.ToList().IndexOf(track) + 1}.";
            }

            await _player.PlayAsync(track);
            return $":arrow_forward: Now playing: `{track.Title}`";
        }

        public async Task<string> StopAsync()
        {
            if (_player is null || _player.Track is null) return ":x: Nothing to stop.";
            _player.Queue.Clear();
            await _player.StopAsync();
            _loop = false;
            _qloop = false;
            return ":stop_button: Stopped.";
        }

        public string Skip()
        {
            if (_player is null || _player.Track == null) return ":x: Nothing to skip.";
            LavaTrack skippedTrack = _player.Track;
            _loop = false;
            if (_player.Queue.Items.Count() == 0 && _player.Track != null)
            {
                _player.StopAsync();
                return ":stop_button: There are no more tracks in the queue.";
            }
            _player.SkipAsync();
            return $":track_next: Skipped `{skippedTrack.Title}`\n" +
                $":arrow_forward: Now playing: `{_player.Track.Title}`";
        }

        public async Task<string> PauseAsync()
        {
            if (_player.PlayerState != PlayerState.Paused)
            {
                await _player.PauseAsync();
                return ":pause_button: Paused player.";
            }
            await _player.ResumeAsync();
            return ":arrow_forward: Resumed player.";
        }

        public string Queue(int page, SocketUser user)
        {
            if (_player is null || _player.Queue.Equals(null) || (_player.Queue.Items.Count() == 0 && _player.Track == null))
            {
                return ":x: Queue is empty.";
            }

            if (_player.VoiceChannel.Name == "Private Office" && user.Id != Config.Owner) return ":x: The bot is in Anas' private office, so the queue is hidden.";

            string result = "```stylus\n";
            if (_qloop) result += "[QUEUE LOOPING]";
            foreach (IQueueable queueObject in _player.Queue.Items)
            {
                LavaTrack track = queueObject as LavaTrack;
                result += $"{_player.Queue.Items.ToList().IndexOf(queueObject) + 1}) {track.Title} -> {track.Duration.ToString(@"m\:ss")}\n";
            }
            if (_loop) result += $"\n0) [LOOPING] {_player.Track.Title} -> {(_player.Track.Duration - _player.Track.Position).ToString(@"m\:ss")} left\n```";
            else result += $"\n0) {_player.Track.Title} -> {(_player.Track.Duration - _player.Track.Position).ToString(@"m\:ss")} left\n```";

            if (result.Count() > 2000)
            {
                result = "```stylus\n";
                if (10 * page >= _player.Queue.Items.Count())
                {
                    for (int x = 10 * (page - 1); x < _player.Queue.Items.Count(); x++)
                    {
                        LavaTrack track = _player.Queue.Items.ElementAt(x) as LavaTrack;
                        result += $"{x + 1}) {track.Title} -> {track.Duration.ToString(@"m\:ss")}\n";
                    }
                }
                else for (int x = 10 * (page - 1); x < 10 * page; x++)
                    {
                        LavaTrack track = _player.Queue.Items.ElementAt(x) as LavaTrack;
                        result += $"{x + 1}) {track.Title} -> {track.Duration.ToString(@"m\:ss")}\n";
                    }

                if (_loop) result += $"\n0) [LOOPING] {_player.Track.Title} -> {(_player.Track.Duration - _player.Track.Position).ToString(@"m\:ss")} left\n";
                else result += $"\n0) {_player.Track.Title} -> {(_player.Track.Duration - _player.Track.Position).ToString(@"m\:ss")} left\n";

                result += $"\nPage {page}/{_player.Queue.Items.Count() / 10 + 1}```";
            }

            return result;
        }

        public string Loop()
        {
            if (_player is null || _player.Track == null) return ":x: Nothing to loop.";
            if (!_loop)
            {
                _loop = true;
                _qloop = false;
                return $":repeat: Looping track `{_player.Track.Title}`";
            }
            _loop = false;
            _qloop = false;
            return $":repeat: Stopped looping track `{_player.Track.Title}`";
        }

        public async Task<string> SeekAsync(string durationS)
        {
            TimeSpan timeSpan;
            /*int durationI;

            switch (durationS[^1])
            {
                case 'm':
                    durationI = int.Parse(durationS.Replace("m", ""));
                    timeSpan = new TimeSpan(0, durationI, 0);
                    break;
                case 's':
                    durationI = int.Parse(durationS.Replace("s", ""));
                    timeSpan = new TimeSpan(0, 0, durationI);
                    break;
                default:
                   return ":x: Invalid format. `^seek <_s> OR ^seek <_m>`";
            }*/
            string[] time = durationS.Split(':');
            timeSpan = new TimeSpan(0, int.Parse(time[0]), int.Parse(time[1]));

            if (timeSpan >= _player.Track.Duration) return $":x: Position must be within the track's length ({_player.Track.Duration.ToString(@"m\:ss")}).";

            await _player.SeekAsync(timeSpan);
            return $":left_right_arrow: Seeking into {timeSpan.ToString(@"m\:ss")}.";
        }

        public string Remove(int index)
        {
            if (_player is null || _player.Queue.Equals(null) || (_player.Queue.Items.Count() == 0 && _player.Track == null))
            {
                return ":x: Queue is empty.";
            }
            LavaTrack track = _player.Queue.Items.ElementAt(index - 1) as LavaTrack;
            _player.Queue.RemoveAt(index - 1);
            return $":hash: Removed track `{track.Title}`";
        }

        public string qLoop()
        {
            if (_player is null || _player.Track == null) return ":x: Nothing to loop.";
            if (!_qloop)
            {
                _loop = false;
                _qloop = true;
                return $":repeat: Looping queue.";
            }
            _loop = false;
            _qloop = false;
            return $":repeat: Stopped looping queue.";
        }

        public string Shuffle()
        {
            if (_player is null || _player.Queue.Equals(null) || (_player.Queue.Items.Count() == 0 && _player.Track == null))
            {
                return ":x: Nothing to shuffle.";
            }

            _player.Queue.Shuffle();

            return ":arrows_counterclockwise: Shuffled the queue.";
        }


        private async Task ClientReadyAsync()
        {
            await _lavaNode.ConnectAsync();

            _client.Ready -= ClientReadyAsync;
        }

        private async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            TrackEndReason reason = args.Reason;
            LavaPlayer player = args.Player;
            LavaTrack track = args.Track;

            if (!reason.ShouldPlayNext()) return;
            if (_loop)
            {
                await player.PlayAsync(track);
                await player.TextChannel.SendMessageAsync($":arrow_forward: Now playing: `{track.Title}`");
                return;
            }
            if (!player.Queue.TryDequeue(out IQueueable queueObject) || !(queueObject is LavaTrack nextTrack))
            {
                await player.TextChannel.SendMessageAsync(":x: There are no more tracks in the queue.");
                return;
            }
            if (_qloop)
            {
                await player.PlayAsync(nextTrack);
                player.Queue.Enqueue(track);
                await player.TextChannel.SendMessageAsync($":arrow_forward: Now playing: `{nextTrack.Title}`");
                return;
            }

            await player.PlayAsync(nextTrack);
            await player.TextChannel.SendMessageAsync($":arrow_forward: Now playing: `{nextTrack.Title}`");
        }

        private async Task CilentVoiceStateChanged(SocketUser user, SocketVoiceState preState, SocketVoiceState postState)
        {
            if (user.IsBot) return;
            if (_player is null) return;
            int preType = -1; // 1 for bot, 2 for not bot, 0 for null
            int postType = -1;

            if (preState.VoiceChannel is null) preType = 0;
            else if (preState.VoiceChannel.Users.Contains(Constants.IConversions.GuildUser(_client))) preType = 1;
            else preType = 2;

            if (postState.VoiceChannel is null) postType = 0;
            else if (postState.VoiceChannel.Users.Contains(Constants.IConversions.GuildUser(_client))) postType = 1;
            else postType = 2;

            if (preType == 1 && _player.PlayerState == PlayerState.Paused) return;
            if ((preType == 0 && postType == 0) || (preType == 2 && postType == 2) || (preType == 0 && postType == 2) || (preType == 2 && postType == 0)) return;
            if (preType == 1 && preState.VoiceChannel.Users.Count != 1) return;
            if (postType == 1 && postState.VoiceChannel.Users.Count != 2) return;

            if ((preType == 1 && postType == 0) || (preType == 1 && postType == 2))
            {
                Console.WriteLine($"Pause, from {preType} to {postType}");
                await PauseAsync();
                await _player.TextChannel.SendMessageAsync(":pause_button: Paused playback; the voice channel is empty.");
                _alone = true;
            }

            if ((preType == 0 && postType == 1) || (preType == 2 && postType == 1))
            {
                Console.WriteLine($"Resume, from {preType} to {postType}");
                await PauseAsync();
                await _player.TextChannel.SendMessageAsync(":arrow_forward: Resumed playback.");
                _alone = false;
            }

            /*if (preState.VoiceChannel == null && postState.VoiceChannel == null) return;
            if (preState.VoiceChannel != null &&
                !preState.VoiceChannel.Users.Contains(Constants.IConversions.GuildUser(_client)) &&
                postState.VoiceChannel != null &&
                !postState.VoiceChannel.Users.Contains(Constants.IConversions.GuildUser(_client))) return;
            
            if (preState.VoiceChannel != null && preState.VoiceChannel.Users.Contains(user) && ((!postState.VoiceChannel.Users.Contains(user) && postState.VoiceChannel.Users.Count == 1 &&) || postState.VoiceChannel == null) && !_player.IsPaused)
            {
                await PauseAsync();
                await _player.TextChannel.SendMessageAsync(":pause_button: Paused playback; the voice channel is empty.");
            }
            if (postState.VoiceChannel != null && postState.VoiceChannel.Users.Contains(user) && (!preState.VoiceChannel.Users.Contains(user) && preState.VoiceChannel.Users.Count == 1) || preState.VoiceChannel == null)
            {
                await PauseAsync();
                await _player.TextChannel.SendMessageAsync(":arrow_forward: Resumed playback.");
            }*/
        }

        private async Task BotMoved(SocketUser user, SocketVoiceState preState, SocketVoiceState postState)
        {
            SocketGuildUser bot = _client.Guilds.First().CurrentUser;
            if (user != bot) return;
            if (_player == null) return;
            if (_player.PlayerState != PlayerState.Paused) return;
            if (!_alone) return;
            if (postState.VoiceChannel.Users.Count() == 1) return;

            await PauseAsync();
            await _player.TextChannel.SendMessageAsync(":arrow_forward: Resumed playback.");
            _alone = false;
        }
    }
}
