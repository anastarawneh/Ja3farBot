using MySql.Data.MySqlClient;
using static Ja3farBot.Services.ConfigService;

namespace Ja3farBot.Services
{
    public class MySqlService
    {
        private readonly string _server = Config.MySql.Server;
        private readonly string _username = Config.MySql.Username;
        private readonly string _password = Config.MySql.Password;
        private readonly string _database = Config.MySql.Database;
        private static MySqlConnection connection;
        
        public async Task InitializeAsync()
        {
            MySqlConnectionStringBuilder builder = new()
            {
                Server = _server,
                UserID = _username,
                Password = _password,
                Database = _database
            };
            connection = new MySqlConnection(builder.GetConnectionString(true));
            await connection.OpenAsync();
            if (!connection.Ping()) LogService.Critical("MySqlService", "MySql server connection failed");
            await connection.CloseAsync();
            LogService.Info("MySqlService", "Connected to MySql server");
        }

        public static MySqlConnection GetConnection() => connection;
    }
}
