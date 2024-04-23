using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Tests.UnitTests.Aggregates.EmployeeTests
{
    public class DriversLicenseTests
    {
        [Theory]
        [InlineData("54627461810", "12/12/2024", "E04E0CA9-C757-4D73-A949-087AC168F61C", "A" ,"B" )]
        [InlineData("77757728853", "12/12/2025", "E04E0CA9-C757-4D73-A949-087AC168F61C", "A" ,"E" )]
        [InlineData("33509132160", "12/12/2025", "E04E0CA9-C757-4D73-A949-087AC168F61C", "C" ,"D" )]
        [InlineData("58101168932", "12/12/2025", "E04E0CA9-C757-4D73-A949-087AC168F61C", "B" ,"C" )]
        public void CreateValidDriversLicense(string registerNumber, string validity, string archiveId, params string[] categories)
        {
            //Act
            var value  = DriversLicense.Create(
                    registerNumber: registerNumber,
                    validity: DateOnly.Parse(validity),
                    archiveId: Guid.Parse(archiveId),
                    categories: categories.Select(c => CategoryDriversLicense.FromDisplayName<CategoryDriversLicense>(c)).ToArray()
                );

            //Assert
            Assert.True(true);
        }

        [Theory]
        [InlineData("54627461811", "12/12/2024", "E04E0CA9-C757-4D73-A949-087AC168F61C", "A", "B")]
        [InlineData("77757728852", "12/12/2025", "E04E0CA9-C757-4D73-A949-087AC168F61C", "A", "E")]
        [InlineData("33509132110", "12/12/2025", "E04E0CA9-C757-4D73-A949-087AC168F61C", "C", "D")]
        [InlineData("58101168934", "12/12/2025", "E04E0CA9-C757-4D73-A949-087AC168F61C", "B", "C")]
        public void CreateDriversLicenseWithInvalidRegisterNumber(string registerNumber, string validity, string archiveId, params string[] categories)
        {
            //Act
            DomainException ex = Assert.Throws<DomainException>(() =>
            {
                var value = DriversLicense.Create(
                    registerNumber: registerNumber,
                    validity: DateOnly.Parse(validity),
                    archiveId: Guid.Parse(archiveId),
                    categories: categories.Select(c => CategoryDriversLicense.FromDisplayName<CategoryDriversLicense>(c)).ToArray()
                );
            });

            //Assert
            Assert.Equal(ex.Errors.First(), DomainErrors.FieldInvalid(nameof(DriversLicense.RegisterNumber), registerNumber));
        }

    }
}
