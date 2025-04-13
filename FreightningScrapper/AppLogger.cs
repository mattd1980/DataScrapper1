using System;

public static class AppLogger
{
    public static void Info(string message)
    {
        Write(message, ConsoleColor.Cyan, "INFO");
    }

    public static void Success(string message)
    {
        Write(message, ConsoleColor.Green, "OK");
    }

    public static void Warn(string message)
    {
        Write(message, ConsoleColor.Yellow, "WARN");
    }

    public static void Error(string message)
    {
        Write(message, ConsoleColor.Red, "ERR");
    }

    private static void Write(string message, ConsoleColor color, string level)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");

        Console.ForegroundColor = color;
        Console.Write($"[{level}] ");

        Console.ResetColor();
        Console.WriteLine(message);
    }
}
