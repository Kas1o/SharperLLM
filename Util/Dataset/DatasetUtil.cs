using Newtonsoft.Json;

namespace SharperLLM.Util.Dataset
{
    public static class DatasetUtil
    {
        public static List<ShareGPTDatasetTerm> LoadShareGPTDataset(string datasetContent)
        {
            // todo :在这里判断类型: csv, json(l), parquet
            try
            {
                var shareGPTDatasetEntries = JsonConvert.DeserializeObject<List<ShareGPTDatasetTerm>>(datasetContent);
                return shareGPTDatasetEntries;
            }
            catch
            {
                throw;
            }
        }
        public static string SaveShareGPTDataset(List<ShareGPTDatasetTerm> dataset)
        {
            return JsonConvert.SerializeObject(dataset);
        }

        public static IEnumerable<string> LoadPretrainDataset(string datasetContent)
        {
            List<dynamic> obj = JsonConvert.DeserializeObject<List<dynamic>>(datasetContent);

            foreach(var item in obj)
            {
                yield return item.text;
            }
        }
        public static string SavePretrainDataset(IEnumerable<string> texts)
        {
            // 创建一个列表来存储动态对象
            var data = new List<dynamic>();

            // 遍历所有的文本并将它们添加到列表中
            foreach (var text in texts)
            {
                data.Add(new { text = text });
            }

            // 序列化列表为 JSON 字符串
            return JsonConvert.SerializeObject(data);
        }
    }
}
