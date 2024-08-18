using LLMSharp.API;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API
{
    /// <summary>
    /// example url: "https://api.openai.com/v1"
    /// </summary>
    public class OpenAIAPI(string url, string apiKey, string model) : iLLMAPI
    {
        public async IAsyncEnumerable<string> GenerateChatReply(PromptBuilder promptBuilder)
        {
            var targetURL = $"{url}/chat/completions";
            var messages = promptBuilder.Messages.Select(m => new { role = m.Item1.ToString(), content = m.Item2 }).ToArray();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                var request = new
                {
                    model = model,
                    messages = messages
                };

                var jsonRequest = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(targetURL, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                    foreach (var choice in jsonResponse.choices)
                    {
                        yield return choice.message.content;
                    }
                }
                else
                {
                    Debug.WriteLine($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
            }
        }
    }
}
