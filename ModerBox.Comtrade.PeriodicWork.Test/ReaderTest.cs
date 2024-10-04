using ModerBox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Test {
    [TestClass]
    public class ReaderTest {
        [TestMethod]
        public async Task ReadMaxTest() {
            // 根据过滤器过滤文件
            var files = FileHelper.FilterFiles("C:\\Users\\adqew\\Downloads\\换流变录波", new List<string> { });
            // 获取文件中以cfg结尾的文件

            var filteredFiles = from e in files
                        where e.ToLower().EndsWith(".cfg")
                        select e;

            var table = new DataWriter();
            var tmp = new List<List<string>>();
            Parallel.ForEach(filteredFiles, async file => {
                // 读取波形
                var comtradeInfo = await Comtrade.ReadComtradeCFG(file);
                Comtrade.ReadComtradeDAT(comtradeInfo).Wait();
                // 根据要求筛选波形
                var matchedObjects = from a in comtradeInfo.AData
                                     where (a.Name.EndsWith("3Io") || a.Name.EndsWith("3I0")) && a.Name.Contains("网侧") && a.Name.Contains("角接")
                                     select a;
                foreach (var matchedObject in matchedObjects) {
                    var count = 0;
                    var exists = false;
                    for (int i = 0; i < matchedObject.Data.Length; i++) {
                        if (count > 20) {
                            exists = true;
                        }
                        if (matchedObject.Data[i] * 2000 > 40.0) {
                            count++;
                        } else {
                            if (exists) {
                                break;
                            } else {
                                count = 0;
                            }
                        }
                    }
                    if (exists) {
                        tmp.Add(new List<string>() { comtradeInfo.dt0.ToString("u"), matchedObject.Name, (MathHelper.GetMax(matchedObject.Data) * 2000).ToString(), count.ToString() });
                    }
                    
                }
            });
            //foreach (var file in filteredFiles) {
                
            //}
            table.WriteDoubleList(tmp, "tmp");
            table.SaveAs("Z:\\换流变录波3.xlsx");
        }
    }
}
