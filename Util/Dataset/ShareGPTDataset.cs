namespace SharperLLM.Util.Dataset
{
    public class ShareGPTDatasetConversationTerm
    {
        public string from { get; set; }
        public string value { get; set; }
    }
    public class ShareGPTDatasetTerm
    {
        public string system { get; set; }
        public List<ShareGPTDatasetConversationTerm> conversations { get; set; } = new();

        public void WriteToPromptBuilder(PromptBuilder originPromptBuilder)
        {
            originPromptBuilder.System = system;
            originPromptBuilder.Messages = conversations.Select(selector: x => (x.value, x.from switch
            {
                "system" => PromptBuilder.From.system,
                "assistant" => PromptBuilder.From.assistant,
                "user" => PromptBuilder.From.user,
                "gpt" => PromptBuilder.From.assistant,
                "human" => PromptBuilder.From.user,
                _ => throw new Exception(message: $"Unknown From: {x.from}")
            })).ToArray();
        }

        public static ShareGPTDatasetTerm CreateFromPromptBuilder(PromptBuilder pb, string systemTag = "system", string assitantTag = "gpt", string userTag = "human")
        {
            var term = new ShareGPTDatasetTerm();
            term.system = pb.System;
            foreach (var item in pb.Messages)
            {
                term.conversations.Add(new ShareGPTDatasetConversationTerm{
                    from = item.Item2 switch 
                    {
                        PromptBuilder.From.system => systemTag,
                        PromptBuilder.From.assistant => assitantTag,
                        PromptBuilder.From.user => userTag
                    },
                    value = item.Item1
                });
            }
            return term;
        }
    }
}
