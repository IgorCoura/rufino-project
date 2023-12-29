namespace PeopleManagement.Domain.Exceptions
{
    public class ErrorModel
    {
        public ErrorModel(string code, string msg)
        {
            Code = code;
            Message = msg;
        }

        public string Code { get; set; }
        public string Message { get; set; }
    }
}
