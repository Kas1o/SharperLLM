using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMSharp.API
{
    public class iLLMAPI
    {
        public virtual async IAsyncEnumerable<string> GenerateTextAsync(string prompt)
        {
            throw new NotImplementedException();
            yield return "";
        }
        public virtual string GenerateText(string prompt)
        {
            throw new NotImplementedException();
        }
    }
}
