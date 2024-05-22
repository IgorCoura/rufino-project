using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public sealed partial class Contact : ValueObject
    {
        public const int MAX_PHONE = 15;
        public const int MAX_EMAIL = 100;

        private string _email = string.Empty;
        private string _phone = string.Empty;

        public string Email 
        { 
            get => _email; 
            private set
            {
                if (!MailAddress.TryCreate(value, out _))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldIsFormatInvalid(nameof(Email)));

                if (value.Length > MAX_EMAIL)
                    throw new DomainException(this, DomainErrors.FieldCannotBeLarger(nameof(Email), MAX_EMAIL));

                _email = value;
            }
        }
        public string Phone 
        { 
            get => _phone;
            private set
            {
                var charValue = value.Select(x => char.IsDigit(x) ? x : ' ').ToArray();
                var number = new string(charValue).Replace(" ", "");

                if (string.IsNullOrWhiteSpace(number))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Phone)));

                if (!PhonerRegex().IsMatch(number))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldIsFormatInvalid(nameof(Phone)));

                if (number.Length > MAX_PHONE)
                    throw new DomainException(this, DomainErrors.FieldCannotBeLarger(nameof(Phone), MAX_PHONE));

                _phone = number;
            }
        }

        private Contact(string email, string phone)
        {
            Email = email;
            Phone = phone;
        }

        public static Contact Create(string email, string phone)
        {
            return new(email, phone);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Email; 
            yield return Phone;
        }

        [GeneratedRegex(@"^[0-9]{2}[0-9]{9}|[0-9]{8}")]
        private static partial Regex PhonerRegex();

    }
}
