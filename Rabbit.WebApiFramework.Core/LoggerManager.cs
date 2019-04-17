using System;
using NLog;

namespace Rabbit.WebApiFramework.Core
{
    public class LoggerManager
    {
        public ILogger Logger { get; set; }

        public void Info(string message, bool useConsole = false)
        {
            Logger.Info(message);
            ConsoleWitreLine(" Info:" + message, "Info", useConsole);
        }

        public void Warning(string message, bool useConsole = false)
        {
            Logger.Warn(message);
            ConsoleWitreLine(" Exception:" + message, "Warning", useConsole);
        }

        public void Warning(Exception ex, object oj = null, bool useConsole = false)
        {
            Logger.Warn(ex, ex.Message, oj);
            ConsoleWitreLine(" Exception:" + ex.Message, "Warning", useConsole);
        }

        public void Error(string message, Exception ex, object oj = null, bool useConsole = false)
        {
            Logger.Error(ex, message, oj);
            ConsoleWitreLine(message + " Exception:" + ex.Message, "Error", useConsole);
        }

        public void Error(Exception ex, object oj = null, bool useConsole = false)
        {
            Logger.Error(ex, ex.Message, oj);
            ConsoleWitreLine(" Exception:" + ex.Message, "Error", useConsole);
        }

        public void Error(string message, Exception ex, string info, bool useConsole = false)
        {
            Logger.Error(ex, message, info);
            ConsoleWitreLine(message + " Exception:" + ex.Message, "Error", useConsole);
        }

        public void ConsoleWitreLine(string msg, string type, bool useConsole = false)
        {
            if (!useConsole) return;
            switch (type)
            {
                case "Error":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "Warning":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        #region Instance

        private LoggerManager()
        {
            Logger = LogManager.GetCurrentClassLogger();

        }

        public static LoggerManager _instance = new LoggerManager();

        public static LoggerManager Instance => _instance;

        #endregion
    }
}