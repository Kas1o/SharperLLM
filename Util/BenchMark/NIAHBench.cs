using SharperLLM.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util.BenchMark;

public class NIAHBench
{
    private ILLMAPI _api;
    private int _textLength;
    private int _interval;
    private int _repetitions;
    private string _hiddenString;
    private string _paddingString;
    private PromptBuilder _pb;

    public NIAHBench(ILLMAPI api, int textLength, int interval, PromptBuilder pb, int repetitions = 1, string hiddenString = null, string paddingString  = null)
    {
        _api = api;
        _textLength = textLength;
        _interval = interval;
        _repetitions = repetitions;
        _hiddenString = hiddenString??GenerateRandomSentence();
        _pb = pb;

        // 预处理padding string
        if (paddingString != null)
        {
            while(paddingString.Length < textLength)
            {
                paddingString += paddingString;
            }

            paddingString = paddingString.Substring(0, textLength);
        }
        _paddingString = paddingString;
    }

    private string GenerateRandomSentence()
    {
        // 生成随机的具备信息的句子
        return $"{Guid.NewGuid().ToString("N").Substring(0, 10)}";
    }

    private string GenerateIrrelevantText(int length)
    {
        // 生成指定长度的无关文本
        StringBuilder sb = new StringBuilder(length);
        Random rand = new Random();
        for (int i = 0; i < length; i++)
        {
            sb.Append((char)('a' + rand.Next(26)));
        }
        return sb.ToString();
    }

    public async Task<List<(int index, float Acc)>> RunTestAsync()
    {
        List<(int index, float Acc)> results = new List<(int index, float Acc)>();

        for (int position = 0; position < _textLength; position += _interval)
        {
            int correctAnswers = 0;

            for (int rep = 0; rep < _repetitions; rep++)
            {
                string irrelevantText = _paddingString?? GenerateIrrelevantText(position);
                string targetText = irrelevantText.Insert(new Random().Next(irrelevantText.Length), "【关键信息："+ _hiddenString+"】");

                _pb.System = "你是一个AI助手，下面请仔细阅读以下文本，并回答问题";
                _pb.Messages = [
                    (targetText,PromptBuilder.From.system),
                    ("上面的文本中隐藏着被【】符号框住的关键信息，请从上面的文本中找到【关键信息】框中包含的内容。并且告诉我。",PromptBuilder.From.user),
                    ];

                var prompt = _pb.GetResult();
                prompt +="\n好的,被隐藏的关键信息的内容是：";
                string response = string.Empty;
                await foreach (var token in _api.GenerateTextStream(prompt, default))
                {
                    response += token;
                }

                // 假设模型返回的信息是正确的
                if (response.Contains(_hiddenString))
                {
                    correctAnswers++;
                }
            }

            float accuracy = (float)correctAnswers / _repetitions;
            results.Add((position, accuracy));
        }

        return results;
    }
}
