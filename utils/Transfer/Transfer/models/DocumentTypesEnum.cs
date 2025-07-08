using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Transfer.models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DocumentTypesEnum
    {
        CPF,
        CNPJ
    }
}
