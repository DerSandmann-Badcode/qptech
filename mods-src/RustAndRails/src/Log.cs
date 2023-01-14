using System;
using Vintagestory.API.Common;

namespace RustAndRails.src
{
    public static class Log
    {
        static ILogger _logger = new SilentLogger();
        public static ILogger Logger
        {
            get => _logger;
            set => _logger = value;
        }
    }

    public class SilentLogger : ILogger
    {
        public bool TraceLog { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event LogEntryDelegate EntryAdded;

        public void Audit(string format, params object[] args)
        {
        }

        public void Audit(string message)
        {
        }

        public void Build(string format, params object[] args)
        {
        }

        public void Build(string message)
        {
        }

        public void Chat(string format, params object[] args)
        {
        }

        public void Chat(string message)
        {
        }

        public void ClearWatchers()
        {
        }

        public void Debug(string format, params object[] args)
        {
        }

        public void Debug(string message)
        {
        }

        public void Error(string format, params object[] args)
        {
        }

        public void Error(string message)
        {
        }

        public void Event(string format, params object[] args)
        {
        }

        public void Event(string message)
        {
        }

        public void Fatal(string format, params object[] args)
        {
        }

        public void Fatal(string message)
        {
        }

        public void Log(EnumLogType logType, string format, params object[] args)
        {
        }

        public void Log(EnumLogType logType, string message)
        {
        }

        public void Notification(string format, params object[] args)
        {
        }

        public void Notification(string message)
        {
        }

        public void StoryEvent(string format, params object[] args)
        {
        }

        public void StoryEvent(string message)
        {
        }

        public void VerboseDebug(string format, params object[] args)
        {
        }

        public void VerboseDebug(string message)
        {
        }

        public void Warning(string format, params object[] args)
        {
        }

        public void Warning(string message)
        {
        }
    }
}
