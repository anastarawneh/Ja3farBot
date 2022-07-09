using Dapper;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using static Ja3farBot.Util.MySqlDatatypes;

namespace Ja3farBot.Services
{
    public class CustomVCService
    {
        private readonly DiscordSocketClient _client;
        public CustomVCService(DiscordSocketClient client)
        {
            _client = client;
        }

        public void Initialize()
        {
            _client.UserVoiceStateUpdated += async (user, state1, state2) =>
            {
                if (state1.VoiceChannel == null || !await IsCustomVC(state1.VoiceChannel)) return;
                SocketVoiceChannel voice = state1.VoiceChannel;
                if (voice.Users.Count != 0) return;

                CustomVC vc = await GetCustomVC(voice);
                await Unload(vc);
            };
        }


        public async Task<string> Load(CustomVC vc)
        {
            if (await IsActive(vc)) return "Your Custom VC is already loaded.";
            using MySqlConnection connection = MySqlService.GetConnection();
            SocketUser user = _client.GetUser(vc.UserID);
            RestVoiceChannel voice = await _client.Guilds.First().CreateVoiceChannelAsync($"{user.Username}'s VC",
                x =>
                {
                    x.CategoryId = 550848069160271905;
                    if (vc.Slots != 0) x.UserLimit = vc.Slots;
                    else x.UserLimit = null;
                    x.Bitrate = vc.Bitrate;
                });
            await connection.ExecuteAsync("UPDATE customvcs SET channelid=@channelid WHERE userid=@userid", new { channelid = voice.Id, userid = vc.UserID });
            OverwritePermissions perms = new(
                muteMembers: PermValue.Allow, 
                deafenMembers: PermValue.Allow, 
                moveMembers: PermValue.Allow);
            await voice.AddPermissionOverwriteAsync(user, perms);
            return "Loaded your Custom VC.";
        }

        public async Task<string> Unload(CustomVC vc)
        {
            if (!await IsActive(vc)) return "Custom VC is not loaded.";
            await ((SocketVoiceChannel)await _client.GetChannelAsync(vc.ChannelID)).DeleteAsync();
            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("UPDATE customvcs SET channelid=0 WHERE userid=@userid", new { userid = vc.UserID });
            return "Unloaded Custom VC.";
        }


        public async Task<CustomVC> CreateCustomVC(SocketUser user, int slots = 5, int bitrate = 64000)
        {
            if (await HasCustomVC(user)) return null;
            CustomVC vc = new(user.Id, slots, bitrate);
            await Insert(vc);
            return vc;
        }

        private async Task<CustomVC> GetCustomVC(SocketUser user)
        {
            if (!await HasCustomVC(user)) return null;
            using MySqlConnection connection = MySqlService.GetConnection();
            return await connection.QueryFirstAsync<CustomVC>("SELECT * FROM customvcs WHERE userid=@userid", new { userid = user.Id });
        }
        private async Task<CustomVC> GetCustomVC(SocketVoiceChannel voice)
        {
            if (!await IsCustomVC(voice)) return null;
            using MySqlConnection connection = MySqlService.GetConnection();
            return await connection.QueryFirstAsync<CustomVC>("SELECT * FROM customvcs WHERE channelid=@channelid", new { channelid = voice.Id });
        }

        public async Task<CustomVC> GetOrCreateCustomVC(SocketUser user)
        {
            if (await HasCustomVC(user)) return await GetCustomVC(user);
            return await CreateCustomVC(user);
        }

        public async Task<bool> HasCustomVC(SocketUser user)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            return await connection.ExecuteScalarAsync<bool>("SELECT COUNT(1) FROM customvcs WHERE userid=@userid", new { userid = user.Id });
        }
        
        private async Task Insert(CustomVC vc)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            await connection.ExecuteAsync("INSERT INTO customvcs (userid, channelid, slots, bitrate) VALUES (@userid, @channelid, @slots, @bitrate)", new { userid = vc.UserID, channelid = vc.ChannelID, slots = vc.Slots, bitrate = vc.Bitrate });
        }
        
        private async Task<bool> IsActive(CustomVC vc)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            return await connection.ExecuteScalarAsync<bool>("SELECT channelid FROM customvcs WHERE userid=@userid", new { userid = vc.UserID });
        }

        public async Task<bool> IsCustomVC(SocketVoiceChannel voiceChannel)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            return await connection.ExecuteScalarAsync<bool>("SELECT COUNT(1) FROM customvcs WHERE channelid=@channelid", new { channelid = voiceChannel.Id });
        }

        public async Task<string> ModifyCustomVC(CustomVC vc, int slots, int bitrate)
        {
            if (!await HasCustomVC(_client.GetUser(vc.UserID))) return "You do not currently have a custom Voice Channel assigned. Use `/customvc create` to create one.";
            
            if (await IsActive(vc)) await _client.Guilds.First().GetVoiceChannel(vc.ChannelID).ModifyAsync(x =>
            {
                if (slots != -1) x.UserLimit = slots;
                if (bitrate != -1) x.Bitrate = bitrate;
            });

            using MySqlConnection connection = MySqlService.GetConnection();
            string query = "UPDATE customvcs SET ";
            if (slots != -1 && bitrate != -1) query += "slots=@slots, bitrate=@bitrate";
            else if (slots != -1) query += "slots=@slots";
            else if (bitrate != -1) query += "bitrate=@bitrate";
            await connection.ExecuteAsync(query + " WHERE userid=@userid", new { slots, bitrate, userid = vc.UserID });
            return "Custom Voice Channel modified.";
        }
    }
}
