namespace PeopleManagement.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public List<ErrorModel> Errors { get; } = new List<ErrorModel>();
        public DomainException() { }
        public DomainException(ErrorModel model) {
            Errors.Add(model);
        }
        public void AddError(ErrorModel model)
        {

            Errors.Add(model);
        }

        public void AddErrors(List<ErrorModel> models)
        {

            Errors.AddRange(models);
        }

        public bool HasErrors => Errors.Count > 0;
        
    }
}
