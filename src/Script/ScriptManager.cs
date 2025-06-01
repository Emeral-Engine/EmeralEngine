using EmeralEngine.Resource.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmeralEngine.Script
{
    public class ScriptInfo
    {
        public string processed_script, speaker;
        public List<string> charas;
        public ScriptInfo()
        {
            charas = new();
        }
        public string script
        {
            set
            {
                processed_script = value.Replace(":script", @"\:script");
            }
            get
            {
                if (string.IsNullOrEmpty(processed_script))
                {
                    return "";
                }
                else
                {
                    return processed_script.Replace(@"\:script", ":script");
                }
            }
        }
    }
}
