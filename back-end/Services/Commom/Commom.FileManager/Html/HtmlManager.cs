using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Commom.FileManager.Html
{
    public class HtmlManager
    {
        public static Task<string> InsertValuesInHtmlTemplate(Dictionary<string, dynamic> values, string pathHtmlTemplate)
        {
            using StreamReader sr = new StreamReader(pathHtmlTemplate);
            string html = sr.ReadToEnd();

            Regex regex = new Regex("{{(.*?)}}");
            string result = regex.Replace(html, m => {

                string key = m.Groups[1].Value;

                string[] listKey = key.Split('.');
                
                var tempValues = values;
                
                foreach (string item in listKey)
                {
                    if (tempValues.ContainsKey(item) is false)
                        break;
                    
                    if (tempValues[item] is not Dictionary<string, dynamic>)
                    {
                        return tempValues[item];
                    }

                    tempValues = tempValues[item];      
                }

                return m.Value;
            });

            return Task.FromResult(result);
        }
    }
}

