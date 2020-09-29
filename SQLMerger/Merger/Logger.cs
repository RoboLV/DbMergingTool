using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Merger
{
    public static class Logger
    {
        private static List<string> msg = new List<string>();

        public static void LogErrorMessage(string message)
        {
            msg.Add($"<div class='msg msg-error'>{message}</div>");
        }

        public static void LogInfoMessage(string message)
        {
            msg.Add($"<div class='msg msg-log'>{message}</div>");
        }

        public static void LogWarningMessage(string message)
        {
            msg.Add($"<div class='msg msg-warning'>{message}</div>");
        }
    }
}
