using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Linq;

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
            Assert.IsNotNull(window);
            Assert.AreEqual("ModerBox", window.Title);
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
            var searchPattern = Path.Combine("publish", "native", executableName);
            var executablePath = Path.Combine(solutionRoot, searchPattern);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }
            
            // Fallback for different build configurations or CI environments
            var files = Directory.GetFiles(solutionRoot, executableName, SearchOption.AllDirectories);
            var file = files.FirstOrDefault(f => f.Contains(Path.Combine("publish", "native")));

            if (file != null)
            {
                return file;
            }

            throw new FileNotFoundException($"Could not find '{executableName}' in any 'publish/native' directory under '{solutionRoot}'.");
        }
    }
} 