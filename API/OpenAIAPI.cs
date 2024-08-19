using LLMSharp.API;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace SharperLLM.API
{
    /// <summary>
    /// example url: "http://api.openai.com/v1"
    /// </summary>
    public class OpenAIAPI(string url, string apiKey, string model) : iLLMAPI
    {
        public override string GenerateChatReply(PromptBuilder promptBuilder)
        {
            var targetURL = $"{url}/chat/completions";
            var messages = promptBuilder.Messages.Select(m => new { role = m.Item1.ToString(), content = m.Item2 }).ToArray();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var request = new
                {
                    model = model,
                    messages = messages
                };

                var jsonRequest = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(targetURL, content).Result;//System.Net.Http.HttpRequestException:“The SSL connection could not be established, see inner exception.” inner: AuthenticationException: Cannot determine the frame size or a corrupted frame was received.

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                    return jsonResponse.choices[0];
                }
                else
                {
                    throw new Exception($"{response.StatusCode}");
                }
            }
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
                        Debug.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        yield break;
                    }

                    // Read the response body as a stream.
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Handle SSE data lines, which look like: "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"text\"},\"finish_reason\":null}],\"object\":\"chat.completion.chunk\",\"created\":1677652943,\"model\":\"gpt-3.5-turbo-0613\"}"
                            var match = Regex.Match(line, @"^data: (.*)$");
                            if (match.Success)
                            {
                                var json = match.Groups[1].Value;

                                dynamic data = 1;
                                try
                                {
                                    data = JsonConvert.DeserializeObject(json);
                                }
                                catch(Exception e)
                                {
                                    if (json == "[DONE]") break;
                                    else throw e;
                                }

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
