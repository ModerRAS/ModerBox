using LiteDB;
using ModerBox.UIAutomation.Android.Common;
using System.Xml;

namespace ModerBox.UIAutomation.Android.IGW {
    public class AutoAnswerDaily {
        public AdbWrapper AdbWrapper { get; set; }
        public LiteDatabase Database { get; set; }
        public string XmlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tmp.xml");
        public ILiteCollection<QuestionData> QuestionData { get; set; }
        public readonly string Question = "000000010000140";
        public readonly string AnswerPrefix = "000000010000141";
        public readonly string AnswerPostfix = "001";
        public readonly string AnswerA = "0000000100001410001";
        public readonly string AnswerB = "0000000100001411001";
        public readonly string AnswerC = "0000000100001412001";
        public readonly string AnswerD = "0000000100001413001";
        public readonly string AnswerE = "0000000100001414001";

        public void Init() {
            Database = new LiteDatabase(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IGWAAD.db"));
            QuestionData = Database.GetCollection<QuestionData>();
        }
        public AutoAnswerDaily(AdbWrapper adbWrapper) {
            Init();
            AdbWrapper = adbWrapper;
            }
        public AutoAnswerDaily(string adbPath, string deviceSerial) {
            Init();
            AdbWrapper = new AdbWrapper(adbPath, deviceSerial);
        }
        public async Task<XmlDocument> GetXmlAsync() {
            AdbWrapper.GetUIAutomator(XmlPath);
            var xmlStr = await File.ReadAllTextAsync(XmlPath);
            return XmlHelper.LoadXmlFromString(xmlStr);
        }
        public string GenerateAnswerXmlPath(int num) {
            return $"{AnswerPrefix}{num}{AnswerPostfix}";
        }
        public async Task RunTask() {
            var xml = await GetXmlAsync();
            var question = xml.GetNodeByPath(Question).GetNodeText().GetQuestion();
            var questionData = QuestionData.Find(a => a.Question.Equals(question)).FirstOrDefault();
            if (questionData != null) { 
                foreach (var ans in questionData.CorrectAnswer) {
                    xml.GetNodeByPath(GenerateAnswerXmlPath(ans));
                }
            }
        }



    }
}
