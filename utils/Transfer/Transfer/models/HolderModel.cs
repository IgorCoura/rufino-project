using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Transfer.models
{
    public class HolderModel
    {

        public HolderModel(string name, DocumentModel document)
        {
            Name = name;
            Document = document;
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("document")]
        public DocumentModel Document { get; set; }
    }
}
