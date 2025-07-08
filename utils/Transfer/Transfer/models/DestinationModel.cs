using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Transfer.models
{
    public class DestinationModel
    {
        public DestinationModel(string bankCode, string accountNumber, string branchNumber, HolderModel holder, AccountTypesEnum accountType)
        {
            BankCode = bankCode;
            AccountNumber = accountNumber;
            BranchNumber = branchNumber;
            Holder = holder;
            AccountType = accountType;
        }

        [JsonPropertyName("bank_code")]
        public string BankCode { get; set; } = string.Empty;

        [JsonPropertyName("account_number")]
        public string AccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("branch_number")]
        public string BranchNumber { get; set; } = string.Empty;

        [JsonPropertyName("holder")]
        public HolderModel Holder { get; set; }

        [JsonPropertyName("account_type")]
        public AccountTypesEnum AccountType { get; set; }
    }
}
