using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace SharperLLM.API
{
    public class KoboldAPI : iLLMAPI
    {
        public class KoboldAPIConf
        {
            public int? max_context_length = 4096;
            public int? max_length = 1024;
            public float? rep_pen = 1.15f;
            public int? rep_pen_range = 512;
            public int? sampler_seed;
            public string[]? stop_sequence = { "</s>", "<|im_end|>" };
            public float? temperature = 0.7f;
            public float? tfs;//无尾
            public float? top_a = 0;
            public int? top_k = 20;
            public float? top_p;
            public float? min_p;
            public float? typical = 1;//典型
            public float? dynatemp_range = 0;
            public float? smoothing_factor = 0;
            public float? dynatemp_exponent = 1;
            public int? mirostat = 0;
            public int? mirostat_tau = 5;
            public float? mirostat_eta = 0.46f;
            public string? genkey;
            public string? grammar;
            public string? images;//base64
            public bool? trim_stop;
            public bool? bypass_eos;
            public string[]? banned_tokens;
            public Dictionary<int, float>? logit_bias;// token id => bias.
            public string prompt;// modify on GenerateFunc
        }
        public KoboldAPIConf conf = new();

        public Uri _uri;
        public string uri
        {
            get => _uri.AbsoluteUri;
            set
            {
                //validate
                _uri = CreateLooseUri(value);
            }
        }

        public KoboldAPI(string uri, KoboldAPIConf conf)
        {
            this.conf = conf;
            _uri = CreateLooseUri(uri);
        }
        public KoboldAPI(string uri)
        {
            _uri = CreateLooseUri(uri);
        }

        public override async IAsyncEnumerable<string> GenerateTextAsync(string prompt)
        {
            conf.prompt = prompt;
            var client = new HttpClient();

            client.BaseAddress = new Uri( uri + "/extra/generate/stream");

            var request = new HttpRequestMessage(HttpMethod.Post, uri + "/extra/generate/stream")
            {
                Content = new StringContent(JsonConvert.SerializeObject(conf, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            await using var stream = await response.Content.ReadAsStreamAsync();

            _ = response.Content.Headers.TryGetValues("Content-Type", out var contentTypes);

            if (contentTypes.Any(x => x.StartsWith("text/event-stream")))
            {
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (line.StartsWith("data:"))
                    {
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(line.Substring(5));
                        yield return data["token"];
                    }
                }
            }
        }
        public override string GenerateText(string prompt)
        {
            conf.prompt = prompt;
            var client = new HttpClient();
            client.BaseAddress = new Uri(uri + "/v1/generate");

            var request = new HttpRequestMessage(HttpMethod.Post, uri+ "/v1/generate")
            {
                Content = new StringContent(JsonConvert.SerializeObject(conf, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                                            Encoding.UTF8, "application/json")
            };


            try
            {
                // 发送请求并获取响应
                var response = client.Send(request);
                response.EnsureSuccessStatusCode();

                // 读取响应内容
                string responseBody = response.Content.ReadAsStringAsync().Result;

                // 解析JSON响应，提取"text"字段  
                JObject json = JObject.Parse(responseBody);
                JArray results = (JArray)json["results"];
                if (results.Count > 0)
                {
                    string text = results[0]["text"].ToString();
                    return text; // 直接返回提取的文本内容
                }
                else
                {
                    throw new ApplicationException("No 'results' found in the response.");
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                // 将503服务繁忙异常直接抛出
                throw;
            }
            catch (Exception ex)
            {
                // 将其他异常也直接抛出
                throw new ApplicationException($"An error occurred while processing the request:{ex.Message}", ex);
            }
        }
        private static Uri CreateLooseUri(string looseUriString)
        {
            looseUriString = looseUriString.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(looseUriString))
            {
                throw new ArgumentException("URI string cannot be null, empty, or whitespace.", nameof(looseUriString));
            }

            // 检查是否已包含http://或https://，如果没有则添加http://
            if (!looseUriString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !looseUriString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                looseUriString = "http://" + looseUriString;
            }

            Uri uri;
            if (Uri.TryCreate(looseUriString, UriKind.Absolute, out uri))
            {
                return uri;
            }
            else
            {
                throw new ArgumentException($"The provided string '{looseUriString}' could not be parsed into a valid URI.");
            }
        }
    }
}