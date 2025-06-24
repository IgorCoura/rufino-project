using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;
using Address = PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Address;

namespace PeopleManagement.Application.Commands.WorkplaceCommands.CreateWorkplace
{

    public sealed class CreateWorkplaceCommandHandler(IWorkplaceRepository workplaceRepository) : IRequestHandler<CreateWorkplaceCommand, CreateWorkplaceResponse>
    {   
        private readonly IWorkplaceRepository _workplaceRepository = workplaceRepository;

        public async Task<CreateWorkplaceResponse> Handle(CreateWorkplaceCommand request, CancellationToken cancellationToken)
        {
            var workplace = Workplace.Create(
                Guid.NewGuid(), 
                request.Name,
                Address.Create(
                    request.Address.ZipCode,
                    request.Address.Street, 
                    request.Address.Number,
                    request.Address.Complement,
                    request.Address.Neighborhood,
                    request.Address.City, 
                    request.Address.State, 
                    request.Address.Country
                    ), 
                request.CompanyId);
            
            await _workplaceRepository.InsertAsync(workplace, cancellationToken);

            await _workplaceRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return workplace.Id;
        }
    }
    
    public class CreateWorkplaceIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateWorkplaceCommand, CreateWorkplaceResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateWorkplaceCommand, CreateWorkplaceResponse>(mediator, logger, requestManager)
    {
        protected override CreateWorkplaceResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}
