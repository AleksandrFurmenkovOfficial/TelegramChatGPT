using NLog;
using NLog.Config;

namespace TelegramChatGPT
{
    internal static class AppLogger
    {
        private static readonly Logger Logger = GetLogger();

        private static Logger GetLogger()
        {
            LogManager.Configuration = new XmlLoggingConfiguration($"{AppContext.BaseDirectory}/Logging/nlog.config");
            return LogManager.GetCurrentClassLogger();
        }

        public static void LogDebugMessage(string message)
        {
            Logger.Debug(message);
        }

        public static void LogInfoMessage(string message)
        {
            Logger.Info(message);
        }

        public static void LogException(Exception e)
        {
            Logger.Error(e);
        }

        public static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((Exception)e.ExceptionObject);
        }
    }
}