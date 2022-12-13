using StockApi.Domain.Seedwork;

namespace StockApi.Domain.SeedWork
{
    public interface IRepository
    {
        IUnitOfWork UnitOfWork { get; }
    }
}
