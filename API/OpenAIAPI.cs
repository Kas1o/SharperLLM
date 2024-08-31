using SharperLLM.API;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using SharperLLM.Util;

namespace SharperLLM.API
{
    /// <summary>
    /// example url: "http://api.openai.com/v1"
    /// </summary>
    public class OpenAIAPI(string url, string apiKey, string model) : iLLMAPI
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
    }
}
