# Azure Functions Sample

## Graph API

This sample uses a MicrosoftGraph client credentials flow to get users on AD

### Configuring your client credentials

First of all, you should add some [secret configurations](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) before running this function.

Init your secrects 

```powershell
dotnet user-secrets init
```

And add your three new values
```powershell
dotnet user-secrets set "tenantId" "{MY_TENANT_ID}" # It's a GUID value like '12345678-1234-1234-1234-1234567890ab'. You can get this value on your tenant's overview page. https://aad.portal.azure.com/
dotnet user-secrets set "clientId" "{MY_CLIENT_ID}" # It's a GUID value like '12345678-1234-1234-1234-1234567890ab'. You can get this value on your app registration's overview page. https://portal.azure.com/
dotnet user-secrets set "clientSecret" "{MY_CLIENT_SECRET}" # It's like an ecrypted password Q~nfhpjRgObkjLeRmjQTsryD. You can get this value once when you create a new client secret on your app registration's page. https://portal.azure.com/
```

After that you be able to make calls to this function. For this sample, you can make a GET or POST requests.

### GET

```
curl -X GET http://localhost:7071/api/FunctionForTest?max=2
```

### POST

```
curl -X POST http://localhost:7071/api/FunctionForTest -H "Content-Type: application/json" -d "{ 'max': 2 }"
```

### Response

The response will look like this

```json
{
    "result": [
        {
            "id": "838cd9e3-48f5-48f5-48f5-6bea2f4b06d7",
            "displayName": "Bill Musk",
            "givenName": "Bill",
            "lastName": "Musk"
        },
        {
            "id": "2997aeb4-4634-4634-4634-2ca3570d8af3",
            "displayName": "Carlos Trump",
            "givenName": "Carlos",
            "lastName": "Trump"
        }
    ]
}
```

## Service Bus

- First, you need to have an Azure Service Bus created. You can create one using the Azure Portal or using the Azure CLI.
- Add a service bus connection string in your local.settings.json file

```json
{
  "IsEncrypted": false,
  "Values": {
	... // other environment variables
	"ServiceBus__ConnectionString": "Endpoint=sb://my-service-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=MY_SECRET_KEY"
    "ServiceBus__Queue__Normal": "normal.queue.name",
    "ServiceBus__Queue__Rejected": "rejected.queue.name"
  }
}
```

### Create a message

Send a post message to CreateServiceBusMessage function

```json
[POST] http://localhost:7240/api/CreateServiceBusMessage

{
    "message": "test",
    "value": 5
}
```

or using curl

```bash
curl -X POST http://localhost:7240/api/CreateServiceBusMessage -H "Content-Type: application/json" -d "{ 'message': 'Hello World!', 'value': 5 }"
```

### Read queue messages

All messages will be automatically read from the queue and processed by the FunctionForTest function.