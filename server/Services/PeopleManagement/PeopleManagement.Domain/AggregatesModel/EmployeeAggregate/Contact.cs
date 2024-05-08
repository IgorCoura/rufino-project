using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed partial class Contact : ValueObject
    {
        private string _email = string.Empty;
        private string _cellphone = string.Empty;

        public string Email 
        { 
            get => _email; 
            private set
            {
                if (!MailAddress.TryCreate(value, out _))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldIsFormatInvalid(nameof(Email)));

                _email = value;
            }
        }
        public string CellPhone 
        { 
            get => _cellphone;
            private set
            {
                var number = value.ToUpper().Trim();
                number = number.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");

                if (string.IsNullOrWhiteSpace(number))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(CellPhone)));

                if (!CellPhonerRegex().IsMatch(number))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldIsFormatInvalid(nameof(CellPhone)));

                _cellphone = number;
            }
        }

        private Contact(string email, string phone)
        {
            Email = email;
            CellPhone = phone;
        }

        public static Contact Create(string email, string phone)
        {
            return new(email, phone);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Email; 
            yield return CellPhone;
        }

        [GeneratedRegex(@"^[0-9]{2}[0-9]{9}")]
        private static partial Regex CellPhonerRegex();

    }
}
