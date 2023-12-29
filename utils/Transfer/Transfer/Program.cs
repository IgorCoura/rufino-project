using RestSharp;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Transfer.models;

Console.WriteLine("Bem Vindo ao Transfer");


// Gerar pfx "openssl pkcs12 -inkey private-key.key -in certificate.pem -export -out certificate.pfx"

var certificate = new X509Certificate2("C:\\Users\\IGOR-NOT\\Desktop\\Codigos\\CodigosSSD\\rufino-project\\utils\\Transfer\\Transfer\\secrets\\cert_key_cora_production_2023_10_26\\certificate.pfx");

var options = new RestClientOptions("https://matls-clients.api.cora.com.br/");

options.ClientCertificates = new X509CertificateCollection() { certificate };

var client = new RestClient(options);

// Criar uma requisição POST para o endpoint /token
var request = new RestRequest("/token", Method.Post);

// Adicionar o cabeçalho Content-Type
request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

// Adicionar os parâmetros grant_type e client_id ao corpo da requisição
request.AddParameter("grant_type", "client_credentials");
request.AddParameter("client_id", "int-3DF6Nm1By0XXA57G3cWFTP");

// Executar a requisição e obter a resposta
RestResponse response = client.Execute(request);

var token = JsonSerializer.Deserialize<TokenModel>(response.Content);

var guid = Guid.NewGuid();

var transfer = new TransferModel(
    destination: new DestinationModel(
        bankCode: "260",
        accountNumber: "81512234",
        branchNumber: "0001",
        holder: new HolderModel(
            name: "IGOR DE BRITO COURA",
            document: new DocumentModel(
                identity: "49853035855",
                type: DocumentTypesEnum.CPF
                )
            ),
        accountType: AccountTypesEnum.CHECKING
        ),
        amount: 1000,
        description: "teste",
        code: "1",
        category: CategoryEnum.UTILITIES,
        scheduled: "2023-10-25"
    );

var transferJson = JsonSerializer.Serialize(transfer);


var requestTransfer = new RestRequest("/transfers/initiate", Method.Post);
requestTransfer.AddHeader("accept", "application/json");
requestTransfer.AddHeader("Idempotency-Key", guid.ToString());
requestTransfer.AddHeader("Authorization", token.ToAuth());
requestTransfer.AddJsonBody(transferJson, false);
var responseTransfer = client.Execute(requestTransfer);

// Imprimir o conteúdo da resposta
Console.WriteLine(responseTransfer.Content);


Console.WriteLine("Fim");


//var options = new RestClientOptions("https://api.stage.cora.com.br/transfers/initiate");
//var client = new RestClient(options);
//var request = new RestRequest("");
//request.AddHeader("accept", "application/json");
//request.AddHeader("Idempotency-Key", "d4f72104-66cd-4b1a-9a92-700b2e1d2c18");
//request.AddJsonBody("{\"destination\":{\"account_type\":\"CHECKING\",\"bank_code\":\"341\",\"account_number\":\"092135\",\"branch_number\":\"7679\",\"holder\":{\"name\":\"Cora Pagamentos\",\"document\":{\"identity\":\"72420176000104\",\"type\":\"CNPJ\"}}},\"amount\":10001,\"description\":\"Mandando\",\"code\":\"your-specific-code-EXP123\",\"category\":\"PAYROLL\",\"scheduled\":\"2023-05-31\"}", false);
//var response = await client.PostAsync(request);

//Console.WriteLine("{0}", response.Content);
