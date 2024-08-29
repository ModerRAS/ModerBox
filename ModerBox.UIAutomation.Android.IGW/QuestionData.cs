using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.UIAutomation.Android.IGW {
    public class QuestionData {
        public string Question { get; set; }
        public List<string> Answers { get; set; }
        public List<int> CorrectAnswer { get; set; }
    }
}
