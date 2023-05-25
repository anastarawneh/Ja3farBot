using Discord;
using Discord.WebSocket;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System.Text.Json.Nodes;

namespace Ja3farBot.Services
{
    public class SchedulerService
    {
        private static DiscordSocketClient _client;
        public SchedulerService(DiscordSocketClient client)
        {
            _client = client;
        }
        private static IScheduler _scheduler;

        public async Task InitializeAsync()
        {
            _client.Ready += async () => await _scheduler.Start();

            LogProvider.SetCurrentLogProvider(new QuartzLogProvider());
            _scheduler = await SchedulerBuilder.Create()
                .WithName("Ja3farBotScheduler")
                .BuildScheduler();
        }
    }
}
