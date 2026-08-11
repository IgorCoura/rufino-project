namespace BillPayment.UnitTests.CaptureSources;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.CaptureSources.Mothers;

public class CaptureSourceTests
{
    private static readonly DateTime Later = CaptureSourceMother.DefaultOccurredAt.AddHours(2);

    // Conectar uma caixa guarda tipo, endereço, credencial e auditoria, e nasce habilitada.
    [Fact]
    public void Connect_WithValidMailbox_ShouldStoreFieldsAndStartEnabled()
    {
        var source = CaptureSourceMother.Connect();

        Assert.Same(CaptureSourceKind.MicrosoftGraphMailbox, source.Kind);
        Assert.Equal(CaptureSourceMother.DefaultMailbox, source.Address);
        Assert.Equal(CaptureSourceMother.DefaultCredential, source.Credential);
        Assert.Equal(CaptureSourceMother.DefaultTenant, source.TenantId);
        Assert.True(source.IsEnabled);
        Assert.Null(source.SyncCursor);
        Assert.Null(source.LastSyncAt);
        Assert.Null(source.LastSyncError);
        Assert.Equal(CaptureSourceMother.DefaultOccurredAt, source.CreatedAt);
    }

    // O endereço da caixa é normalizado para minúsculas e sem espaços nas bordas.
    [Theory]
    [InlineData("  CONTAS@Empresa.COM.BR  ")]
    [InlineData("Contas@Empresa.com.br")]
    public void Connect_WithUnnormalizedMailbox_ShouldStoreNormalizedAddress(string input)
    {
        var source = CaptureSourceMother.Connect(address: input);

        Assert.Equal(CaptureSourceMother.DefaultMailbox, source.Address);
    }

    // Endereço que não é e-mail válido é recusado com BLP.CPS06.
    [Theory]
    [InlineData("contas")]
    [InlineData("contas@")]
    [InlineData("contas@empresa")]
    [InlineData("a@b@empresa.com.br")]
    public void Connect_WithInvalidMailbox_ShouldThrowBLP_CPS06(string address)
    {
        var exception = Assert.Throws<DomainException>(() => CaptureSourceMother.Connect(address: address));

        Assert.Equal("BLP.CPS06", exception.Id);
    }

    // Fonte que exige credencial não existe sem o ponteiro do cofre — BLP.CPS01.
    [Fact]
    public void Connect_WithoutCredential_ShouldThrowBLP_CPS01()
    {
        var exception = Assert.Throws<DomainException>(() => CaptureSourceMother.ConnectVerbatim(
            CaptureSourceKind.MicrosoftGraphMailbox,
            "Caixa",
            CaptureSourceMother.DefaultMailbox,
            credential: null));

        Assert.Equal("BLP.CPS01", exception.Id);
    }

    // ManualUpload não guarda credencial e por isso conecta sem ponteiro de cofre.
    [Fact]
    public void Connect_ManualUploadWithoutCredential_ShouldSucceed()
    {
        var source = CaptureSourceMother.ConnectVerbatim(
            CaptureSourceKind.ManualUpload,
            "Envio manual",
            "envio-manual",
            credential: null);

        Assert.Null(source.Credential);
        Assert.Equal("envio-manual", source.Address);
    }

    // Portal só é aceito em https; qualquer outro esquema cai em BLP.CPS07.
    [Theory]
    [InlineData("http://portal.concessionaria.com.br")]
    [InlineData("ftp://portal.concessionaria.com.br")]
    [InlineData("portal.concessionaria.com.br")]
    public void Connect_PortalWithoutHttps_ShouldThrowBLP_CPS07(string address)
    {
        var exception = Assert.Throws<DomainException>(() => CaptureSourceMother.Connect(
            CaptureSourceKind.Portal, address: address));

        Assert.Equal("BLP.CPS07", exception.Id);
    }

    // A URL do portal preserva as maiúsculas do caminho — rebaixá-las apontaria para outro recurso.
    [Fact]
    public void Connect_PortalUrl_ShouldPreservePathCasing()
    {
        var source = CaptureSourceMother.Connect(
            CaptureSourceKind.Portal, address: CaptureSourceMother.DefaultPortalUrl);

        Assert.Equal(CaptureSourceMother.DefaultPortalUrl, source.Address);
    }

    // Nome de exibição vazio é recusado com BLP.CPS08.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Connect_WithBlankDisplayName_ShouldThrowBLP_CPS08(string displayName)
    {
        var exception = Assert.Throws<DomainException>(() => CaptureSourceMother.Connect(displayName: displayName));

        Assert.Equal("BLP.CPS08", exception.Id);
    }

    // Nome de exibição acima do limite é recusado com BLP.CPS09.
    [Fact]
    public void Connect_WithOverlongDisplayName_ShouldThrowBLP_CPS09()
    {
        var displayName = new string('a', CaptureSource.DISPLAY_NAME_MAX_LENGTH + 1);

        var exception = Assert.Throws<DomainException>(() => CaptureSourceMother.Connect(displayName: displayName));

        Assert.Equal("BLP.CPS09", exception.Id);
    }

    // BeginSync devolve o cursor de onde retomar; nulo significa varredura completa.
    [Fact]
    public void BeginSync_WhenEnabled_ShouldReturnCursor()
    {
        var fresh = CaptureSourceMother.Connect();
        var synced = CaptureSourceMother.Synced();

        Assert.Null(fresh.BeginSync());
        Assert.Equal("deltaLink-abc123", synced.BeginSync());
    }

    // Fonte desativada recusa a sincronização com BLP.CPS12 — o botão de parada não é decorativo.
    [Fact]
    public void BeginSync_WhenDisabled_ShouldThrowBLP_CPS12()
    {
        var source = CaptureSourceMother.Synced();
        source.SetEnabled(false, Later);

        var exception = Assert.Throws<DomainException>(source.BeginSync);

        Assert.Equal("BLP.CPS12", exception.Id);
    }

    // Sincronização bem-sucedida avança o cursor, marca o instante e limpa o erro anterior.
    [Fact]
    public void RecordSyncSuccess_ShouldAdvanceCursorAndClearError()
    {
        var source = CaptureSourceMother.Connect();
        source.RecordSyncFailure("timeout", CaptureSourceMother.DefaultOccurredAt.AddMinutes(1));

        source.RecordSyncSuccess("deltaLink-novo", Later);

        Assert.Equal("deltaLink-novo", source.SyncCursor);
        Assert.Equal(Later, source.LastSyncAt);
        Assert.Null(source.LastSyncError);
    }

    // Falha de sincronização NÃO toca no cursor: avançar pularia mensagens, apagar varreria a caixa inteira.
    [Fact]
    public void RecordSyncFailure_ShouldPreserveCursor()
    {
        var source = CaptureSourceMother.Synced();

        source.RecordSyncFailure("503 Service Unavailable", Later);

        Assert.Equal("deltaLink-abc123", source.SyncCursor);
        Assert.Equal("503 Service Unavailable", source.LastSyncError);
        Assert.Equal(Later, source.LastSyncAt);
    }

    // Mensagem de erro do provedor é truncada, não recusada — perder o registro da falha seria pior.
    [Fact]
    public void RecordSyncFailure_WithOverlongMessage_ShouldTruncate()
    {
        var source = CaptureSourceMother.Connect();

        source.RecordSyncFailure(new string('x', CaptureSource.SYNC_ERROR_MAX_LENGTH + 50), Later);

        Assert.Equal(CaptureSource.SYNC_ERROR_MAX_LENGTH, source.LastSyncError!.Length);
    }

    // Falha sem mensagem ainda fica registrada, com motivo genérico.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordSyncFailure_WithBlankMessage_ShouldRecordUnknownError(string error)
    {
        var source = CaptureSourceMother.Connect();

        source.RecordSyncFailure(error, Later);

        Assert.Equal("unknown_error", source.LastSyncError);
    }

    // Cursor acima do limite é recusado com BLP.CPS11 — é quebra de contrato do adapter.
    [Fact]
    public void RecordSyncSuccess_WithOverlongCursor_ShouldThrowBLP_CPS11()
    {
        var source = CaptureSourceMother.Connect();
        var cursor = new string('c', CaptureSource.SYNC_CURSOR_MAX_LENGTH + 1);

        var exception = Assert.Throws<DomainException>(() => source.RecordSyncSuccess(cursor, Later));

        Assert.Equal("BLP.CPS11", exception.Id);
    }

    // Cursor vazio é guardado como nulo, e nulo significa varredura completa na próxima vez.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordSyncSuccess_WithBlankCursor_ShouldStoreNull(string? cursor)
    {
        var source = CaptureSourceMother.Synced();

        source.RecordSyncSuccess(cursor, Later);

        Assert.Null(source.SyncCursor);
    }

    // Descartar o cursor é a resposta ao 410 Gone do Graph: a próxima varredura é completa.
    [Fact]
    public void ResetCursor_ShouldClearCursorOnly()
    {
        var source = CaptureSourceMother.Synced();

        source.ResetCursor(Later);

        Assert.Null(source.SyncCursor);
        Assert.Equal(Later, source.UpdatedAt);
    }

    // Trocar a credencial mantém o cursor (a caixa é a mesma) e limpa o erro que motivou a troca.
    [Fact]
    public void ReplaceCredential_ShouldKeepCursorAndClearError()
    {
        var source = CaptureSourceMother.Synced();
        source.RecordSyncFailure("invalid_client", Later);
        var novaCredencial = Domain.Secrets.CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c2"));

        source.ReplaceCredential(novaCredencial, Later.AddMinutes(1));

        Assert.Equal(novaCredencial, source.Credential);
        Assert.Equal("deltaLink-abc123", source.SyncCursor);
        Assert.Null(source.LastSyncError);
    }

    // Habilitar e desabilitar é um método só, para o if não vazar para a Application.
    [Fact]
    public void SetEnabled_ShouldToggleAndStampUpdatedAt()
    {
        var source = CaptureSourceMother.Connect();

        source.SetEnabled(false, Later);
        Assert.False(source.IsEnabled);
        Assert.Equal(Later, source.UpdatedAt);

        source.SetEnabled(true, Later.AddHours(1));
        Assert.True(source.IsEnabled);
    }

    // Sem pasta informada, a fonte monitora a caixa de entrada inteira.
    [Fact]
    public void Connect_WithoutFolder_ShouldMonitorTheWholeInbox()
        => Assert.Null(CaptureSourceMother.Connect().FolderPath);

    // A pasta é normalizada: espaços e barras nas pontas saem, para o caminho não duplicar.
    [Theory]
    [InlineData("  Contas  ", "Contas")]
    [InlineData("/Contas/", "Contas")]
    [InlineData("Contas/2026", "Contas/2026")]
    public void Connect_WithFolder_ShouldStoreNormalizedPath(string input, string expected)
    {
        var source = CaptureSource.Connect(
            CaptureSourceMother.DefaultTenant,
            CaptureSourceKind.MicrosoftGraphMailbox,
            "Caixa",
            CaptureSourceMother.DefaultMailbox,
            CaptureSourceMother.DefaultCredential,
            CaptureSourceMother.DefaultOccurredAt,
            input);

        Assert.Equal(expected, source.FolderPath);
    }

    // Pasta em branco equivale a ausência — não existe pasta de nome vazio.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    public void ChangeFolder_WithBlankPath_ShouldFallBackToInbox(string input)
    {
        var source = CaptureSourceMother.Connect();

        source.ChangeFolder(input, Later);

        Assert.Null(source.FolderPath);
    }

    // Caminho acima do limite é recusado com BLP.CPS15.
    [Fact]
    public void ChangeFolder_WithOverlongPath_ShouldThrowBLP_CPS15()
    {
        var source = CaptureSourceMother.Connect();

        var exception = Assert.Throws<DomainException>(
            () => source.ChangeFolder(new string('p', CaptureSource.FOLDER_PATH_MAX_LENGTH + 1), Later));

        Assert.Equal("BLP.CPS15", exception.Id);
    }

    // Trocar de pasta DESCARTA o cursor: a varredura incremental do provedor é por pasta, e
    // manter o cursor faria a primeira leitura da pasta nova voltar vazia — falha silenciosa.
    [Fact]
    public void ChangeFolder_ShouldDiscardTheCursor()
    {
        var source = CaptureSourceMother.Synced();
        Assert.NotNull(source.SyncCursor);

        source.ChangeFolder("Contas", Later);

        Assert.Equal("Contas", source.FolderPath);
        Assert.Null(source.SyncCursor);
        Assert.Equal(Later, source.UpdatedAt);
    }

    // Normalize é a única implementação da chave de comparação, inclusive para o índice global do ADR-008.
    [Fact]
    public void Normalize_ShouldLowercaseMailboxAndOnlyTrimPortalUrl()
    {
        Assert.Equal(
            "contas@empresa.com.br",
            CaptureSource.Normalize(CaptureSourceKind.MicrosoftGraphMailbox, "  Contas@Empresa.COM.BR "));

        Assert.Equal(
            CaptureSourceMother.DefaultPortalUrl,
            CaptureSource.Normalize(CaptureSourceKind.Portal, $"  {CaptureSourceMother.DefaultPortalUrl}  "));
    }
}
