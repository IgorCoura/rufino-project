using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Transfer.models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CategoryEnum
    {
        PAYROLL,
        BONUS,
        UTILITIES,
        RENT,
        FURNITURE_EQUIPMENT,
        OFFICE_SUPPLIES,
        MARKETING,
        SOFTWARE_IT,
        TRAVEL_MEAL,
        TRANSPORTATION,
        BENEFITS,
        TAXES,
        BUSINESS_SERVICES,
        EDUCATION,
        ENTERTAINMENT

    }
}
