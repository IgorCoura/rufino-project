using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Tests.UnitTests.Aggregates.EmployeeTests
{
    public class VoteIdTests
    {
        [Theory]
        [InlineData("281662310124",  "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        [InlineData("4821.82-15.106 6",  "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        [InlineData("641723481023",  "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        [InlineData("1466042-51031",  "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        public void CreateValidVoteId(string number, string archiveId)
        {
            //Act
            var value  = VoteId.Create(
                    value: number,
                    archiveId: Guid.Parse(archiveId)
                );

            //Assert
            Assert.True(true);
        }

        [Theory]
        [InlineData("281664310124", "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        [InlineData("4821.82-15.102 6", "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        [InlineData("641723481029", "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        [InlineData("1466042-51032", "E04E0CA9-C757-4D73-A949-087AC168F61C")]
        public void CreateInvalidVoteId(string number, string archiveId)
        {
            //Act
            DomainException ex = Assert.Throws<DomainException>(() =>
            {
                var value = VoteId.Create(
                    value: number,
                    archiveId: Guid.Parse(archiveId)
                );
            });

            //Assert
            Assert.Equal(ex.Errors.First(), DomainErrors.FieldInvalid(nameof(VoteId), number));
        }

    }
}
