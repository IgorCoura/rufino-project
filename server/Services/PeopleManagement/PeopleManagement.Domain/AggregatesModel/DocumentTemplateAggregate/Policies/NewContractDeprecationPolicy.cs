namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Depreciação em novo contrato: ao começar um novo contrato de trabalho para o funcionário, as
    /// unidades entregues (OK) dos documentos deste template são depreciadas — o que foi entregue vale
    /// para o vínculo em que foi entregue, não para o seguinte.
    ///
    /// Regra sem parâmetro: presença da policy = regra ativa, ausência = documentos atravessam o novo
    /// contrato intactos. Não há "grau" de depreciação a configurar, então não há nada para guardar aqui —
    /// o jsonb fica vazio de propósito, e um parâmetro futuro entra sem mudar o discriminador.
    /// </summary>
    public sealed class NewContractDeprecationPolicy : INewContractDeprecationPolicy
    {
    }
}
