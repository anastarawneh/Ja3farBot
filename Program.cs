using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Ja3farBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using static Ja3farBot.Services.ConfigService;

namespace Ja3farBot
{
    public class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        private readonly string _runningDir = Assembly.GetEntryAssembly().Location.Contains(@"C:\")
            ? AppDomain.CurrentDomain.BaseDirectory.Replace(@"bin\Debug\net6.0\", @"")
            : AppDomain.CurrentDomain.BaseDirectory.Replace("Ja3farBot/", "");
        private IServiceProvider _serviceProvider;
        private DiscordSocketClient _client;
        private InteractionService _interactionService;

        public async Task MainAsync(string[] args)
        {
            _client = new(new()
            {
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All,
                LogGatewayIntentWarnings = false,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 200
            });

            _interactionService = new(_client, new()
            {
                LogLevel = LogSeverity.Debug,
                UseCompiledLambda = true
            });

            _serviceProvider = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_interactionService)
                .AddSingleton<AutomodService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<CustomVCService>()
                .AddSingleton<EventHandlerService>()
                .AddSingleton<LogService>()
                .AddSingleton<MySqlService>()
                .AddSingleton<RoleSelectionService>()
                .AddSingleton<SuggestionService>()
                .BuildServiceProvider();

            _serviceProvider.GetRequiredService<ConfigService>().ReadConfig(Path.Combine(_runningDir, "config.yml"));
            
            _serviceProvider.GetRequiredService<AutomodService>().Initialize();
            _serviceProvider.GetRequiredService<CustomVCService>().Initialize();
            _serviceProvider.GetRequiredService<LogService>().Initialize();
            await _serviceProvider.GetRequiredService<MySqlService>().InitializeAsync();
            _serviceProvider.GetRequiredService<SuggestionService>().Initialize();

            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            await _client.LoginAsync(TokenType.Bot, Config.Token);
            await _client.StartAsync();

            await _serviceProvider.GetRequiredService<EventHandlerService>().InitializeAsync();

            await Task.Delay(-1);
        }
    }
}