using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.Identified
{
    public class IdentifiedCommandHandler<T, R>(IMediator mediator, ILogger<IdentifiedCommandHandler<T, R>> logger, IRequestManager requestManager) : IRequestHandler<IdentifiedCommand<T, R>, R>
    where T : IRequest<R> 
    {
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<IdentifiedCommandHandler<T, R>> _logger = logger;  
        private readonly IRequestManager _requestManager = requestManager;

        protected virtual R CreateResultForDuplicateRequest()
        {
            return default!;
        }

        public async Task<R> Handle(IdentifiedCommand<T, R> request, CancellationToken cancellationToken)
        {
            var alreadyExists = await _requestManager.ExistAsync(request.Id);
            if (alreadyExists)
            {
                _logger.LogInformation("Request {Id} already exists", request.Id);
                return CreateResultForDuplicateRequest();
            }

            await _requestManager.CreateRequestForCommandAsync<T>(request.Id);

            _logger.LogInformation("Request {Id} created", request.Id);

            var command = request.Command;

            var result = await _mediator.Send(command, cancellationToken);

            return result;
        }
    }
}
