using BuildManagement.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class ValidationService : IValidationService
    {
        public IEnumerable<ValidationResult> ValidateModel(object obj)
        {
            ICollection<ValidationResult> validate = new List<ValidationResult>();
            ValidationContext context = new ValidationContext(obj);
            Validator.TryValidateObject(obj, context, validate, true);
            return validate;
        }
    }
}
