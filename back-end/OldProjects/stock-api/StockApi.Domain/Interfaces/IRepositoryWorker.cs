using StockApi.Domain.Entities;
using StockApi.Domain.SeedWork;

namespace StockApi.Domain.Interfaces
{
    public interface IRepositoryWorker : IRepository
    {
        public Worker Save(Worker obj);
        public Worker Update(Worker obj);
        public Worker Remove(Worker obj);
        public Worker GetById(Guid id);
        public IEnumerable<Worker> GetAll(Func<Worker, bool> predicate);
    }
}
