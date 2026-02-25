using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;

namespace PeopleManagement.Services.Services
{
    public class RecurringDocumentService(
        IEmployeeRepository employeeRepository, 
        IServiceProvider serviceProvider,
        ILogger<RecurringDocumentService> logger) 
        : IRecurringDocumentService
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly ILogger<RecurringDocumentService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        [DisableConcurrentExecution(timeoutInSeconds: 3600)] // 1 hora de timeout
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })] // Retry: 1min, 5min, 15min
        public async Task RecurringCreateDocumentUnits(int recurringEventId, CancellationToken cancellationToken = default)
        {
            var recurringEvent = RecurringEvents.FromValue(recurringEventId);
            
            if (recurringEvent == null)
            {
                _logger.LogError("RecurringEvent with ID {RecurringEventId} not found", recurringEventId);
                return;
            }

            _logger.LogInformation("Recurring CreateDocument Units Init - event: {EventName} (ID: {EventId})", 
                recurringEvent.Name, recurringEvent.Id);

            var allEmployees = await _employeeRepository.GetDataAsync(cancellation: cancellationToken);

            await Parallel.ForEachAsync(allEmployees, cancellationToken, async (employee, ct) =>
            {
                // Criar um novo scope com DbContext isolado para cada employee
                using var scope = _serviceProvider.CreateScope();
                var scopedDocumentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                var scopedDocumentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                await scopedDocumentService.CreateDocumentUnitsForEvent(employee.Id, employee.CompanyId, recurringEvent.Id);
                await scopedDocumentRepository.UnitOfWork.SaveChangesAsync();
                
            });
            _logger.LogInformation("Recurring CreateDocument Units Complete - event: {EventName} (ID: {EventId})", 
                recurringEvent.Name, recurringEvent.Id);

        }

    }
}
