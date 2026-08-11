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
        Assert.Null(CaptureSourceMother.OnlyFolder(source).SyncCursor);
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

    // BeginSync devolve as pastas a varrer, cada uma com o próprio cursor; nulo é varredura completa.
    [Fact]
    public void BeginSync_WhenEnabled_ShouldReturnFoldersWithTheirCursors()
    {
        var fresh = CaptureSourceMother.Connect();
        var synced = CaptureSourceMother.Synced();

        Assert.Null(Assert.Single(fresh.BeginSync()).SyncCursor);
        Assert.Equal("deltaLink-abc123", Assert.Single(synced.BeginSync()).SyncCursor);
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

    // Sincronização bem-sucedida avança o cursor da pasta, marca o instante e limpa o erro anterior.
    [Fact]
    public void RecordSyncSuccess_ShouldAdvanceCursorAndClearError()
    {
        var source = CaptureSourceMother.Connect();
        var folder = CaptureSourceMother.OnlyFolder(source);
        source.RecordSyncFailure(folder.Id, "timeout", CaptureSourceMother.DefaultOccurredAt.AddMinutes(1));

        source.RecordSyncSuccess(folder.Id, "deltaLink-novo", Later);

        Assert.Equal("deltaLink-novo", folder.SyncCursor);
        Assert.Equal(Later, source.LastSyncAt);
        Assert.Null(source.LastSyncError);
    }

    // Falha de sincronização NÃO toca no cursor: avançar pularia mensagens, apagar varreria a pasta inteira.
    [Fact]
    public void RecordSyncFailure_ShouldPreserveCursor()
    {
        var source = CaptureSourceMother.Synced();
        var folder = CaptureSourceMother.OnlyFolder(source);

        source.RecordSyncFailure(folder.Id, "503 Service Unavailable", Later);

        Assert.Equal("deltaLink-abc123", folder.SyncCursor);
        Assert.Equal("503 Service Unavailable", source.LastSyncError);
        Assert.Equal(Later, source.LastSyncAt);
    }

    // Mensagem de erro do provedor é truncada, não recusada — perder o registro da falha seria pior.
    [Fact]
    public void RecordSyncFailure_WithOverlongMessage_ShouldTruncate()
    {
        var source = CaptureSourceMother.Connect();

        source.RecordSyncFailure(
            CaptureSourceMother.OnlyFolder(source).Id,
            new string('x', CaptureSource.SYNC_ERROR_MAX_LENGTH + 50),
            Later);

        Assert.Equal(CaptureSource.SYNC_ERROR_MAX_LENGTH, source.LastSyncError!.Length);
    }

    // Falha sem mensagem ainda fica registrada, com motivo genérico.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordSyncFailure_WithBlankMessage_ShouldRecordUnknownError(string error)
    {
        var source = CaptureSourceMother.Connect();

        source.RecordSyncFailure(CaptureSourceMother.OnlyFolder(source).Id, error, Later);

        Assert.Equal("unknown_error", source.LastSyncError);
    }

    // Cursor acima do limite é recusado com BLP.CPS11 — é quebra de contrato do adapter.
    [Fact]
    public void RecordSyncSuccess_WithOverlongCursor_ShouldThrowBLP_CPS11()
    {
        var source = CaptureSourceMother.Connect();
        var cursor = new string('c', CaptureSource.SYNC_CURSOR_MAX_LENGTH + 1);

        var exception = Assert.Throws<DomainException>(
            () => source.RecordSyncSuccess(CaptureSourceMother.OnlyFolder(source).Id, cursor, Later));

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
        var folder = CaptureSourceMother.OnlyFolder(source);

        source.RecordSyncSuccess(folder.Id, cursor, Later);

        Assert.Null(folder.SyncCursor);
    }

    // Descartar o cursor de uma pasta é a resposta ao 410 Gone do Graph: a próxima varredura dela é completa.
    [Fact]
    public void ResetCursor_ShouldClearCursorOnly()
    {
        var source = CaptureSourceMother.Synced();
        var folder = CaptureSourceMother.OnlyFolder(source);

        source.ResetCursor(folder.Id, Later);

        Assert.Null(folder.SyncCursor);
        Assert.Equal(Later, source.UpdatedAt);
    }

    // Trocar a credencial mantém o cursor (a caixa é a mesma) e limpa o erro que motivou a troca.
    [Fact]
    public void ReplaceCredential_ShouldKeepCursorAndClearError()
    {
        var source = CaptureSourceMother.Synced();
        var folder = CaptureSourceMother.OnlyFolder(source);
        source.RecordSyncFailure(folder.Id, "invalid_client", Later);
        var novaCredencial = Domain.Secrets.CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c2"));

        source.ReplaceCredential(novaCredencial, Later.AddMinutes(1));

        Assert.Equal(novaCredencial, source.Credential);
        Assert.Equal("deltaLink-abc123", folder.SyncCursor);
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

    // Sem pasta informada, a fonte acompanha a caixa de entrada — e nasce com exatamente uma pasta.
    [Fact]
    public void Connect_WithoutFolder_ShouldMonitorTheWholeInbox()
        => Assert.Null(Assert.Single(CaptureSourceMother.Connect().Folders).Path);

    // A pasta é normalizada: espaços e barras nas pontas saem, para o caminho não duplicar.
    [Theory]
    [InlineData("  Contas  ", "Contas")]
    [InlineData("/Contas/", "Contas")]
    [InlineData("Contas/2026", "Contas/2026")]
    public void Connect_WithFolder_ShouldStoreNormalizedPath(string input, string expected)
    {
        var source = CaptureSourceMother.Connect(folderPath: input);

        Assert.Equal(expected, Assert.Single(source.Folders).Path);
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

        Assert.Null(Assert.Single(source.Folders).Path);
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
        Assert.NotNull(CaptureSourceMother.OnlyFolder(source).SyncCursor);

        source.ChangeFolder("Contas", Later);

        var folder = Assert.Single(source.Folders);
        Assert.Equal("Contas", folder.Path);
        Assert.Null(folder.SyncCursor);
        Assert.Equal(Later, source.UpdatedAt);
    }

    // Acrescentar pasta amplia o que a fonte acompanha, e a pasta nova nasce sem cursor: a
    // primeira varredura dela lê tudo o que já está lá.
    [Fact]
    public void AddFolder_ShouldMonitorTheNewFolderWithoutCursor()
    {
        var source = CaptureSourceMother.Synced();

        var added = source.AddFolder("Contas/Fornecedores", Later);

        Assert.Equal(2, source.Folders.Count);
        Assert.Equal("Contas/Fornecedores", added.Path);
        Assert.Null(added.SyncCursor);
        Assert.Equal("deltaLink-abc123", CaptureSourceMother.OnlyFolder(source).SyncCursor);
    }

    // Pasta repetida é recusada com BLP.CPS16, e a normalização é quem decide o que é repetido —
    // senão "Contas", "/Contas" e "Contas/" virariam três pastas iguais gastando três chamadas.
    [Theory]
    [InlineData("Contas")]
    [InlineData("/Contas/")]
    [InlineData("  contas  ")]
    public void AddFolder_WhenAlreadyMonitored_ShouldThrowBLP_CPS16(string duplicate)
    {
        var source = CaptureSourceMother.Connect(folderPath: "Contas");

        var exception = Assert.Throws<DomainException>(() => source.AddFolder(duplicate, Later));

        Assert.Equal("BLP.CPS16", exception.Id);
    }

    // A caixa de entrada também é uma pasta acompanhada: acrescentá-la duas vezes é repetição.
    [Fact]
    public void AddFolder_WhenInboxIsAlreadyMonitored_ShouldThrowBLP_CPS16()
    {
        var source = CaptureSourceMother.Connect();

        var exception = Assert.Throws<DomainException>(() => source.AddFolder(null, Later));

        Assert.Equal("BLP.CPS16", exception.Id);
    }

    // O teto de pastas é recusado com BLP.CPS19: cada pasta é uma chamada ao provedor por ciclo.
    [Fact]
    public void AddFolder_BeyondTheLimit_ShouldThrowBLP_CPS19()
    {
        var source = CaptureSourceMother.Connect();
        for (var i = 1; i < CaptureSource.MAX_FOLDERS; i++)
            source.AddFolder($"Pasta{i}", Later);

        var exception = Assert.Throws<DomainException>(() => source.AddFolder("Excedente", Later));

        Assert.Equal("BLP.CPS19", exception.Id);
    }

    // Remover pasta deixa de acompanhá-la e não toca nas outras.
    [Fact]
    public void RemoveFolder_ShouldStopMonitoringItOnly()
    {
        var source = CaptureSourceMother.Connect();
        source.AddFolder("Contas", Later);

        source.RemoveFolder("Contas", Later.AddMinutes(1));

        Assert.Null(Assert.Single(source.Folders).Path);
    }

    // Pasta não acompanhada é recusada com BLP.CPS17.
    [Fact]
    public void RemoveFolder_WhenNotMonitored_ShouldThrowBLP_CPS17()
    {
        var source = CaptureSourceMother.Connect();

        var exception = Assert.Throws<DomainException>(() => source.RemoveFolder("Inexistente", Later));

        Assert.Equal("BLP.CPS17", exception.Id);
    }

    // Remover a última pasta é recusado com BLP.CPS18: fonte sem pasta não varre nada e não
    // avisa — zero pasta produz zero item exatamente como uma caixa vazia. Quem quer parar, desativa.
    [Fact]
    public void RemoveFolder_WhenItIsTheLastOne_ShouldThrowBLP_CPS18()
    {
        var source = CaptureSourceMother.Connect();

        var exception = Assert.Throws<DomainException>(() => source.RemoveFolder(null, Later));

        Assert.Equal("BLP.CPS18", exception.Id);
    }

    // Falha numa pasta NÃO contamina as outras: cada uma tem cursor e erro próprios, e é isso
    // que impede uma pasta renomeada no cliente de e-mail de parar a captura inteira.
    [Fact]
    public void RecordSyncFailure_ShouldNotAffectSiblingFolders()
    {
        var source = CaptureSourceMother.Synced();
        var inbox = CaptureSourceMother.OnlyFolder(source);
        var contas = source.AddFolder("Contas", Later);

        source.RecordSyncFailure(contas.Id, "folder_not_found", Later.AddMinutes(1));

        Assert.Equal("folder_not_found", contas.LastSyncError);
        Assert.Null(inbox.LastSyncError);
        Assert.Equal("deltaLink-abc123", inbox.SyncCursor);
    }

    // O erro da raiz é resumo: varrer uma pasta com êxito não apaga o aviso enquanto OUTRA
    // continua falhando — senão a pasta quebrada sumiria da tela enquanto as demais rodam bem.
    [Fact]
    public void RecordSyncSuccess_WhenAnotherFolderStillFails_ShouldKeepTheRootError()
    {
        var source = CaptureSourceMother.Connect();
        var inbox = CaptureSourceMother.OnlyFolder(source);
        var contas = source.AddFolder("Contas", Later);

        source.RecordSyncFailure(contas.Id, "folder_not_found", Later.AddMinutes(1));
        source.RecordSyncSuccess(inbox.Id, "deltaLink-novo", Later.AddMinutes(2));

        Assert.Equal("folder_not_found", source.LastSyncError);
    }

    // A releitura deliberada zera o cursor de TODAS as pastas: é o que permite reavaliar o que
    // já passou depois de cadastrar PayerProfile, Payee ou TrustedOrigin.
    [Fact]
    public void ResetAllCursors_ShouldClearEveryFolderCursor()
    {
        var source = CaptureSourceMother.Synced();
        var contas = source.AddFolder("Contas", Later);
        source.RecordSyncSuccess(contas.Id, "deltaLink-contas", Later);

        source.ResetAllCursors(Later.AddMinutes(1));

        Assert.All(source.Folders, f => Assert.Null(f.SyncCursor));
        Assert.Equal(Later.AddMinutes(1), source.UpdatedAt);
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
