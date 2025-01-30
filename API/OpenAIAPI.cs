using SharperLLM.API;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using SharperLLM.Util;
using Newtonsoft.Json.Linq;

namespace SharperLLM.API
{
    /// <summary>
    /// example url: "http://api.openai.com/v1"
    /// </summary>
    public class OpenAIAPI(string url, string apiKey, string model, float temperature = 0.7f, int max_tokens = 8192) : iLLMAPI
    {
        public override string GenerateChatReply(PromptBuilder promptBuilder)
        {
            return Task.Run(async () =>
            {
                var result = new StringBuilder();
                await foreach (var item in GenerateChatReplyAsync(promptBuilder))
                {
                    result.Append(item);
                }
                return result.ToString();
            }).GetAwaiter().GetResult();
        }

        public override async IAsyncEnumerable<string> GenerateChatReplyAsync(PromptBuilder promptBuilder)
        {
            var targetURL = $"{url}/chat/completions";
            var messages = promptBuilder.Messages.Select(m => new { role = m.Item2.ToString(), content = m.Item1 }).ToArray();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

                var request = new HttpRequestMessage(HttpMethod.Post, targetURL)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        model = model,
                        messages = messages,
                        stream = true
                    }), Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }

                    // Read the response body as a stream.
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Handle SSE data lines, which look like: "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"text\"},\"finish_reason\":null}],\"object\":\"chat.completion.chunk\",\"created\":1677652943,\"model\":\"gpt-3.5-turbo-0613\"}"
                            var match = Regex.Match(line, @"^data: (.*)$");
                            if (match.Success)
                            {
                                var json = match.Groups[1].Value;

                                dynamic? data = 1;
                                try
                                {
                                    data = JsonConvert.DeserializeObject(json);
                                }
                                catch
                                {
                                    if (json == "[DONE]") break;
                                    else throw;
                                }

                                if(data != null)
                                foreach (var choice in data.choices)
                                {
                                    if (choice.delta != null && choice.delta.content != null)
                                    {
                                        yield return choice.delta.content;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

		[Obsolete("对于新版本的OpenAI模型，这个接口无效，仅用于使用老版本OpenAI接口的API")]
		public override string GenerateText(string prompt, int retry = 0)
		{
			var uri = new Uri(url);
			var requestBody = new
			{
				model,
				prompt,
				max_tokens,
				temperature
			};

			HttpClient client = new();
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

			var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			var response =  client.PostAsync(uri, content).Result;
			var responseString = response.Content.ReadAsStringAsync().Result;

			if (response.IsSuccessStatusCode)
			{
				JObject jsonResponse = JObject.Parse(responseString);
				return jsonResponse["choices"][0]["text"].ToString();
			}
			else
			{
				throw new Exception($"Error calling OpenAI API: {responseString}");
			}
		}
	}
}
