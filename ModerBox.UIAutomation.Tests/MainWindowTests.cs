using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModerBox.UIAutomation.Tests
{
    [TestClass]
    public class MainWindowTests
    {
        private static Application? app;
        private static UIA3Automation? automation;
        private static Window? window;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var appPath = FindExecutable();
            
            app = Application.Launch(appPath);
            automation = new UIA3Automation();
            window = app.GetMainWindow(automation);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            window?.Close();
            app?.Dispose();
            automation?.Dispose();
        }

        [TestMethod]
        public void MainWindow_ShouldHaveCorrectTitle()
        {
        }

        private static string FindExecutable()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var solutionRoot = Directory.GetParent(assemblyPath!)?.Parent?.Parent?.Parent?.FullName;
            if (solutionRoot == null)
            {
                throw new DirectoryNotFoundException("Could not find the solution root directory.");
            }

            var executableName = "ModerBox.exe";

            var preferredFolders = new[]
            {
                Path.Combine("publish", "native"),
                "publish-native-aot",
                Path.Combine("publish", "nativeaot"),
                "publish-native",
                "publish-normal"
            };

            foreach (var folder in preferredFolders)
            {
                var candidate = Path.Combine(solutionRoot, folder, executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            // Fallback for different build configurations or CI environments
            var files = Directory
                .GetFiles(solutionRoot, executableName, SearchOption.AllDirectories)
                .Where(f => f.Contains("publish", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Contains("native", StringComparison.OrdinalIgnoreCase))
                .ThenBy(f => f.Length)
                .ToArray();

            if (files.Length > 0)
            {
                return files[0];
            }

            throw new FileNotFoundException($"Could not find '{executableName}' in any publish directory under '{solutionRoot}'.");
        }
    }
} 