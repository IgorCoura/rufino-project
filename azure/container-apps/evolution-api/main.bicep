@description('Nome do Container App')
param containerAppName string = 'evolution-api'

@description('Localização dos recursos')
param location string = resourceGroup().location

@description('Nome do ambiente do Container App')
param environmentName string

@description('ID do Container App Environment')
param environmentId string

@description('Imagem do container')
param containerImage string = 'evoapicloud/evolution-api:v2.3.7'

@description('Porta do container')
param containerPort int = 8080

@description('Variáveis de ambiente')
@secure()
param environmentVariables array = []

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  properties: {
    environmentId: environmentId
    configuration: {
      ingress: {
        external: true
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
      }
    }
    template: {
      containers: [
        {
          name: 'evolution-api'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: environmentVariables
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
output containerAppId string = containerApp.id
