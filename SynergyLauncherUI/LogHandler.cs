using System;
using Avalonia.Threading;
using SynergyLauncherCore;

namespace SynergyLauncherUI;

public class LogHandler(MainWindow mainWindow) : ILogHandler
{
    public void Log(string sender, string message)
    {
        if (IsInvalid(message)) return;
        LogToConsoleAndUi(Format(sender, message));
    }

    public void LogDebug(string sender, string message)
    {
        if (IsInvalid(message)) return;
        Console.WriteLine(Format(sender, "[DEBUG] " + message));
    }
    
    private static bool IsInvalid(string message)
    {
        return string.IsNullOrWhiteSpace(message);
    }

    private void LogToConsoleAndUi(string text)
    {
        Console.WriteLine(text);
        Dispatcher.UIThread.InvokeAsync(() => mainWindow.Logs.Add(new LogData(text)));
    }

    private static string Format(string sender, string message)
    {
        return $"[{sender.ToUpper()}] [{DateTime.Now:HH:mm:ss}] {message}";
    }
}