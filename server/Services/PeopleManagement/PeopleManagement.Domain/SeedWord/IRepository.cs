namespace PeopleManagement.Domain.SeedWord;

public interface IRepository<T> where T : IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}
