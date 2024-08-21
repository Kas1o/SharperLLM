using Newtonsoft.Json;

namespace SharperLLM.Util.Dataset
{
    public static class DatasetUtil
    {
        public static List<ShareGPTDatasetTerm> LoadShareGPTDataset(string datasetContent)
        {
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
    }
}
