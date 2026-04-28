using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModerBox.Cli.Test;

/// <summary>
/// Base class for CLI command integration tests.
/// Provides process-based CLI execution and JSON assertion helpers.
/// </summary>
public abstract class CommandTestBase
{
    private readonly string _cliProjectPath;

    protected CommandTestBase()
    {
        // Resolve to ModerBox.Cli project directory
        var currentDir = Directory.GetCurrentDirectory();
        _cliProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "ModerBox.Cli"));
    }

    /// <summary>
    /// Runs the CLI application with the specified arguments and captures output.
    /// </summary>
    /// <param name="args">Command-line arguments to pass to the CLI</param>
    /// <returns>Tuple containing exit code, stdout, and stderr</returns>
    protected async Task<(int exitCode, string stdout, string stderr)> RunCommand(params string[] args)
    {
        var dotnetPath = "dotnet";
        var cliProject = Path.Combine(_cliProjectPath, "ModerBox.Cli.csproj");
        var fullArgs = $"run --project \"{cliProject}\" -- {string.Join(" ", args)}";

        var startInfo = new ProcessStartInfo
        {
            FileName = dotnetPath,
            Arguments = fullArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stdout.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    /// <summary>
    /// Runs the CLI and asserts the expected exit code and output.
    /// </summary>
    /// <param name="expectedExitCode">Expected process exit code</param>
    /// <param name="expectedStdout">Expected stdout substring (null to skip check)</param>
    /// <param name="expectedStderr">Expected stderr substring (null to skip check)</param>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Tuple containing exit code, stdout, and stderr for further assertions</returns>
    protected async Task<(int exitCode, string stdout, string stderr)> RunCommandAndAssert(
        int expectedExitCode,
        string? expectedStdout,
        string? expectedStderr,
        params string[] args)
    {
        var result = await RunCommand(args);

        Assert.AreEqual(expectedExitCode, result.exitCode,
            $"Exit code mismatch. Stdout: {result.stdout}, Stderr: {result.stderr}");

        if (expectedStdout != null)
        {
            Assert.IsTrue(result.stdout.Contains(expectedStdout),
                $"Expected stdout to contain: {expectedStdout}. Actual: {result.stdout}");
        }

        if (expectedStderr != null)
        {
            Assert.IsTrue(result.stderr.Contains(expectedStderr),
                $"Expected stderr to contain: {expectedStderr}. Actual: {result.stderr}");
        }

        return result;
    }

    /// <summary>
    /// Asserts that the given string is valid JSON.
    /// </summary>
    /// <param name="json">JSON string to validate</param>
    protected void AssertValidJson(string json)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(json), "JSON string cannot be null or empty");

        Exception? exception = null;
        try
        {
            JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        Assert.IsNull(exception, $"Invalid JSON: {exception?.Message}");
    }

    /// <summary>
    /// Asserts that the JSON contains the expected value at the specified path.
    /// Uses JsonNode for path navigation (e.g., "$.data.name" or "[0].value").
    /// </summary>
    /// <param name="json">JSON string to parse</param>
    /// <param name="path">JSON path (e.g., "$.name", "$.items[0].id")</param>
    /// <param name="expectedValue">Expected value at the path</param>
    protected void AssertJsonPath(string json, string path, object expectedValue)
    {
        AssertValidJson(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Handle both JsonElement and JsonNode approaches
        JsonElement current = root;

        // Parse path: $.foo.bar -> ["foo", "bar"], $.foo[0].bar -> ["foo", "0", "bar"]
        var segments = ParseJsonPath(path);

        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out var index))
            {
                Assert.AreEqual(JsonValueKind.Array, current.ValueKind,
                    $"Cannot index into {current.ValueKind} at path segment '{segment}'");
                Assert.IsTrue(index < current.GetArrayLength(),
                    $"Array index {index} out of bounds for array of length {current.GetArrayLength()}");
                current = current[index];
            }
            else
            {
                Assert.AreEqual(JsonValueKind.Object, current.ValueKind,
                    $"Cannot access property '{segment}' on {current.ValueKind}");
                Assert.IsTrue(current.TryGetProperty(segment, out current),
                    $"Property '{segment}' not found in JSON object");
            }
        }

        // Compare the actual value with expected
        var actualValue = GetJsonElementValue(current);
        var expectedString = expectedValue?.ToString();
        var actualString = actualValue?.ToString();

        Assert.AreEqual(expectedString, actualString,
            $"JSON path '{path}' expected value '{expectedString}' but got '{actualString}'");
    }

    private static string[] ParseJsonPath(string path)
    {
        // Remove leading $ if present
        if (path.StartsWith("$"))
            path = path.Substring(1);

        if (path.StartsWith("."))
            path = path.Substring(1);

        // Split by '.' but handle array indices
        var segments = new List<string>();
        var current = new StringBuilder();

        for (int i = 0; i < path.Length; i++)
        {
            var c = path[i];

            if (c == '.')
            {
                if (current.Length > 0)
                {
                    segments.Add(current.ToString());
                    current.Clear();
                }
            }
            else if (c == '[')
            {
                if (current.Length > 0)
                {
                    segments.Add(current.ToString());
                    current.Clear();
                }
                // Find closing bracket
                var endBracket = path.IndexOf(']', i);
                if (endBracket == -1)
                    throw new ArgumentException($"Invalid JSON path: missing closing bracket at index {i}");

                var indexStr = path.Substring(i + 1, endBracket - i - 1);
                segments.Add(indexStr);
                i = endBracket;
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            segments.Add(current.ToString());

        return segments.ToArray();
    }

    private static object? GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(GetJsonElementValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => GetJsonElementValue(p.Value)),
            _ => throw new InvalidOperationException($"Unexpected JSON value kind: {element.ValueKind}")
        };
    }
}
