namespace SynergyLauncherCore;

public interface ILogHandler
{
    public void Log(string sender, string message);
    public void LogDebug(string sender, string message);
}
