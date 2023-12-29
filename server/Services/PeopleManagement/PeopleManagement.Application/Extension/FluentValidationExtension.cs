using FluentValidation;
using FluentValidation.Results;
using PeopleManagement.Domain.Exceptions;
namespace PeopleManagement.Application.Extension
{
    public static class FluentValidationExtension
    {
        public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, ErrorModel errorModel)
        {
            rule.WithErrorCode(errorModel.Code);
            rule.WithMessage(errorModel.Message);

            return rule;
        }

        public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Func<T, ErrorModel> messageProvider)
        {
            DefaultValidatorOptions.Configurable(rule).Current.SetErrorMessage((ctx, val) => {
                var result = messageProvider(ctx.InstanceToValidate);
                rule.WithErrorCode(result.Code);
                return result.Message;
            });
            return rule;
        }
        public static List<ErrorModel> GetErrors(this ValidationResult validationResult)
        {
            var result = validationResult.Errors.Select(x => new ErrorModel(x.ErrorCode, x.ErrorMessage)).ToList();
            return result;
        }
    }
}
