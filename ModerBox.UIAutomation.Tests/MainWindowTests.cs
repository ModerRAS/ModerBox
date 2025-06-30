using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;

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
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectRoot = Directory.GetParent(assemblyPath!)!.Parent!.Parent!.Parent!.FullName;
            var appPath = Path.Combine(projectRoot, "publish", "native", "ModerBox.exe");
            
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
    }
} 