using System.Runtime.CompilerServices;

namespace WhiteBinTools.Native;

public enum LogType
{
    Info,
    Warning,
    Error,
    Debug
}

internal static class NativeLogger
{
    private const string LogFormat = "[WBT NATIVE] [{0}] {1} {2}: {3}";

    private static void Log(string message, LogType type = LogType.Info, 
        [CallerFilePath] string filePath = "", 
        [CallerMemberName] string memberName = "")
    {
        // 1. Extract Class Name from File Path (AOT-friendly way to get context)
        // e.g., "C:\Projects\Repack.cs" becomes "Repack"
        var className = Path.GetFileNameWithoutExtension(filePath);

        // 2. Format the Type Tag
        var typeTag = type switch
        {
            LogType.Info    => "[INFO]",
            LogType.Warning => "[WARN]",
            LogType.Error   => "[ERR]",
            LogType.Debug   => "[DBG]",
            _               => "[INF]"
        };

        // 3. Build the string
        var finalLog = string.Format(LogFormat, className, typeTag, memberName, message);

        // 4. Output with Color (Optional visual aid)
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = type switch
        {
            LogType.Error => ConsoleColor.Red,
            LogType.Warning => ConsoleColor.Yellow,
            LogType.Debug => ConsoleColor.DarkGray,
            _ => ConsoleColor.White
        };

        Console.WriteLine(finalLog);
        
        // Reset color
        Console.ForegroundColor = originalColor;
    }

    // Shorthand helpers
    public static void Info(string msg, [CallerFilePath] string f = "", [CallerMemberName] string m = "") 
        => Log(msg, LogType.Info, f, m);
        
    public static void Error(string msg, [CallerFilePath] string f = "", [CallerMemberName] string m = "") 
        => Log(msg, LogType.Error, f, m);
        
    public static void Warn(string msg, [CallerFilePath] string f = "", [CallerMemberName] string m = "") 
        => Log(msg, LogType.Warning, f, m);
    public static void Debug(string msg, [CallerFilePath] string f = "", [CallerMemberName] string m = "") => Log(msg, LogType.Debug, f, m);
}