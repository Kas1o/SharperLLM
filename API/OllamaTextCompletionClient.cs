using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharperLLM.Util;

namespace SharperLLM.API
{
    public class OllamaTextCompletionClient(string uri, string modelName) : ITextCompletionClient
    {
        public class OllamaConf// request parameters
        {
            public string model;
            public string prompt;//will be override by generation func
            public Options options = new();

            public class Options
            {
                public int num_ctx = 4096;
            }
        }
        public OllamaConf conf = new()
        {
            model = modelName
        };
        public async Task<string> GenerateAsync(string prompt)
        {
            var result = new StringBuilder();
            await foreach (var item in GenerateStreamAsync(prompt, default))
            {
                result.Append(item);
            }
            return result.ToString();
        }
        public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            conf.prompt = prompt;

            HttpClient client = new();
            client.BaseAddress = new Uri(uri + "api/generate");

            var request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress)
            {
                Content = new StringContent(JsonConvert.SerializeObject(conf,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }), Encoding.UTF8, "application/json")
            };
            //

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            await using var stream = await response.Content.ReadAsStreamAsync();

            _ = response.Content.Headers.TryGetValues("Content-Type", out var contentTypes);

            if (contentTypes.Any(x => x.StartsWith("application/x-ndjson")))
            {
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    var data = JsonConvert.DeserializeObject<dynamic>(line);
                    if (data["done"] == "true") yield break;
                    yield return data["response"];
                }
            }

        }
    }
}
