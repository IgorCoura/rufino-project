using StockApi.Domain.Entities;
using StockApi.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Service.Services
{
    public class WorkerService: IServiceWorker
    {
        private readonly IRepositoryWorker _repositoryWorker;

        public WorkerService(IRepositoryWorker repositoryWorker)
        {
            _repositoryWorker = repositoryWorker;
        }

        public IEnumerable<Worker> RecoverAll(DateTime modificationDate)
        {
            return _repositoryWorker.GetAll(x => x.ModificationDate > modificationDate);
        }
    }
}
