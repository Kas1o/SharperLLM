﻿using SharperLLM.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API
{
    public class iLLMAPI//为啥不是接口? 因为奇怪的编译检查。
    {
        public virtual async IAsyncEnumerable<string> GenerateTextAsync(string prompt)
        {
            throw new NotImplementedException();
            yield return "";//应对奇怪的编译检查。必须写一个。
        }
        public virtual string GenerateText(string prompt, int retry = 0)
        {
            throw new NotImplementedException();
        }
        public virtual async IAsyncEnumerable<string> GenerateChatReplyAsync(PromptBuilder promptBuilder)
        {
            throw new NotImplementedException();
            yield return "";//应对奇怪的编译检查。必须写一个。
        }
        public virtual string GenerateChatReply(PromptBuilder promptBuilder)
        {
            throw new NotImplementedException();
        }
    }
}
