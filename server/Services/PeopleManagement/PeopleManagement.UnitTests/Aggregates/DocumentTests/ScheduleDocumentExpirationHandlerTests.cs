using Hangfire.States;
using Microsoft.Extensions.Logging.Abstractions;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Services.DomainEventHandlers;
using PeopleManagement.UnitTests.Fakes;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Caracterização do <see cref="ScheduleDocumentExpirationHandler"/>: quantos jobs agenda e para quando,
    /// dado o vencimento da unidade. Usa <see cref="DocumentOptions"/> padrão (WarningDaysBeforeDocumentExpiration=30,
    /// WarningRatio=0.3). O handler acopla-se a DateTime.Now, então as expectativas são construídas a partir de "today".
    /// </summary>
    public class ScheduleDocumentExpirationHandlerTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

        private static ScheduleDocumentExpirationHandler CreateHandler(FakeBackgroundJobClient client)
            => new(client, NullLogger<ScheduleDocumentExpirationHandler>.Instance, new DocumentOptions());

        private static ScheduleDocumentExpirationEvent Event(DateOnly expiration, DateOnly issuedAt)
            => ScheduleDocumentExpirationEvent.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), expiration, issuedAt);

        [Fact]
        public async Task Handle_WhenExpirationIsInPast_SchedulesOnlyDepreciation()
        {
            var client = new FakeBackgroundJobClient();
            var handler = CreateHandler(client);

            await handler.Handle(Event(Today.AddDays(-5), Today.AddDays(-35)), CancellationToken.None);

            var created = Assert.Single(client.Created);
            Assert.Equal("DepreciateExpirateDocument", created.MethodName);
        }

        [Fact]
        public async Task Handle_WhenExpirationIsInFuture_SchedulesDepreciationAndWarning()
        {
            var client = new FakeBackgroundJobClient();
            var handler = CreateHandler(client);

            await handler.Handle(Event(Today.AddDays(40), Today), CancellationToken.None);

            Assert.Equal(2, client.Created.Count);
            var depreciation = client.Created.Single(x => x.MethodName == "DepreciateExpirateDocument");
            var warning = client.Created.Single(x => x.MethodName == "WarningExpirateDocument");

            Assert.Equal(Today.AddDays(40), DateOnly.FromDateTime(Assert.IsType<ScheduledState>(depreciation.State).EnqueueAt));
            Assert.Equal(Today.AddDays(28), DateOnly.FromDateTime(Assert.IsType<ScheduledState>(warning.State).EnqueueAt));
        }

        [Fact]
        public async Task Handle_WhenValidityIsLong_WarningIsCappedAtThirtyDays()
        {
            var client = new FakeBackgroundJobClient();
            var handler = CreateHandler(client);

            await handler.Handle(Event(Today.AddDays(365), Today), CancellationToken.None);

            var warning = client.Created.Single(x => x.MethodName == "WarningExpirateDocument");
            Assert.Equal(Today.AddDays(335), DateOnly.FromDateTime(Assert.IsType<ScheduledState>(warning.State).EnqueueAt));
        }

        [Fact]
        public async Task Handle_WhenWarningDayIsInPast_SchedulesWarningImmediatelyAndDepreciationAtExpiration()
        {
            var client = new FakeBackgroundJobClient();
            var handler = CreateHandler(client);

            await handler.Handle(Event(Today.AddDays(2), Today.AddDays(-100)), CancellationToken.None);

            Assert.Equal(2, client.Created.Count);
            var depreciation = client.Created.Single(x => x.MethodName == "DepreciateExpirateDocument");
            var warning = client.Created.Single(x => x.MethodName == "WarningExpirateDocument");

            Assert.Equal(Today.AddDays(2), DateOnly.FromDateTime(Assert.IsType<ScheduledState>(depreciation.State).EnqueueAt));
            Assert.True(DateOnly.FromDateTime(Assert.IsType<ScheduledState>(warning.State).EnqueueAt) < Today.AddDays(2));
        }
    }
}
