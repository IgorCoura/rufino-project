namespace PeopleManagement.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public List<ErrorModel> Errors { get; set; } = new List<ErrorModel>();
        public DomainException() { }
        public DomainException(ErrorModel error) {
            Errors.Add(error);
        }
        public DomainException(List<ErrorModel> errors)
        {
            Errors.AddRange(errors);
        }
    }
}
