using Dapper;
using Discord;
using Discord.WebSocket;
using Ja3farBot.Services;
using MySql.Data.MySqlClient;
using static Ja3farBot.Util.Datatypes;
using static Ja3farBot.Util.MySqlDatatypes;

namespace Ja3farBot.Util
{
    public static class Extensions
    {
        public static string CapitalizeFirstLetter(this string Input)
            => char.ToUpper(Input[0]) + Input[1..];

        public static string GetRGB(this Color color)
            => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        public static bool IsTimedOut(this SocketGuildUser user)
            => user.TimedOutUntil.HasValue && user.TimedOutUntil.Value > DateTimeOffset.Now;
    }

    public static class MySqlExtensions
    {
        public static IEnumerable<Warning> GetWarnings(this SocketGuildUser user)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            return connection.Query<Warning>("SELECT * FROM warnings WHERE userid=@userid", new { userid = user.Id });
        }

        public static async Task<bool> AddWarningAsync(this SocketGuildUser user, IUserMessage response, string reason, ulong modID)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            int result = await connection.ExecuteAsync("INSERT INTO warnings (userid, channelid, messageid, timestamp, reason, modid) VALUES (@userid, @channelid, @messageid, @timestamp, '@reason', @modid)", new { userid = user.Id, channelid = response.Channel.Id, messageid = response.Id, timestamp = response.Timestamp.ToUnixTimeMilliseconds(), reason, modid = modID });
            return result == 1;
        }


        public static async Task<bool> HasSocialsAsync(this SocketGuildUser user)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            return await connection.ExecuteScalarAsync<bool>("SELECT COUNT(1) FROM socials WHERE userid=@userid", new { userid = user.Id });
        }

        public static async Task SetSocialsAsync(this SocketGuildUser user, string twitter, string instagram, string snapchat)
        {
            using MySqlConnection connection = MySqlService.GetConnection();
            if (!await user.HasSocialsAsync()) await connection.ExecuteAsync("INSERT INTO socials (userid, twitter, instagram, snapchat, messageid) VALUES (@userid, 'None', 'None', 'None', 0)", new { userid = user.Id });
            Socials socials = await connection.QueryFirstAsync<Socials>("SELECT * FROM socials WHERE userid=@userid", new { userid = user.Id });
            socials.Twitter = twitter ?? socials.Twitter;
            socials.Instagram = instagram ?? socials.Instagram;
            socials.Snapchat = snapchat ?? socials.Snapchat;
            await connection.ExecuteAsync("UPDATE socials SET twitter='@twitter', instagram='@instagram', snapchat='@snapchat' WHERE userid=@userid", new { twitter = socials.Twitter, instagram = socials.Instagram, snapchat = socials.Snapchat, userid = user.Id });

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(114, 137, 218)
                .WithAuthor(user)
                .AddField("Twitter", socials.Twitter != "None" ? $"[@{socials.Twitter}](https://twitter.com/{socials.Twitter})" : "None")
                .AddField("Instagram", socials.Instagram != "None" ? $"[@{socials.Instagram}](https://instagram.com/{socials.Instagram})" : "None")
                .AddField("Snapchat", socials.Snapchat != "None" ? socials.Snapchat : "None");

            SocketTextChannel socialsChannel = user.Guild.GetTextChannel(596700865398833182);
            if (socials.MessageID != 0) await ((IUserMessage)await socialsChannel.GetMessageAsync(socials.MessageID)).ModifyAsync(x => x.Embed = embed.Build());
            else
            {
                IUserMessage message = await socialsChannel.SendMessageAsync(embed: embed.Build());
                await connection.ExecuteAsync("UPDATE socials SET messageid=@messageid WHERE userid=@userid", new { messageid = message.Id, userid = user.Id });
            }
        }
    }
}
