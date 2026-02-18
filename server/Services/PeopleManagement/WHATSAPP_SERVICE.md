# WhatsApp Service - Guia de Uso

## Visão Geral

O `WhatsAppService` é um serviço de integração que permite enviar mensagens de texto via WhatsApp através de uma API HTTP.

## Configuração

### 1. appsettings.json

Adicione as seguintes configurações no arquivo `appsettings.json`:

```json
{
  "WhatsApp": {
    "BaseUrl": "https://your-whatsapp-api-url.com",
    "Instance": "your-instance-name",
    "ApiKey": "your-api-key-here",
    "Delay": 1200,
    "LinkPreview": false,
    "MentionsEveryOne": false
  }
}
```

### 2. Variáveis de Ambiente (Produção)

Em produção, você pode configurar via variáveis de ambiente:

```bash
WhatsApp__BaseUrl=https://your-whatsapp-api-url.com
WhatsApp__Instance=your-instance-name
WhatsApp__ApiKey=your-api-key-here
WhatsApp__Delay=1200
WhatsApp__LinkPreview=false
WhatsApp__MentionsEveryOne=false
```

## Parâmetros de Configuração

| Parâmetro | Tipo | Descrição | Padrão |
|-----------|------|-----------|---------|
| `BaseUrl` | string | URL base da API do WhatsApp | - |
| `Instance` | string | Nome da instância do WhatsApp | - |
| `ApiKey` | string | Chave de API para autenticação | - |
| `Delay` | int | Delay em milissegundos antes de enviar a mensagem | 1200 |
| `LinkPreview` | bool | Habilita preview de links nas mensagens | false |
| `MentionsEveryOne` | bool | Menciona todos em grupos | false |

## Uso

### Injeção de Dependência

O serviço já está registrado automaticamente na DI. Basta injetá-lo em qualquer classe:

```csharp
using PeopleManagement.Domain.Services;

public class MyService
{
    private readonly IWhatsAppService _whatsAppService;

    public MyService(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task NotifyEmployee(string phoneNumber, string message)
    {
        await _whatsAppService.SendTextMessageAsync(phoneNumber, message);
    }
}
```

### Exemplo em um Command Handler

```csharp
using MediatR;
using PeopleManagement.Domain.Services;

public class SendWelcomeMessageCommandHandler : IRequestHandler<SendWelcomeMessageCommand>
{
    private readonly IWhatsAppService _whatsAppService;

    public SendWelcomeMessageCommandHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task<Unit> Handle(SendWelcomeMessageCommand request, CancellationToken cancellationToken)
    {
        var phoneNumber = request.Employee.PhoneNumber;
        var message = $"Olá {request.Employee.Name}, bem-vindo à empresa!";

        await _whatsAppService.SendTextMessageAsync(phoneNumber, message, cancellationToken);

        return Unit.Value;
    }
}
```

### Exemplo em um Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using PeopleManagement.Domain.Services;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;

    public NotificationController(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    [HttpPost("send-whatsapp")]
    public async Task<IActionResult> SendWhatsAppMessage(
        [FromBody] SendMessageRequest request)
    {
        try
        {
            await _whatsAppService.SendTextMessageAsync(
                request.PhoneNumber, 
                request.Message);

            return Ok(new { success = true, message = "Mensagem enviada com sucesso!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class SendMessageRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

### Exemplo em um Domain Event Handler

```csharp
using MediatR;
using PeopleManagement.Domain.Services;
using PeopleManagement.Domain.Events;

public class EmployeeHiredDomainEventHandler : INotificationHandler<EmployeeHiredDomainEvent>
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<EmployeeHiredDomainEventHandler> _logger;

    public EmployeeHiredDomainEventHandler(
        IWhatsAppService whatsAppService,
        ILogger<EmployeeHiredDomainEventHandler> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task Handle(EmployeeHiredDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var employee = notification.Employee;
            var message = $"Parabéns {employee.Name}! Você foi contratado(a). " +
                         $"Sua data de início é {notification.StartDate:dd/MM/yyyy}.";

            await _whatsAppService.SendTextMessageAsync(
                employee.PhoneNumber, 
                message, 
                cancellationToken);

            _logger.LogInformation(
                "WhatsApp de boas-vindas enviado para {EmployeeName} no número {PhoneNumber}", 
                employee.Name, 
                employee.PhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Erro ao enviar WhatsApp para {EmployeeId}", 
                notification.Employee.Id);
        }
    }
}
```

## Formato do Número de Telefone

O número de telefone deve estar no formato internacional, incluindo o código do país sem o sinal de +:

✅ **Correto**: `5511999999999` (Brasil)
❌ **Incorreto**: `+5511999999999` ou `11999999999`

## Endpoint da API

O serviço faz um POST para:
```
{{BaseUrl}}/message/sendText/{{Instance}}
```

### Headers
```
apiKey: your-api-key-here
```

### Body
```json
{
  "number": "5511999999999",
  "text": "Sua mensagem aqui",
  "delay": 1200,
  "linkPreview": false,
  "mentionsEveryOne": false
}
```

## Tratamento de Erros

O serviço loga automaticamente erros e lança exceções que devem ser tratadas:

```csharp
try
{
    await _whatsAppService.SendTextMessageAsync(phoneNumber, message);
}
catch (HttpRequestException ex)
{
    // Erro de comunicação HTTP
    _logger.LogError(ex, "Falha na comunicação com a API do WhatsApp");
}
catch (Exception ex)
{
    // Outros erros
    _logger.LogError(ex, "Erro inesperado ao enviar WhatsApp");
}
```

## Política de Retry

O serviço está configurado com:
- **Retry Count**: 3 tentativas
- **Timeout**: 30 segundos
- **Handler Lifetime**: 5 minutos

Isso é configurado automaticamente via Polly no `InfraInjectionConfig.cs`.

## Logs

O serviço gera logs automáticos em nível de Information e Error:

```
[Information] Sending WhatsApp message to 5511999999999
[Information] WhatsApp message sent successfully to 5511999999999. Response: {...}
[Error] Failed to send WhatsApp message to 5511999999999
```

## Testes

### Teste Unitário

```csharp
using Moq;
using Xunit;
using PeopleManagement.Domain.Services;

public class WhatsAppServiceTests
{
    [Fact]
    public async Task SendTextMessageAsync_ShouldSendMessage_WhenValidParameters()
    {
        // Arrange
        var mockWhatsAppService = new Mock<IWhatsAppService>();
        mockWhatsAppService
            .Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var service = mockWhatsAppService.Object;

        // Act
        await service.SendTextMessageAsync("5511999999999", "Test message");

        // Assert
        mockWhatsAppService.Verify(
            x => x.SendTextMessageAsync("5511999999999", "Test message", default), 
            Times.Once);
    }
}
```

## Considerações de Segurança

⚠️ **IMPORTANTE**:
- Nunca commite a `ApiKey` no repositório
- Use variáveis de ambiente ou Azure Key Vault em produção
- Configure as secrets no GitHub Actions para deploy automatizado

```yaml
# GitHub Action - Configurar variável de ambiente
- name: Deploy to Azure Container Apps
  uses: azure/container-apps-deploy-action@v1
  with:
    environmentVariables: WhatsApp__ApiKey=secretref:whatsapp-apikey
```

## Suporte

Para problemas com o serviço, verifique:
1. Configuração correta no appsettings.json
2. Logs da aplicação
3. Conectividade com a API do WhatsApp
4. Formato correto do número de telefone

---

📱 Desenvolvido para integração com WhatsApp no Projeto Rufino
