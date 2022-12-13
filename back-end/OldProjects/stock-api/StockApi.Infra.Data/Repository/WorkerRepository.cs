using StockApi.Domain.Entities;
using StockApi.Domain.Exceptions;
using StockApi.Domain.Interfaces;
using StockApi.Domain.Seedwork;
using StockApi.Infra.Data.Context;


namespace StockApi.Infra.Data.Repository
{
    public class WorkerRepository : IRepositoryWorker
    {
        private readonly StockApiContext _context;

        public IUnitOfWork UnitOfWork
        {
            get
            {
                return _context;
            }
        }

        public WorkerRepository(StockApiContext context)
        {
            _context = context;
        }

        public Worker Save(Worker obj)
        {
            return _context.Worker.Add(obj).Entity;
        }
        public Worker Update(Worker obj)
        {
            return _context.Worker.Update(obj).Entity;

        }
        public Worker Remove(Worker obj)
        {
            return _context.Worker.Remove(obj).Entity;
        }

        public Worker GetById(Guid id) =>
            _context.Worker.Where(w => w.Id == id).SingleOrDefault() ?? throw new DomainException("Worker id is invalid");

        public IEnumerable<Worker> GetAll(Func<Worker, bool> predicate) =>
             _context.Worker.Where(predicate);
    }
}