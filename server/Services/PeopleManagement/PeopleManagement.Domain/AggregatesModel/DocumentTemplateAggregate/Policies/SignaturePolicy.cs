namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Assinatura: o template aceita que seus documentos sejam assinados, e carrega onde as assinaturas
    /// entram na página. Presença da policy = aceita assinatura.
    ///
    /// Os locais moram aqui, e não soltos no template, porque local de assinatura só faz sentido quando a
    /// assinatura é aceita — juntos, o estado contraditório deixa de ser representável no modelo persistido.
    /// Aceitar assinatura sem definir local é legítimo: a assinatura vai sem posicionamento fixo.
    /// </summary>
    public sealed class SignaturePolicy : ISignaturePolicy
    {
        private readonly List<PlaceSignature> _placeSignatures;

        public IReadOnlyList<PlaceSignature> PlaceSignatures => _placeSignatures.AsReadOnly();

        public SignaturePolicy(IEnumerable<PlaceSignature>? placeSignatures = null)
        {
            _placeSignatures = placeSignatures?.ToList() ?? [];
        }
    }
}
