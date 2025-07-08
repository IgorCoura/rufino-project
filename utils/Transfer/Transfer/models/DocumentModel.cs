using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Transfer.models
{
    public class DocumentModel
    {
        public DocumentModel(string identity, DocumentTypesEnum type)
        {
            Identity = identity;
            Type = type;
        }

        [JsonPropertyName("identity")]
        public string Identity { get; set; }

        [JsonPropertyName("type")]
        public DocumentTypesEnum Type { get; set; }

    }
}
