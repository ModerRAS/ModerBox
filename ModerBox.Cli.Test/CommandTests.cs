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

[TestClass]
public class VideoCommandTests
{
    [TestCleanup]
    public void Cleanup()
    {
        GlobalJsonOption.IsJsonMode = false;
    }

    [TestMethod]
    public async Task Invoke_Analyze_MissingApiKey_ReturnsExitCode1()
    {
        var oldKey = Environment.GetEnvironmentVariable("VIDEO_ANALYSIS_API_KEY");
        Environment.SetEnvironmentVariable("VIDEO_ANALYSIS_API_KEY", null);
        try
        {
            var command = VideoCommand.Create();
            var result = await command.InvokeAsync(new[] { "analyze", "--video-path", "test.mp4" });
            result.Should().Be(1, "missing API key should return exit code 1");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VIDEO_ANALYSIS_API_KEY", oldKey);
        }
    }

    [TestMethod]
    public void Create_ParentCommand_HasSubcommandsAnalyzeAndFolder()
    {
        var command = VideoCommand.Create();
        command.Name.Should().Be("video");
        command.Subcommands.Should().Contain(c => c.Name == "analyze");
        command.Subcommands.Should().Contain(c => c.Name == "folder");
    }

    [TestMethod]
    public async Task Invoke_Analyze_MissingVideoPath_ReturnsNonZero()
    {
        var command = VideoCommand.Create();
        var result = await command.InvokeAsync(new[] { "analyze" });
        result.Should().NotBe(0, "missing required --video-path should return non-zero exit code");
    }

    [TestMethod]
    public async Task Invoke_Folder_MissingRequiredArgs_ReturnsNonZero()
    {
        var command = VideoCommand.Create();
        var result = await command.InvokeAsync(new[] { "folder" });
        result.Should().NotBe(0, "missing required --folder-path and --output-folder should return non-zero exit code");
    }

    [TestMethod]
    public void Create_ReturnsCommand_Successfully()
    {
        var command = VideoCommand.Create();
        command.Should().NotBeNull();
        command.Name.Should().Be("video");
        command.Description.Should().Be("视频分析");
    }
}

[TestClass]
public class PeriodicWorkCommandTests
{
    [TestMethod]
    public async Task Invoke_MissingRequiredConfig_ReturnsNonZeroExitCode()
    {
        var command = PeriodicWorkCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\temp_source", "--output", "C:\\temp_output.xlsx" });
        result.Should().NotBe(0, "missing required --config option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_MissingRequiredSource_ReturnsNonZeroExitCode()
    {
        var command = PeriodicWorkCommand.Create();
        var result = await command.InvokeAsync(new[] { "--config", "C:\\temp_config.json", "--output", "C:\\temp_output.xlsx" });
        result.Should().NotBe(0, "missing required --source option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_MissingRequiredOutput_ReturnsNonZeroExitCode()
    {
        var command = PeriodicWorkCommand.Create();
        var result = await command.InvokeAsync(new[] { "--config", "C:\\temp_config.json", "--source", "C:\\temp_source" });
        result.Should().NotBe(0, "missing required --output option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_WithNonexistentConfig_ReturnsExitCode1()
    {
        var command = PeriodicWorkCommand.Create();
        var result = await command.InvokeAsync(new[] {
            "--config", "C:\\nonexistent_config_12345.json",
            "--source", "C:\\nonexistent_source_12345",
            "--output", "C:\\temp_output.xlsx"
        });
        result.Should().Be(1, "nonexistent config file should return exit code 1");
    }

    [TestMethod]
    public void Create_ReturnsCommandWithCorrectNameAndAlias()
    {
        var command = PeriodicWorkCommand.Create();
        command.Name.Should().Be("periodic-work");
        command.Aliases.Should().Contain("pw");
    }
}

[TestClass]
public class FilterCopyCommandTests
{
    [TestMethod]
    public async Task Invoke_MissingRequiredSource_ReturnsNonZeroExitCode()
    {
        var command = FilterCopyCommand.Create();
        var result = await command.InvokeAsync(new[] { "--target", "C:\\temp_target" });
        result.Should().NotBe(0, "missing required --source option should return non-zero exit code");
    }

    [TestMethod]
    public async Task Invoke_MissingRequiredTarget_ReturnsNonZeroExitCode()
    {
        var command = FilterCopyCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\temp_source" });
        result.Should().NotBe(0, "missing required --target option should return non-zero exit code");
    }

    [TestMethod]
    public async Task Invoke_MissingBothRequiredOptions_ReturnsNonZeroExitCode()
    {
        var command = FilterCopyCommand.Create();
        var result = await command.InvokeAsync(Array.Empty<string>());
        result.Should().NotBe(0, "missing both required options should return non-zero exit code");
    }

    [TestMethod]
    public async Task Invoke_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = FilterCopyCommand.Create();
        var result = await command.InvokeAsync(new[]
        {
            "--source", "C:\\nonexistent_directory_12345",
            "--target", "C:\\temp_target"
        });
        result.Should().Be(1, "nonexistent source directory should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_WithInvalidDateFormat_HandlesGracefully()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = FilterCopyCommand.Create();
            // Invalid date format should cause an exception, which is caught → exit code 1
            var result = await command.InvokeAsync(new[]
            {
                "--source", tempDir,
                "--target", Path.Combine(tempDir, "output"),
                "--start-date", "not-a-date"
            });
            result.Should().Be(1, "invalid date format should return exit code 1");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public void Create_ReturnsCommandWithCorrectNameAndAlias()
    {
        var command = FilterCopyCommand.Create();
        command.Name.Should().Be("filter-copy");
        command.Aliases.Should().Contain("fc");
    }

    [TestMethod]
    public void Create_HasAllRequiredOptions()
    {
        var command = FilterCopyCommand.Create();
        command.Options.Should().Contain(o => o.Aliases.Contains("--source"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--target"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--channel-name-regex"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--start-date"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--end-date"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--check-switch-change"));
    }

    [TestMethod]
    public async Task Invoke_WithValidEmptyDirectory_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        var targetDir = Path.Combine(tempDir, "output");
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = FilterCopyCommand.Create();
            var result = await command.InvokeAsync(new[]
            {
                "--source", tempDir,
                "--target", targetDir
            });
            result.Should().Be(0, "valid empty directory should succeed with zero files");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task Invoke_WithJsonMode_OutputsJson()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            var command = FilterCopyCommand.Create();
            await command.InvokeAsync(new[]
            {
                "--source", "C:\\nonexistent_directory_json_test",
                "--target", "C:\\nonexistent_target"
            });

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
    public async Task Invoke_WithShortAliases_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        var targetDir = Path.Combine(tempDir, "output");
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = FilterCopyCommand.Create();
            // Short aliases: -s for source, -t for target
            var result = await command.InvokeAsync(new[]
            {
                "-s", tempDir,
                "-t", targetDir
            });
            result.Should().Be(0, "short aliases should parse correctly");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task Invoke_WithAllOptionalOptions_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        var targetDir = Path.Combine(tempDir, "output");
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = FilterCopyCommand.Create();
            var result = await command.InvokeAsync(new[]
            {
                "--source", tempDir,
                "--target", targetDir,
                "--channel-name-regex", ".*开关.*",
                "--start-date", "2024-01-01",
                "--end-date", "2024-12-31",
                "--check-switch-change", "false"
            });
            result.Should().Be(0, "all optional options should parse correctly with a valid empty directory");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task Invoke_WithValidDateButNonexistentSource_ReturnsExitCode1()
    {
        var command = FilterCopyCommand.Create();
        var result = await command.InvokeAsync(new[]
        {
            "--source", "C:\\nonexistent_directory_12345",
            "--target", "C:\\temp_target",
            "--start-date", "2024-01-01"
        });
        result.Should().Be(1, "valid date but nonexistent source should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_WithoutCheckSwitchChange_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        var targetDir = Path.Combine(tempDir, "output");
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = FilterCopyCommand.Create();
            var result = await command.InvokeAsync(new[]
            {
                "--source", tempDir,
                "--target", targetDir,
                "--check-switch-change", "false"
            });
            result.Should().Be(0, "disabling check-switch-change should parse correctly");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}

[TestClass]
public class ComtradeExportCommandTests
{
    [TestMethod]
    public void Create_ParentCommand_HasSubcommandsListAndExport()
    {
        var command = ComtradeExportCommand.Create();
        command.Name.Should().Be("comtrade-export");
        command.Subcommands.Should().Contain(c => c.Name == "list");
        command.Subcommands.Should().Contain(c => c.Name == "export");
    }

    [TestMethod]
    public async Task List_MissingCfgFile_ReturnsNonzeroExitCode()
    {
        var command = ComtradeExportCommand.Create();
        var result = await command.InvokeAsync("list");
        // Missing required --cfg-file should return non-zero exit code
        result.Should().NotBe(0);
    }

    [TestMethod]
    public async Task Export_MissingRequiredArgs_ReturnsNonzeroExitCode()
    {
        var command = ComtradeExportCommand.Create();
        var result = await command.InvokeAsync("export");
        // Missing required --cfg-file and --output should return non-zero exit code
        result.Should().NotBe(0);
    }

    [TestMethod]
    public async Task List_WithNonexistentFile_ReturnsNonzeroExitCode()
    {
        var command = ComtradeExportCommand.Create();
        var result = await command.InvokeAsync(new[] { "list", "--cfg-file", "C:\\nonexistent_comtrade_12345.cfg" });
        // Should handle nonexistent file gracefully (no exception thrown) and return error exit code
        result.Should().Be(ExitCodes.Error, "nonexistent file should return error exit code");
    }

    [TestMethod]
    public async Task Export_WithNonexistentFile_ReturnsNonzeroExitCode()
    {
        var command = ComtradeExportCommand.Create();
        var result = await command.InvokeAsync(new[] { "export", "--cfg-file", "C:\\nonexistent_comtrade_12345.cfg", "--output", "C:\\output_12345" });
        // Should handle nonexistent file gracefully (no exception thrown) and return error exit code
        result.Should().Be(ExitCodes.Error, "nonexistent file should return error exit code");
    }

    [TestMethod]
    public async Task List_WithJsonMode_OutputsJson()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            var command = ComtradeExportCommand.Create();
            var result = await command.InvokeAsync(new[] { "list", "--cfg-file", "C:\\nonexistent_comtrade_json_test.cfg" });
            result.Should().Be(ExitCodes.Error);

            var output = consoleOut.ToString();
            output.Should().Contain("\"channels\"");
        }
        finally
        {
            Console.SetOut(originalOut);
            GlobalJsonOption.IsJsonMode = false;
        }
    }

    [TestMethod]
    public async Task Export_WithJsonMode_OutputsJson()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            var command = ComtradeExportCommand.Create();
            var result = await command.InvokeAsync(new[] { "export", "--cfg-file", "C:\\nonexistent_comtrade_json_test.cfg", "--output", "C:\\output_test" });
            result.Should().Be(ExitCodes.Error);

            var output = consoleOut.ToString();
            output.Should().Contain("\"error\"");
        }
        finally
        {
            Console.SetOut(originalOut);
            GlobalJsonOption.IsJsonMode = false;
        }
    }

    [TestMethod]
    public async Task Export_NoChannelsSelected_ReturnsErrorJson()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            // Use a temp file that exists so it passes the file-exists check
            var tempFile = Path.GetTempFileName();
            try
            {
                var command = ComtradeExportCommand.Create();
                // No channels selected - should return error
                var result = await command.InvokeAsync(new[] { "export", "--cfg-file", tempFile, "--output", "C:\\output_test" });
                result.Should().Be(ExitCodes.Error, "no channels selected should return error exit code");

                var output = consoleOut.ToString();
                output.Should().Contain("\"success\"");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        finally
        {
            Console.SetOut(originalOut);
            GlobalJsonOption.IsJsonMode = false;
        }
    }

    [TestMethod]
    public void Create_CommandCreatesSuccessfully()
    {
        var command = ComtradeExportCommand.Create();
        command.Should().NotBeNull();
        command.Name.Should().Be("comtrade-export");
        command.Description.Should().NotBeNullOrEmpty();
    }
}

[TestClass]
public class ThreePhaseIdeeCommandTests
{
    [TestMethod]
    public void Create_ReturnsParentCommand_WithCorrectName()
    {
        var command = ThreePhaseIdeeCommand.Create();
        command.Name.Should().Be("threephase-idee");
    }

    [TestMethod]
    public void Create_ParentCommand_HasSubcommandsIdeeAndIdeeIdel()
    {
        var command = ThreePhaseIdeeCommand.Create();
        command.Subcommands.Should().HaveCount(2);
        command.Subcommands.Should().Contain(c => c.Name == "idee");
        command.Subcommands.Should().Contain(c => c.Name == "idee-idel");
    }

    [TestMethod]
    public async Task Invoke_IdeeSubcommand_MissingSource_ReturnsNonZero()
    {
        var command = ThreePhaseIdeeCommand.Create();
        var result = await command.InvokeAsync(new[] { "idee" });
        result.Should().NotBe(0, "missing required --source option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_IdeeIdelSubcommand_MissingSource_ReturnsNonZero()
    {
        var command = ThreePhaseIdeeCommand.Create();
        var result = await command.InvokeAsync(new[] { "idee-idel" });
        result.Should().NotBe(0, "missing required --source option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_IdeeSubcommand_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = ThreePhaseIdeeCommand.Create();
        var result = await command.InvokeAsync(new[] { "idee", "--source", "C:\\nonexistent_directory_12345" });
        result.Should().Be(1, "nonexistent source directory should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_IdeeIdelSubcommand_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = ThreePhaseIdeeCommand.Create();
        var result = await command.InvokeAsync(new[] { "idee-idel", "--source", "C:\\nonexistent_directory_12345" });
        result.Should().Be(1, "nonexistent source directory should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_IdeeSubcommand_WithJsonMode_OutputsJsonError()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            var command = ThreePhaseIdeeCommand.Create();
            await command.InvokeAsync(new[] { "idee", "--source", "C:\\nonexistent_directory_json_test" });

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
    public async Task Invoke_IdeeSubcommand_WithShortAliases_ParsesCorrectly()
    {
        var command = ThreePhaseIdeeCommand.Create();
        // Short aliases: -s for source, -o for output
        var result = await command.InvokeAsync(new[] { "idee", "-s", "C:\\nonexistent_directory_12345", "-o", "C:\\output.xlsx" });
        result.Should().Be(1, "should parse short aliases and reach directory check");
    }

    [TestMethod]
    public async Task Invoke_IdeeIdelSubcommand_WithShortAliases_ParsesCorrectly()
    {
        var command = ThreePhaseIdeeCommand.Create();
        // Short aliases: -s for source, -o for output
        var result = await command.InvokeAsync(new[] { "idee-idel", "-s", "C:\\nonexistent_directory_12345", "-o", "C:\\output.xlsx" });
        result.Should().Be(1, "should parse short aliases and reach directory check");
    }

    [TestMethod]
    public async Task Invoke_IdeeSubcommand_WithValidDirectory_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = ThreePhaseIdeeCommand.Create();
            var result = await command.InvokeAsync(new[] { "idee", "--source", tempDir });
            result.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task Invoke_IdeeIdelSubcommand_WithValidDirectory_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ModerBox_Test_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var command = ThreePhaseIdeeCommand.Create();
            var result = await command.InvokeAsync(new[] { "idee-idel", "--source", tempDir });
            result.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}

[TestClass]
public class ContributionCommandTests
{
    [TestMethod]
    public async Task Invoke_MissingRequiredSource_ReturnsNonZeroExitCode()
    {
        var command = ContributionCommand.Create();
        var result = await command.InvokeAsync(Array.Empty<string>());
        result.Should().NotBe(0, "missing required --source option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = ContributionCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_file_12345.csv" });
        result.Should().Be(1, "nonexistent source file should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_WithShortAlias_Source_ReturnsExitCode1()
    {
        var command = ContributionCommand.Create();
        var result = await command.InvokeAsync(new[] { "-s", "C:\\nonexistent_file_12345.csv" });
        result.Should().Be(1, "short alias -s should work and return exit code 1 for nonexistent file");
    }

    [TestMethod]
    public async Task Invoke_WithTargetOption_ParsesCorrectly()
    {
        var command = ContributionCommand.Create();
        // --target should parse correctly even with nonexistent source
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_file.csv", "--target", "C:\\output.xlsx" });
        result.Should().Be(1, "target option should parse and reach file check");
    }

    [TestMethod]
    public async Task Invoke_WithShortTargetOption_ParsesCorrectly()
    {
        var command = ContributionCommand.Create();
        // -t short alias should work
        var result = await command.InvokeAsync(new[] { "-s", "C:\\nonexistent_file.csv", "-t", "C:\\output.xlsx" });
        result.Should().Be(1, "short alias -t should work and parse correctly");
    }

    [TestMethod]
    public void Create_ReturnsCommandWithAliasCtb()
    {
        var command = ContributionCommand.Create();
        command.Name.Should().Be("contribution");
        command.Aliases.Should().Contain("ctb");
    }

    [TestMethod]
    public void Create_HasAllOptions()
    {
        var command = ContributionCommand.Create();
        command.Options.Should().HaveCount(2);
        command.Options.Should().Contain(o => o.Aliases.Contains("--source"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--target"));
    }

    [TestMethod]
    public async Task Invoke_WithJsonMode_OutputsJson()
    {
        GlobalJsonOption.IsJsonMode = true;
        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);

        try
        {
            var command = ContributionCommand.Create();
            await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_file_json_test.csv" });

            var output = consoleOut.ToString();
            output.Should().Contain("\"success\"");
        }
        finally
        {
            Console.SetOut(originalOut);
            GlobalJsonOption.IsJsonMode = false;
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        GlobalJsonOption.IsJsonMode = false;
    }
}

[TestClass]
public class SwitchReportCommandTests
{
    [TestMethod]
    public async Task Invoke_MissingRequiredSource_ReturnsNonZeroExitCode()
    {
        var command = SwitchReportCommand.Create();
        var result = await command.InvokeAsync(new[] { "--target", "C:\\output.xlsx" });
        result.Should().NotBe(0, "missing required --source option should return error exit code");
    }

    [TestMethod]
    public async Task Invoke_MissingRequiredTarget_ReturnsNonZeroExitCode()
    {
        var command = SwitchReportCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\some_directory" });
        result.Should().NotBe(0, "missing required --target option should return error exit code");
    }

    [TestMethod]
    public void Create_ReturnsCommand_WithCorrectName()
    {
        var command = SwitchReportCommand.Create();
        command.Name.Should().Be("switch-report");
    }

    [TestMethod]
    public async Task Invoke_WithNonexistentSource_ReturnsExitCode1()
    {
        var command = SwitchReportCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345", "--target", "C:\\output.xlsx" });
        result.Should().Be(1, "nonexistent source directory should return exit code 1");
    }

    [TestMethod]
    public async Task Invoke_WithSlidingWindowFlag_ParsesCorrectly()
    {
        var command = SwitchReportCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345", "--target", "C:\\output.xlsx", "--use-sliding-window" });
        result.Should().Be(1, "sliding window flag should parse correctly and reach directory check");
    }

    [TestMethod]
    public async Task Invoke_WithCustomWorkers_ParsesCorrectly()
    {
        var command = SwitchReportCommand.Create();
        var result = await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_12345", "--target", "C:\\output.xlsx", "--io-workers", "8", "--process-workers", "12" });
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
            var command = SwitchReportCommand.Create();
            await command.InvokeAsync(new[] { "--source", "C:\\nonexistent_directory_json_test", "--target", "C:\\output.xlsx" });

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
    public void Create_HasAllOptions()
    {
        var command = SwitchReportCommand.Create();
        command.Options.Should().HaveCount(5);
        command.Options.Should().Contain(o => o.Aliases.Contains("--source"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--target"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--use-sliding-window"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--io-workers"));
        command.Options.Should().Contain(o => o.Aliases.Contains("--process-workers"));
    }
}
