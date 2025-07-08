using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.UnitTests.Aggregates.EmployeeTests
{
    public class NameTests
    {
        [Theory]
        [InlineData("  Murilo Bernardo Farias ")]
        [InlineData("Kaique Hénry Giovânni da Crûz")]
        [InlineData("Kaique Henry")]
        public void CreateValidName(string value)
        {
            Name name = value;

            var expectValue = value.Trim().ToUpper();
            Assert.Equal(expectValue, name);
        }

        [Theory]
        [InlineData("  ")]
        [InlineData("Murilo")]
        [InlineData("Kaique H3e4nry")]
        [InlineData("Kaique H#enry")]
        public void CreateInvalidName(string value)
        {


            DomainException exception = Assert.Throws<DomainException>(() =>
            {
                Name name = value;
            });

            Assert.Equal(DomainErrors.FieldIsFormatInvalid(""), exception.Errors[nameof(Name)].FirstOrDefault());
        }
    }
}
