using Discord;
using System.Dynamic;
using YamlDotNet.Serialization;

namespace Ja3farBot.Services
{
    public class ConfigService
    {
        private static dynamic _config;
        private string _configPath;

        public void ReadConfig(string ConfigFilePath)
        {
            _configPath = ConfigFilePath;
            try
            {
                string YML = File.ReadAllText(ConfigFilePath);
                IDeserializer deserializer = new DeserializerBuilder().Build();
                _config = deserializer.Deserialize<ExpandoObject>(YML);
            }
            catch (Exception ex)
            {
                LogService.Critical("LogService", ex.Message);
            }
        }

        public string GetFilePath(string FileName)
            => _configPath.Replace("config.yml", FileName);

        public static class Config
        {
            public static string Token { get { return _config.token; } }
            public static string EventsEnabled { get { return _config.events_enabled; } }
            public static int StarboardMinimum { get { return int.Parse(_config.starboard_minimum); } }
            
            public class Announcement
            {
                private static Dictionary<object, object> Raw { get { return _config.announcement; } }
                public static string Title { get { return Raw.GetValueOrDefault("title").ToString(); } }
                public static string Description { get { return Raw.GetValueOrDefault("description").ToString(); } }
                public static List<EmbedFieldBuilder> Fields { get {
                        List<EmbedFieldBuilder> fields = new();
                        foreach (Dictionary<object, object> dict in (List<object>)Raw.GetValueOrDefault("fields"))
                            fields.Add(new EmbedFieldBuilder().WithName(dict.First(x => x.Key.ToString() == "title").Value.ToString()).WithValue(dict.First(x => x.Key.ToString() == "content").Value.ToString()));
                        return fields;
                    } }
            }

            public static List<string> BannedWords { get { return ((List<object>)_config.banned_words).Select(x => x.ToString()).ToList(); } }
            public static int MaxMentions { get { return int.Parse(_config.max_mentions); } }

            public class MySql
            {
                public static string Server { get { return _config.mysql_server; } }
                public static string Username { get { return _config.mysql_username; } }
                public static string Password { get { return _config.mysql_password; } }
                public static string Database { get { return _config.mysql_database; } }
            }
        }
    }
}
