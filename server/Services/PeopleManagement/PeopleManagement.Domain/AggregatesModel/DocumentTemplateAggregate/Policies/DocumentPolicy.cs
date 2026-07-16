namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Representação persistida (flat) de uma policy de um <see cref="DocumentTemplate"/>: discriminador
    /// (<see cref="PolicyType"/>) + parâmetros serializados (jsonb). É o elemento da coleção filha mapeada
    /// no EF (Opção 1 de persistência). A conversão de/para as policies tipadas (comportamento) fica em
    /// <see cref="DocumentPolicyFactory"/>.
    /// </summary>
    public sealed class DocumentPolicy
    {
        public PolicyType Type { get; private set; } = null!;
        public string Params { get; private set; } = null!;

        private DocumentPolicy() { }

        public DocumentPolicy(PolicyType type, string @params)
        {
            Type = type;
            Params = @params;
        }
    }
}
