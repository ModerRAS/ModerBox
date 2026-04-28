using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Cli.Commands;
using ModerBox.Cli.Infrastructure;
using FluentAssertions;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace ModerBox.Cli.Test;

[TestClass]
public class HarmonicCommandTests
{
    [TestMethod]
    public async Task Create_Invoke_WithoutSource_ReturnsError()
    {
        var command = HarmonicCommand.Create();
        var result = await command.InvokeAsync(Array.Empty<string>());
        // System.CommandLine returns non-zero when a required option is missing
        result.Should().NotBe(0);
    }

    [TestMethod]
    public async Task Create_Invoke_WithNonexistentDirectory_ReturnsExitCode1()
    {
        var command = HarmonicCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345" });
        result.Should().Be(1);
    }

    [TestMethod]
    public async Task Create_Invoke_WithValidDirectory_Succeeds()
    {
        // Create a temporary empty directory (no COMTRADE files = empty results, but no error)
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = HarmonicCommand.Create();
            var result = await command.InvokeAsync(new[] { "--source", tempDir });
            result.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task Create_Invoke_WithJsonMode_OutputsJson()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            var command = HarmonicCommand.Create();
            await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_json_test" });

            var output = consoleOut.ToString();
            output.Should().Contain("\"success\"");
        }
        finally
        {
            Console.SetOut(originalOut);
            GlobalJsonOption.IsJsonMode = false;
        }
    }

    [TestMethod]
    public async Task Create_Invoke_WithHighPrecisionFlag_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = HarmonicCommand.Create();
            // --high-precision should be parsed without error
            var result = await command.InvokeAsync(new[] { "--source", tempDir, "--high-precision" });
            result.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task Create_Invoke_WithCustomTarget_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = HarmonicCommand.Create();
            // Custom target path should be parsed without error
            var result = await command.InvokeAsync(new[] { "--source", tempDir, "--target", Path.Combine(tempDir, "custom.xlsx") });
            result.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task Create_Invoke_WithShortAliases_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = HarmonicCommand.Create();
            // Short aliases: -s for source, -t for target, -p for high-precision
            var result = await command.InvokeAsync(new[] { "-s", tempDir, "-t", Path.Combine(tempDir, "out.xlsx"), "-p" });
            result.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public void Create_ReturnsCommand_WithCorrectNameAndAlias()
    {
        var command = HarmonicCommand.Create();
        command.Name.Should().Be("harmonic");
        command.Aliases.Should().Contain("h");
    }
}

[TestClass]
public class FilterWaveformCommandTests
{
    [TestMethod]
    public async Task Invoke_MissingRequiredSource_ReturnsExitCode2()
    {
        var command = FilterWaveformCommand.Create();
        var result = await command.InvokeAsync(Array.Empty<string>());
        result.Should().NotBe(0, "missing required --source option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = FilterWaveformCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345" });
        result.Should().Be(1, "nonexistent source directory should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_WithOldAlgorithmFlag_ParsesCorrectly()
    {
        var command = FilterWaveformCommand.Create();
        // Verify --old-algorithm flag parses without error (will still fail on nonexistent dir)
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345", "--old-algorithm" });
        result.Should().Be(1, "old-algorithm flag should parse correctly and process to directory check");
    }

    [TestMethod]
    public async Task Invoke_WithJsonFlag_ChangesOutputMode()
    {
        var command = FilterWaveformCommand.Create();
        GlobalJsonOption.IsJsonMode = true;
        try
        {
            var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345" });
            result.Should().Be(1, "JSON mode should still return error for nonexistent directory");
        }
        finally
        {
            GlobalJsonOption.IsJsonMode = false;
        }
    }

    [TestMethod]
    public void Create_ReturnsCommandWithAliasF()
    {
        var command = FilterWaveformCommand.Create();
        command.Name.Should().Be("filter");
        command.Aliases.Should().Contain("f");
    }

    [TestMethod]
    public void Create_HasAllOptions()
    {
        var command = FilterWaveformCommand.Create();
        command.Options.Should().HaveCount(5);
        command.Options.Should().Contain(o => o.Aliases.Contains("--source"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--target"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--old-algorithm"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--io-workers"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--process-workers"));
    }

    [TestMethod]
    public async Task Invoke_WithDefaultWorkers_ParsesCorrectly()
    {
        var command = FilterWaveformCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345", "--io-workers", "8", "--process-workers", "12" });
        result.Should().Be(1, "should parse custom worker counts and reach directory check");
    }

    [TestMethod]
    public async Task Invoke_WithJsonMode_OutputsJsonError()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            var command = FilterWaveformCommand.Create();
            await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_json_test" });

            var output = consoleOut.ToString();
            output.Should().Contain("\"success\"");
        }
        finally
        {
            Console.SetOut(originalOut);
            GlobalJsonOption.IsJsonMode = false;
        }
    }
}

[TestClass]
public class CurrentDifferenceCommandTests
{
    [TestMethod]
    public async Task Invoke_MissingRequiredSource_ReturnsExitCode2()
    {
        var command = CurrentDifferenceCommand.Create();
        var result = await command.InvokeAsync(Array.Empty<string>());
        result.Should().NotBe(0, "missing required --source option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = CurrentDifferenceCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345" });
        result.Should().Be(1, "nonexistent source directory should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_WithChartAndTop100Flags_ParsesCorrectly()
    {
        var command = CurrentDifferenceCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345", "--chart", "--top100" });
        result.Should().Be(1, "chart and top100 flags should parse and reach directory check");
    }

    [TestMethod]
    public void Create_ReturnsCommandWithAliasCd()
    {
        var command = CurrentDifferenceCommand.Create();
        command.Name.Should().Be("current-diff");
        command.Aliases.Should().Contain("cd");
    }
}

[TestClass]
public class QuestionBankCommandTests
{
    [TestMethod]
    public async Task Invoke_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = QuestionBankCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_file_12345.txt" });
        result.Should().Be(1, "nonexistent source file should return exit code 1");
    }

    [TestMethod]
    public void Create_ReturnsCommandWithAliasQb()
    {
        var command = QuestionBankCommand.Create();
        command.Name.Should().Be("question-bank");
        command.Aliases.Should().Contain("qb");
    }
}

[TestClass]
public class CableRoutingCommandTests
{
    [TestMethod]
    public async Task Invoke_WithNonexistentConfig_ReturnsExitCode1()
    {
        var command = CableRoutingCommand.Create();
        var result = await command.InvokeAsync(new[] { "--config", "C:\\nonexistent_config_12345.json" });
        result.Should().Be(1, "nonexistent config file should return exit code 1");
    }

    [TestMethod]
    public void Create_ReturnsCommandWithAliasC()
    {
        var command = CableRoutingCommand.Create();
        command.Name.Should().Be("cable");
        command.Aliases.Should().Contain("c");
    }
}
