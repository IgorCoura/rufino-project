using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Transfer.models
{
    public class TransferModel
    {
        public TransferModel(DestinationModel destination, int amount, string description, string code, CategoryEnum category, string scheduled)
        {
            Destination = destination;
            Amount = amount;
            Description = description;
            Code = code;
            Category = category;
            Scheduled = scheduled;
        }

        [JsonPropertyName("destination")]
        public DestinationModel Destination{ get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public CategoryEnum Category { get; set; }

        [JsonPropertyName("scheduled")]
        public string Scheduled { get; set; } = string.Empty;



    }
}
