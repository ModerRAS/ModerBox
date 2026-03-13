using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Cli.Commands;
using FluentAssertions;

namespace ModerBox.Cli.Test;

[TestClass]
public class HarmonicCommandTests
{
    [TestMethod]
    public async Task RunAsync_WithInvalidSourceDirectory_ReturnsError()
    {
        var result = await HarmonicCommand.RunAsync(["--source", "C:\\nonexistent_directory_12345", "--target", "test.xlsx"]);
        result.Should().Be(1);
    }

    [TestMethod]
    public void RunAsync_WithNullArgs_DoesNotThrow()
    {
        var result = HarmonicCommand.RunAsync(null);
        result.Should().NotBeNull();
    }
}

[TestClass]
public class FilterWaveformCommandTests
{
    [TestMethod]
    public async Task RunAsync_WithInvalidSourceDirectory_ReturnsError()
    {
        var result = await FilterWaveformCommand.RunAsync(["--source", "C:\\nonexistent_directory_12345", "--target", "test.xlsx"]);
        result.Should().Be(1);
    }

    [TestMethod]
    public void RunAsync_WithNullArgs_DoesNotThrow()
    {
        var result = FilterWaveformCommand.RunAsync(null);
        result.Should().NotBeNull();
    }
}

[TestClass]
public class CurrentDifferenceCommandTests
{
    [TestMethod]
    public async Task RunAsync_WithInvalidSourceDirectory_ReturnsError()
    {
        var result = await CurrentDifferenceCommand.RunAsync(["--source", "C:\\nonexistent_directory_12345", "--target", "test.csv"]);
        result.Should().Be(1);
    }

    [TestMethod]
    public void RunAsync_WithNullArgs_DoesNotThrow()
    {
        var result = CurrentDifferenceCommand.RunAsync(null);
        result.Should().NotBeNull();
    }
}

[TestClass]
public class QuestionBankCommandTests
{
    [TestMethod]
    public async Task RunAsync_WithInvalidSourceFile_ReturnsError()
    {
        var result = await QuestionBankCommand.RunAsync(["--source", "C:\\nonexistent_file_12345.txt", "--target", "test.xlsx"]);
        result.Should().Be(1);
    }

    [TestMethod]
    public void RunAsync_WithNullArgs_DoesNotThrow()
    {
        var result = QuestionBankCommand.RunAsync(null);
        result.Should().NotBeNull();
    }
}

[TestClass]
public class CableRoutingCommandTests
{
    [TestMethod]
    public async Task RunAsync_WithInvalidConfigFile_ReturnsError()
    {
        var result = await CableRoutingCommand.RunAsync(["--config", "C:\\nonexistent_config_12345.json"]);
        result.Should().Be(1);
    }

    [TestMethod]
    public void RunAsync_WithNullArgs_DoesNotThrow()
    {
        var result = CableRoutingCommand.RunAsync(null);
        result.Should().NotBeNull();
    }
}
