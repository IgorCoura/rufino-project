using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Exceptions;
namespace PeopleManagement.Tests.UnitTests.Aggregates.EmployeeTests
{
    public class SocialIntegrationProgramTests
    {
        [Theory]
        [InlineData("07183177441")]
        [InlineData("873.58571.07-9")]
        [InlineData("235.01965.95-4")]
        [InlineData("679.556-24321")]
        public void CreateValidSocialIntegrationProgram(string value)
        {
            //Act
            SocialIntegrationProgram pis = value;
            //Assert
            Assert.True(true);
        }

        [Theory]
        [InlineData("07184177441")]
        [InlineData("873.58571.07-1")]
        [InlineData("235.01365.96-4")]
        [InlineData("679.556-24329")]
        public void CreateInvalidSocialIntegrationProgram(string value)
        {
            //Act
            DomainException ex = Assert.Throws<DomainException>(() =>
            {
                SocialIntegrationProgram pis = value;
            });

            //Assert
            Assert.Equal(ex.Errors.First(), DomainErrors.FieldInvalid(nameof(SocialIntegrationProgram), value));
        }

    }
}
