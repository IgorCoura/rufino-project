
using FluentValidation.Results;
using System.ComponentModel.DataAnnotations;

namespace BuildManagement.Domain.Exceptions
{
    public class BadRequestException : Exception
    {
        public List<ApiValidationError> Errors { get; set; } = new List<ApiValidationError>();

        public BadRequestException(int erroCode, string errorMessage) 
        {
            Errors.Add(new ApiValidationError(erroCode, errorMessage));
        }

        public BadRequestException(IEnumerable<ValidationFailure> errorMessage)
        {
            Errors.AddRange(errorMessage.Select(x => new ApiValidationError(1, x.ErrorMessage)));
        }
        public BadRequestException(Enum error)
        {
            Errors.Add(GetError(error));
        }
        public BadRequestException(Enum error, params string[] msgParms) 
        {
            var apiError = GetError(error);
            apiError.Msg = String.Format(apiError.Msg, msgParms);
            Errors.Add(apiError);
        }

        private ApiValidationError GetError(Enum error)
        {
            var enumType = typeof(ErrorsMessages);
            var memberInfos = enumType.GetMember(error.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo!.GetCustomAttributes(typeof(ApiErrorAttribute), false);
            var erroCode = ((ApiErrorAttribute)valueAttributes[0]).ErrorCode;
            var message = ((ApiErrorAttribute)valueAttributes[0]).ErrorMsgTemplate;
            return new ApiValidationError(erroCode, message);
        }
    }
}
