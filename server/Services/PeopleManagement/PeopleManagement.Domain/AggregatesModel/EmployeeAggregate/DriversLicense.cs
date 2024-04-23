using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public class DriversLicense : ValueObject
    {
        public const int MAX_LENGHT = 11;

        public const int MAX_YEARS_VALIDITY = 10;

        private string _registerNumber = string.Empty;
        private CategoryDriversLicense[] _categories = [];
        private DateOnly _validity;
        private File[] _files = [];

        public string RegisterNumber
        {
            get => _registerNumber;
            private set
            {
                value = value.ToUpper().Trim();
                value = value.Replace(".", "").Replace("-", "").Replace("/", "").Replace(" ", "");
                Validate(value);
                _registerNumber = value;
            } 
        }
        public CategoryDriversLicense[] Categories
        { 
            get => _categories;
            private set
            {
                if (value.Length <= 0)
                    throw new DomainException(DomainErrors.ListNotBeNullOrEmpty(nameof(Categories)));
                _categories = _categories.Distinct().ToArray();
            }
        }
        public DateOnly Validity 
        { 
            get => _validity;
            private set
            {
                var minDate = DateOnly.FromDateTime(DateTime.UtcNow);
                var maxDate = minDate.AddYears(MAX_YEARS_VALIDITY);
                if (value < minDate || value > maxDate)
                    throw new DomainException(DomainErrors.DataHasBeBetween(nameof(Validity), value, minDate, maxDate));
                _validity = value;
            }
        }

        public File[] Files 
        {
            get => _files;
            private set
            {
                _files = value;
                _files = _files.Distinct().ToArray();
            }
        }

        private DriversLicense(string registerNumber, CategoryDriversLicense[] categories, DateOnly validity)
        {
            RegisterNumber = registerNumber;
            Categories = categories;
            Validity = validity;
        }

        private DriversLicense(string registerNumber, CategoryDriversLicense[] categories, DateOnly validity, File[] files)
        {
            RegisterNumber = registerNumber;
            Categories = categories;
            Validity = validity;
            Files = files;
        }

        public static DriversLicense Create(string registerNumber, DateOnly validity, params CategoryDriversLicense[] categories) => new(registerNumber, categories, validity);

        public DriversLicense AddFile(File file)
        {
            File[] files = [.. Files, file];
            return new(RegisterNumber, Categories, Validity, files);
        }

        private void Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException(DomainErrors.FieldNotBeNullOrEmpty(nameof(RegisterNumber)));
            }

            string[] invalidForm = ["00000000000", "11111111111", "22222222222", "33333333333", "44444444444", "55555555555", "66666666666", "77777777777", "88888888888", "99999999999"];
            string aux;
            string digit;
            int sum, rest, increment, multiplier, digitNumber;


            if (value.Length != MAX_LENGHT)
            {
                throw new DomainException((DomainErrors.FieldCannotBeLarger(nameof(RegisterNumber), MAX_LENGHT)));
            }

            foreach (string item in invalidForm)
            {
                if (item == value)
                {
                    throw new DomainException(DomainErrors.FieldInvalid(nameof(RegisterNumber), value));
                }
            }

            aux = value[..9];
            sum = 0;
            increment = 0;
            multiplier = 9;

            for (int i = 0; i < 9; i++)
            {
                sum += int.Parse(aux[i].ToString()) * multiplier;
                multiplier -= 1;
            }
                

            rest = sum % 11;

            if (rest == 10)
                increment -= 2;

            if (rest > 9)
                rest = 0;

            digit = rest.ToString();

            aux = value[..9];
            sum = 0;
            multiplier = 1;
            digitNumber = 0;

            for (int i = 0; i < 9; i++)
            {
                sum += int.Parse(aux[i].ToString()) * multiplier;
                multiplier += 1;
            }
                

            rest = sum % 11;

            if (rest + increment < 0)
                digitNumber = 11 + rest + increment;

            if (rest + increment >= 0)
                digitNumber = rest + increment;

            if(digitNumber > 9)
                digitNumber = 0;

            digit += digitNumber.ToString();

            if (!value.EndsWith(digit))
                throw new DomainException(DomainErrors.FieldInvalid(nameof(RegisterNumber), value));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return RegisterNumber; 
            yield return Validity;
            foreach (var category in Categories)
            {
                yield return category;
            }
            foreach (var file in Files)
            {
                yield return file;
            }
        }
    }
}
