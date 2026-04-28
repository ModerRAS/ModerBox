using System;
using System.Text.Json;

namespace ModerBox.Cli.Infrastructure;

/// <summary>
/// Global option for JSON output mode.
/// </summary>
public static class GlobalJsonOption
{
    private static bool _isJsonMode;

    /// <summary>
    /// Gets or sets whether JSON output mode is enabled.
    /// When true, JsonOutputWriter outputs JSON to stdout and StatusWriter outputs to stderr.
    /// </summary>
    public static bool IsJsonMode
    {
        get => _isJsonMode;
        set => _isJsonMode = value;
    }
}

/// <summary>
/// Writes JSON output to Console.Out.
/// Use this in JSON mode to output machine-readable results.
/// </summary>
public static class JsonOutputWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Writes the specified object as formatted JSON to Console.Out.
    /// </summary>
    /// <param name="obj">The object to serialize and output.</param>
    public static void Write(object obj)
    {
        var json = JsonSerializer.Serialize(obj, Options);
        Console.Out.Write(json);
    }

    /// <summary>
    /// Writes the specified value as formatted JSON to Console.Out.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize and output.</param>
    public static void Write<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        Console.Out.Write(json);
    }
}

/// <summary>
/// Writes status/progress messages.
/// In JSON mode, outputs to Console.Error to separate from JSON results on stdout.
/// In non-JSON mode, outputs to Console.Out for normal console display (e.g., Spectre.Console).
/// </summary>
public static class StatusWriter
{
    /// <summary>
    /// Writes a line to the appropriate output stream.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteLine(string message)
    {
        if (GlobalJsonOption.IsJsonMode)
        {
            Console.Error.WriteLine(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes a message without a trailing newline to the appropriate output stream.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void Write(string message)
    {
        if (GlobalJsonOption.IsJsonMode)
        {
            Console.Error.Write(message);
        }
        else
        {
            Console.Write(message);
        }
    }
}
