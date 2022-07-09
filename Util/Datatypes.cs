using Dapper;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Ja3farBot.Services;
using MySql.Data.MySqlClient;
using static Ja3farBot.Services.ConfigService;
using static Ja3farBot.Util.MySqlDatatypes;

namespace Ja3farBot.Util
{
    public class Datatypes
    {
        public class Socials
        {
            public ulong UserID { get; set; }
            public ulong MessageID { get; set; }
            public string Twitter { get; set; }
            public string Instagram { get; set; }
            public string Snapchat { get; set; }
        }

        public class StarboardMessage
        {
            public IUserMessage Message { get; set; }
            public SocketTextChannel Channel { get; set; }
            public IUser Author { get; set; }
            public int Stars { get; set; }
            public ulong StarboardID { get; set; }

            public async Task UpdateAsync()
            {
                SocketGuild guild = Channel.Guild;
                SocketTextChannel starboardChannel = guild.GetTextChannel(672840486502793266);
                if (!await HasStarboardAsync() && Stars >= Config.StarboardMinimum)
                {
                    Starboard starboard = new()
                    {
                        MessageID = Message.Id,
                        ChannelID = Channel.Id,
                        UserID = Author.Id
                    };
                    string link = (await Channel.GetMessageAsync(Message.Id)).GetJumpUrl();
                    EmbedBuilder embed = new EmbedBuilder()
                        .WithAuthor(Author)
                        .WithColor(Color.Gold)
                        .WithDescription($"[Link to original message]({link})");
                    if (Message.Content != null || Message.Content != "") embed.AddField("Content", Message.Content);
                    embed.AddField("Channel", Channel.Mention);
                    if (Message.Attachments.Count == 1) embed.WithImageUrl(Message.Attachments.First().Url);
                    RestUserMessage message = await starboardChannel.SendMessageAsync($"{Stars} :star2:", embed: embed.Build());
                    StarboardID = message.Id;
                    await SaveAsync();
                }
                else if (await HasStarboardAsync() && Stars >= Config.StarboardMinimum)
                {
                    Starboard starboard = await GetStarboardAsync();
                    IUserMessage message = (IUserMessage)await starboardChannel.GetMessageAsync(starboard.StarboardMessageID);
                    await message.ModifyAsync(x => x.Content = $"{Stars} :star2:");
                }
                else if (await HasStarboardAsync() && Stars < Config.StarboardMinimum)
                {
                    Starboard starboard = await GetStarboardAsync();
                    IUserMessage message = (IUserMessage)await starboardChannel.GetMessageAsync(starboard.StarboardMessageID);
                    await message.DeleteAsync();
                    await SaveAsync();
                }
            }
            public async Task SaveAsync()
            {
                using MySqlConnection connection = MySqlService.GetConnection();
                if (Stars >= Config.StarboardMinimum && !await HasStarboardAsync()) await connection.ExecuteAsync($"INSERT INTO starboards (messageid, channelid, userid, starboardmessageid) VALUES ({Message.Id}, {Channel.Id}, {Author.Id}, {StarboardID})");
                else await connection.ExecuteAsync("DELETE FROM starboards WHERE messageid=@messageid", new { messageid = Message.Id });
            }

            public async Task<bool> HasStarboardAsync()
            {
                using MySqlConnection connection = MySqlService.GetConnection();
                return await connection.ExecuteScalarAsync<bool>("SELECT COUNT(1) FROM starboards WHERE messageid=@messageid", new { messageid = Message.Id });
            }

            public async Task<Starboard> GetStarboardAsync()
            {
                using MySqlConnection connection = MySqlService.GetConnection();
                return await connection.QueryFirstAsync<Starboard>("SELECT * FROM starboards WHERE messageid=@messageid", new { messageid = Message.Id });
            }
        }

        public class Suggestion
        {
            public IUserMessage Message { get; set; }
            public int Number { get; set; }
            public string Author { get; set; }
            public string Text { get; set; }
            public SuggestionState State { get; set; }
            public string Reason { get; set; }
            public string Moderator { get; set; }
        }
        public enum SuggestionState
        {
            Normal = 0,
            Approved = 1,
            Denied = -1,
            Implemented = 2,
            Considered = 3
        }
    }
    
    public class FileDatatypes
    {
        public class RoleSetting
        {
            public ulong ID { get; set; }
            public ulong RoleID { get; set; }
            public string Emote { get; set; }
            public string Group { get; set; }
            public string Emoji { get; set; }
        }
    }
    
    public class MySqlDatatypes
    {
        public class CustomVC
        {
            public ulong UserID { get; set; }
            public ulong ChannelID { get; set; }
            public int Slots { get; set; }
            public int Bitrate { get; set; }

            public CustomVC() { }
            public CustomVC(ulong UserID, int Slots, int Bitrate)
            {
                this.UserID = UserID;
                ChannelID = 0;
                this.Slots = Slots;
                this.Bitrate = Bitrate;
            }
        }

        public class Starboard
        {
            public ulong MessageID { get; set; }
            public ulong ChannelID { get; set; }
            public ulong UserID { get; set; }
            public ulong StarboardMessageID { get; set; }
        }

        public class Warning
        {
            public ulong WarningID { get; set; }
            public ulong UserID { get; set; }
            public ulong ChannelID { get; set; }
            public ulong MessageID { get; set; }
            public long Timestamp { get; set; }
            public string Reason { get; set; }
            public ulong ModID { get; set; }
        }
    }
}
