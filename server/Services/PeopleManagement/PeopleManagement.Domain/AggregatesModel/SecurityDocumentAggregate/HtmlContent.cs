using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class HtmlContent : ValueObject
    {

        public string Header { get; private set; }
        public string Body { get; private set; }
        public string Footer { get; private set; }

        private HtmlContent(string header, string body, string footer)
        {
            Header = header;
            Body = body;
            Footer = footer;
        }

        public static HtmlContent Create(string header, string body, string footer) => new(header, body, footer);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Header;
            yield return Body;
            yield return Footer;
        }
    }
}
