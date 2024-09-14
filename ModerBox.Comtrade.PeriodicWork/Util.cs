using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Formats.Asn1;

namespace ModerBox.Comtrade.PeriodicWork {

    public static class Util {
        // Parallel processing using ThreadPoolExecutor equivalent in C#
        public static List<T> ParallelProcess<T>(Func<string, T> processFile, IEnumerable<string> allFiles, Action<string, double> progressPrinter) {
            var resultData = new List<T>();
            var numCpus = Environment.ProcessorCount;

            Parallel.ForEach(allFiles, new ParallelOptions { MaxDegreeOfParallelism = numCpus }, file =>
            {
                try {
                    var data = processFile(file);
                    lock (resultData) {
                        resultData.Add(data);
                        progressPrinter("", (double)resultData.Count / allFiles.Count());
                    }
                } catch (Exception ex) {
                    progressPrinter($"处理 {file} 时发生异常: {ex.Message}", 0);
                }
            });

            return resultData;
        }

        public static string GetFilenameKeywordWithPole(string name) {
            if (name.Length == 6) {
                return $"{name.Substring(0, 5)}{name[5]}1";
            }
            if (name.Length == 7) {
                return $"{name.Substring(0, 5)}{name[6]}{name[5]}";
            }
            return name;
        }

        public static string GetFilenameKeyword(string name) {
            if (name.Length == 5) {
                return $"P{name[3]}{name.Substring(0, 3)}{name[4]}1";
            }
            if (name.Length == 6) {
                return $"P{name[3]}{name.Substring(0, 3)}{name[5]}{name[4]}";
            }
            return name;
        }

        

        

        public static Dictionary<string, object> ConvertDataToCsvStyle(string dataname, List<Dictionary<string, object>> data, bool transpose = true) {
            HashSet<string> allNames = new HashSet<string>();
            foreach (var item in data) {
                foreach (var subItem in (List<Dictionary<string, string>>)item["data"]) {
                    allNames.Add(subItem["name"]);
                }
            }

            var rows = new List<Dictionary<string, object>>();
            foreach (var item in data) {
                var row = new Dictionary<string, object> { { "name", item["name"] } };
                foreach (var subItem in (List<Dictionary<string, string>>)item["data"]) {
                    row[subItem["name"]] = subItem["value"];
                }

                foreach (var nameKey in allNames) {
                    if (!row.ContainsKey(nameKey))
                        row[nameKey] = "";
                }
                rows.Add(row);
            }

            if (transpose) {
                var transposedRows = new List<Dictionary<string, object>>();
                foreach (var name in allNames) {
                    var transposedRow = new Dictionary<string, object> { { "name", name } };
                    foreach (var row in rows) {
                        transposedRow[row["name"].ToString()] = row.GetValueOrDefault(name, "");
                    }
                    transposedRows.Add(transposedRow);
                }

                return new Dictionary<string, object>
                {
                { "dataname", dataname },
                { "rows", rows },
                { "data", transposedRows },
                { "lines", ((List<string>)data[0]["row"]) }
            };
            } else {
                return new Dictionary<string, object>
                {
                { "dataname", dataname },
                { "rows", allNames.Select(name => new Dictionary<string, string> { { "name", name } }).ToList() },
                { "data", rows },
                { "lines", data.Select(item => item["name"].ToString()).ToList() }
            };
            }
        }
        public static List<List<T>> ChunkArray<T>(List<T> arr, int chunkSize = 1000) {
            var chunks = new List<List<T>>();
            for (int i = 0; i < arr.Count; i += chunkSize) {
                chunks.Add(arr.GetRange(i, Math.Min(chunkSize, arr.Count - i)));
            }
            return chunks;
        }

        public static List<List<T>> OverlapChunks<T>(List<T> arr, int chunkSize = 1000) {
            var chunks = new List<List<T>>();
            for (int i = 0; i <= arr.Count - chunkSize; i++) {
                chunks.Add(arr.GetRange(i, chunkSize));
            }
            return chunks;
        }

        
    }
    

}
