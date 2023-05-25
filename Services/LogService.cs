using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Quartz.Logging;

namespace Ja3farBot.Services
{
    public class LogService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        public LogService(DiscordSocketClient client, InteractionService interactions)
        {
            _client = client;
            _interactions = interactions;
        }
        
        public void Initialize()
        {
            _client.Log += (logMessage) => Log(logMessage.Source, logMessage.Message, logMessage.Severity);
            _interactions.Log += (logMessage) =>
            {
                if (logMessage.Source == "App Commands" && logMessage.Severity == LogSeverity.Error) return Log("App Commands", logMessage.Exception.ToString(), LogSeverity.Error);
                return Log(logMessage.Source, logMessage.Message, logMessage.Severity);
            };
        }

        public static void Critical(string Source, string Message)
            => Log(Source, Message, LogSeverity.Critical);
        public static void Error(string Source, string Message)
                    => Log(Source, Message, LogSeverity.Error);
        public static void Warning(string Source, string Message)
                    => Log(Source, Message, LogSeverity.Warning);
        public static void Info(string Source, string Message)
                    => Log(Source, Message, LogSeverity.Info);
        public static void Verbose(string Source, string Message)
                    => Log(Source, Message, LogSeverity.Verbose);
        public static void Debug(string Source, string Message)
                    => Log(Source, Message, LogSeverity.Debug);
        public static void Exception(string Source, Exception Exception)
                    => Log(Source, Exception.ToString(), LogSeverity.Error);


        public void InteractionError(SocketInteraction Interaction, IResult Result)
            => Log("App Commands", $"Interaction Error | {Interaction.Id} | {Interaction.User.Username}#{Interaction.User.Discriminator} | {Result.ErrorReason} ({Result.Error})", LogSeverity.Error);


        private static Task Log(string Source, string Message, LogSeverity Severity)
        {
            Console.ForegroundColor = Severity switch
            {
                LogSeverity.Critical => ConsoleColor.DarkRed,
                LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                LogSeverity.Info => ConsoleColor.White,
                LogSeverity.Verbose => ConsoleColor.Blue,
                LogSeverity.Debug => ConsoleColor.DarkBlue
            };
            Console.WriteLine($"[{DateTime.Now} @ {Source}] {Message}");
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }

    public class QuartzLogProvider : ILogProvider
    {
        public Logger GetLogger(string name)
            => (level, func, exception, parameters) =>
            {
                if (func != null) switch (level)
                    {
                        case LogLevel.Trace:
                            LogService.Debug("Quartz", func());
                            break;
                        case LogLevel.Debug:
                            LogService.Verbose("Quartz", func());
                            return true;
                        case LogLevel.Info:
                            LogService.Info("Quartz", func());
                            return true;
                        case LogLevel.Warn:
                            LogService.Warning("Quartz", func());
                            return true;
                        case LogLevel.Error:
                            LogService.Error("Quartz", func());
                            return true;
                        case LogLevel.Fatal:
                            LogService.Critical("Quartz", func());
                            return true;
                        default:
                            return false;
                    }
                return true;
            };

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }
    }
}
