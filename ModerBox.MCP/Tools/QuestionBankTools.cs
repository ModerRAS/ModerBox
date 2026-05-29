using ModerBox.QuestionBank;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class QuestionBankTools
{
    [McpServerTool, Description("Convert question bank between different formats. Supports various source and target formats including TXT, Excel, JSON formats.")]
    public static QuestionBankConversionResult ConvertQuestionBank(
        [Description("Source question bank file path")] string sourcePath,
        [Description("Target output file path")] string targetPath,
        [Description("Target format (Ksb, Mtb, Wldx, Wldx4, Xiaobao, XiaobaoTxt)")] QuestionBankTargetFormat targetFormat,
        [Description("Source format (AutoDetect, Txt, Wldx, Wldx4, Exc, Gdpx, Simple). If not specified, format will be auto-detected.")] QuestionBankSourceFormat? sourceFormat = null)
    {
        var result = new QuestionBankConversionResult
        {
            SourcePath = sourcePath,
            TargetPath = targetPath,
            TargetFormat = targetFormat.ToString()
        };

        try
        {
            if (!File.Exists(sourcePath))
            {
                result.Success = false;
                result.ErrorMessage = $"Source file does not exist: {sourcePath}";
                return result;
            }

            var service = new QuestionBankConversionService();
            var sourceFmt = sourceFormat ?? QuestionBankSourceFormat.AutoDetect;

            var summary = service.Convert(sourcePath, targetPath, sourceFmt, targetFormat);

            result.Success = true;
            result.QuestionCount = summary.QuestionCount;
            result.SourceFormat = summary.SourceFormat.ToString();
            result.TargetFormat = summary.TargetFormat.ToString();
            result.OutputPath = summary.TargetPath;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Detect the format of a question bank source file. Supports auto-detection for TXT, Excel (various formats), and JSON.")]
    public static QuestionBankFormatDetectionResult DetectQuestionBankFormat(
        [Description("File path of the question bank to detect")] string filePath)
    {
        var result = new QuestionBankFormatDetectionResult
        {
            FilePath = filePath
        };

        try
        {
            if (!File.Exists(filePath))
            {
                result.Success = false;
                result.ErrorMessage = $"File does not exist: {filePath}";
                return result;
            }

            var service = new QuestionBankConversionService();
            var detectedFormat = service.DetectSourceFormat(filePath);

            result.Success = true;
            result.DetectedFormat = detectedFormat.ToString();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.DetectedFormat = "Unknown";
        }

        return result;
    }

    [McpServerTool, Description("Merge multiple question bank files into one output file. Supports auto-detection per source file and optional duplicate removal.")]
    public static QuestionBankMergeResult MergeQuestionBanks(
        [Description("Source question bank file paths to merge")] string[] sourcePaths,
        [Description("Target output file path")] string targetPath,
        [Description("Target format (Ksb, Mtb, Wldx, Wldx4, Xiaobao, XiaobaoTxt)")] QuestionBankTargetFormat targetFormat,
        [Description("Source format (AutoDetect, Txt, Ksb, Mtb, Wldx, Wldx4, Exc, Gdpx, Simple). If not specified, each source file will be auto-detected.")] QuestionBankSourceFormat? sourceFormat = null,
        [Description("Remove duplicate questions while merging. Defaults to true.")] bool deduplicate = true)
    {
        var result = new QuestionBankMergeResult
        {
            SourcePaths = sourcePaths,
            TargetPath = targetPath,
            TargetFormat = targetFormat.ToString()
        };

        try
        {
            if (sourcePaths.Length == 0)
            {
                result.Success = false;
                result.ErrorMessage = "At least one source file is required.";
                return result;
            }

            var missing = sourcePaths.FirstOrDefault(path => !File.Exists(path));
            if (missing is not null)
            {
                result.Success = false;
                result.ErrorMessage = $"Source file does not exist: {missing}";
                return result;
            }

            var service = new QuestionBankConversionService();
            var sourceFmt = sourceFormat ?? QuestionBankSourceFormat.AutoDetect;
            var title = Path.GetFileNameWithoutExtension(targetPath);

            var summary = service.Merge(sourcePaths, targetPath, sourceFmt, targetFormat, deduplicate, title);

            result.Success = true;
            result.SourceFileCount = summary.SourceFileCount;
            result.TotalQuestionCount = summary.TotalQuestionCount;
            result.DuplicateQuestionCount = summary.DuplicateQuestionCount;
            result.OutputQuestionCount = summary.OutputQuestionCount;
            result.TargetFormat = summary.TargetFormat.ToString();
            result.OutputPath = summary.TargetPath;
            result.Sources = summary.Sources
                .Select(source => new QuestionBankMergeSourceResult
                {
                    SourcePath = source.SourcePath,
                    SourceFormat = source.SourceFormat.ToString(),
                    QuestionCount = source.QuestionCount
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class QuestionBankConversionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourcePath { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public int QuestionCount { get; set; }
    public string SourceFormat { get; set; } = "";
    public string TargetFormat { get; set; } = "";
    public string OutputPath { get; set; } = "";
}

public class QuestionBankFormatDetectionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string FilePath { get; set; } = "";
    public string DetectedFormat { get; set; } = "";
}

public class QuestionBankMergeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string[] SourcePaths { get; set; } = Array.Empty<string>();
    public string TargetPath { get; set; } = "";
    public int SourceFileCount { get; set; }
    public int TotalQuestionCount { get; set; }
    public int DuplicateQuestionCount { get; set; }
    public int OutputQuestionCount { get; set; }
    public string TargetFormat { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public QuestionBankMergeSourceResult[] Sources { get; set; } = Array.Empty<QuestionBankMergeSourceResult>();
}

public class QuestionBankMergeSourceResult
{
    public string SourcePath { get; set; } = "";
    public string SourceFormat { get; set; } = "";
    public int QuestionCount { get; set; }
}
