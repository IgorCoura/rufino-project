using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace PeopleManagement.UnitTests.Fakes
{
    /// <summary>
    /// Fake manual de <see cref="IBackgroundJobClient"/> — captura as chamadas <c>Create(Job, IState)</c>
    /// produzidas pelas extensões <c>Schedule&lt;T&gt;</c>, permitindo inspecionar qual método foi agendado
    /// e para quando (via <see cref="ScheduledState.EnqueueAt"/>). Não é mock de domínio; é um duplo de
    /// infraestrutura para o handler de agendamento.
    /// </summary>
    public sealed class FakeBackgroundJobClient : IBackgroundJobClient
    {
        public List<(string MethodName, IState State)> Created { get; } = [];

        public string Create(Job job, IState state)
        {
            Created.Add((job.Method.Name, state));
            return Guid.NewGuid().ToString();
        }

        public bool ChangeState(string jobId, IState state, string expectedState) => true;
    }
}
