using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.UnitTests.Aggregates.EmployeeTests
{
    public class VoteIdTests
    {
        [Theory]
        [InlineData("021874220116")]
        [InlineData("4821.82-15.106 6")]
        [InlineData("641723481023")]
        [InlineData("1466042-51031")]
        public void CreateValidVoteId(string number)
        {
            //Act
            var _ = VoteId.Create(
                    value: number
                );

            //Assert
            Assert.True(true);
        }

        [Theory]
        [InlineData("281664310124")]
        [InlineData("4821.82-15.102 6")]
        [InlineData("641723481029")]
        [InlineData("1466042-51032")]
        public void CreateInvalidVoteId(string number)
        {
            //Act
            DomainException ex = Assert.Throws<DomainException>(() =>
            {
                var value = VoteId.Create(
                    value: number
                );
            });

            //Assert
            Assert.Equal(ex.Errors[nameof(VoteId)].First(), DomainErrors.FieldInvalid(nameof(VoteId), number));
        }

    }
}
