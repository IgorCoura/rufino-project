using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.FileManager.Html
{
    public class HtmlContent
    {
        public HtmlContent(string header, string pathFileHtmlBody, string footer)
        {
            Header = header;
            PathFileHtmlBody = pathFileHtmlBody;
            Footer = footer;
        }

        public string Header { get; set; }
        public string PathFileHtmlBody { get; set; }
        public string Footer { get; set; }
    }
}
