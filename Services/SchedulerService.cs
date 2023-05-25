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

            await _scheduler.ScheduleJob(JobBuilder.Create<WeatherJob>().WithIdentity("ForecastRefresh", "Weather").Build(), TriggerBuilder.Create()
                .WithCronSchedule("0 0 */1 * * ?")
                .StartNow()
                .Build());
        }

        public class WeatherJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                string URL = "https://api.open-meteo.com/v1/forecast";
                string response_text;
                JsonNode json;
                using (HttpClient client = new())
                {
                    client.BaseAddress = new(URL);
                    HttpResponseMessage response = await client.GetAsync(
                        "?latitude=31.96" +
                        "&longitude=35.95" +
                        "&timezone=Asia/Amman" +
                        "&current_weather=true" +
                        "&daily=temperature_2m_max,temperature_2m_min" +
                        "&forecast_days=8");
                    response.EnsureSuccessStatusCode();
                    response_text = await response.Content.ReadAsStringAsync();
                    json = JsonNode.Parse(response_text)!;
                }

                string condition = (int)json["current_weather"]["weathercode"] switch
                {
                    0 => "Clear",
                    1 => "Mostly Clear",
                    2 => "Partly Cloudy",
                    3 => "Overcast",
                    45 or 48 => "Fog",
                    51 or 53 or 55 or 56 or 57 => "Drizzle",
                    61 or 63 or 65 or 66 or 67 => "Rain",
                    71 or 73 or 75 or 77 => "Snow",
                    80 or 81 or 82 => "Rain Showers",
                    85 or 86 => "Snow Showers",
                    95 or 96 or 99 => "Thunderstorm",
                    _ => $"Unknown weather code {json["current_weather"]["weathercode"]}",
                };

                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Weather Forecast (Amman)")
                    .WithColor(66, 134, 244)
                    .WithDescription($@"
Temperature: {json["current_weather"]!["temperature"]}°C
High: {json["daily"]["temperature_2m_max"][0]}°C
Low: {json["daily"]["temperature_2m_min"][0]}°C
Condition: {condition}

{DateTime.Parse(json["daily"]["time"][1].ToString()):ddd d/M/yyyy}: {json["daily"]["temperature_2m_min"][1]}-{json["daily"]["temperature_2m_max"][1]}°C
{DateTime.Parse(json["daily"]["time"][2].ToString()):ddd d/M/yyyy}: {json["daily"]["temperature_2m_min"][2]}-{json["daily"]["temperature_2m_max"][2]}°C
{DateTime.Parse(json["daily"]["time"][3].ToString()):ddd d/M/yyyy}: {json["daily"]["temperature_2m_min"][3]}-{json["daily"]["temperature_2m_max"][3]}°C
{DateTime.Parse(json["daily"]["time"][4].ToString()):ddd d/M/yyyy}: {json["daily"]["temperature_2m_min"][4]}-{json["daily"]["temperature_2m_max"][4]}°C
{DateTime.Parse(json["daily"]["time"][5].ToString()):ddd d/M/yyyy}: {json["daily"]["temperature_2m_min"][5]}-{json["daily"]["temperature_2m_max"][5]}°C
{DateTime.Parse(json["daily"]["time"][6].ToString()):ddd d/M/yyyy}: {json["daily"]["temperature_2m_min"][6]}-{json["daily"]["temperature_2m_max"][6]}°C
{DateTime.Parse(json["daily"]["time"][7].ToString()):ddd d/M/yyyy}: {json["daily"]["temperature_2m_min"][7]}-{json["daily"]["temperature_2m_max"][7]}°C

Last Updated: {TimestampTag.FromDateTime(DateTime.Parse(json["current_weather"]!["time"].ToString()), TimestampTagStyles.ShortDateTime)}"
                    ).WithCurrentTimestamp();

                await ((SocketTextChannel)await _client.GetChannelAsync(1111049984259923988)).ModifyMessageAsync(1111051030898155661, message =>
                {
                    message.Embed = embed.Build();
                });
            }
        }
    }
}
