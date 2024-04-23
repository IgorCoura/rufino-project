namespace PeopleManagement.Domain.Exceptions
{
    public class ErrorModel
    {
        public ErrorModel(string code, string msg)
        {
            Code = code;
            Message = msg;
        }

        public string Code { get; private set; }
        public string Message { get; private set; }

        protected static bool EqualOperator(ErrorModel left, ErrorModel right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        protected static bool NotEqualOperator(ErrorModel left, ErrorModel right)
        {
            return !(EqualOperator(left, right));
        }

        protected IEnumerable<object> GetEqualityComponents()
        {
            yield return Code;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ErrorModel)obj;

            return this.GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }

    }
}
