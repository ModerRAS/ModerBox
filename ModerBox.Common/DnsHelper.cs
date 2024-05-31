using DnsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common {
    public class DnsHelper {
        public static Dictionary<string, string> GetTxtRecords(string domain) {
            var result = new Dictionary<string, string>();
            try {
                var query = new LookupClient();
                var response = query.Query(domain, QueryType.TXT);

                if (response.HasError) {
                    throw new Exception($"获取TXT记录时发生错误：{response.ErrorMessage}");
                }

                foreach (var record in response.Answers) {
                    if (record.RecordType == DnsClient.Protocol.ResourceRecordType.TXT) {
                        var txtRecord = (DnsClient.Protocol.TxtRecord)record;
                        foreach (var txtValue in txtRecord.Text) {
                            result.Merge(ParseTxtRecord(txtValue));
                        }
                    }
                }
            } catch (Exception ex) {
                throw new Exception($"获取TXT记录时发生错误：{ex.Message}");
            }

            return result;
        }

        public static Dictionary<string, string> ParseTxtRecord(string txtRecord) {
            var result = new Dictionary<string, string>();
            var parts = txtRecord.Split(new[] { '=' }, 2);
            if (parts.Length == 2) {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                result[key] = value;
            }
            return result;
        }
    }
}
