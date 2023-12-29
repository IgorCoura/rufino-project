namespace PeopleManagement.Application.Commands.Identified
{
    public class IdentifiedCommandHandler<T, R> : IRequestHandler<IdentifiedCommand<T, R>, R>
    where T : IRequest<R> 
    {
        private IList<IdentifiedCommand<T, R>> _commands;
        private readonly IMediator _mediator;
        private readonly ILogger<IdentifiedCommandHandler<T, R>> _logger;

        public IdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<T, R>> logger)
        {
            _commands = new List<IdentifiedCommand<T, R>>();
            _mediator = mediator;
            _logger = logger;
        }

        protected virtual R CreateResultForDuplicateRequest()
        {
            return default!;
        }

        public async Task<R> Handle(IdentifiedCommand<T, R> request, CancellationToken cancellationToken)
        {
            var alreadyExists = _commands.Any(x => x.Id == request.Id);
            if (alreadyExists)
            {
                return CreateResultForDuplicateRequest();
            }

            _commands.Add(request);

            var command = request.Command;

            var result = await _mediator.Send(command, cancellationToken);

            return result;
        }
    }
}
