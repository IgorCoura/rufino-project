using FluentValidation;
using FluentValidation.Results;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Application.Extension
{
    public static class FluentValidationExtension
    {
        public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Error errorModel)
        {
            rule.WithErrorCode(errorModel.Code);
            rule.WithMessage(errorModel.Message);

            return rule;
        }

        public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Func<T, Error> messageProvider)
        {
            DefaultValidatorOptions.Configurable(rule).Current.SetErrorMessage((ctx, val) => {
                var result = messageProvider(ctx.InstanceToValidate);
                rule.WithErrorCode(result.Code);
                return result.Message;
            });
            return rule;
        }
        public static List<Error> GetErrors(this ValidationResult validationResult)
        {
            var result = validationResult.Errors.Select(x => new Error(x.ErrorCode, x.ErrorMessage, new {})).ToList();
            return result;
        }
    }
}
