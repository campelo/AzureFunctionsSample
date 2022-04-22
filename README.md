# Azure Functions Sample

This sample uses a MicrosoftGraph client credentials flow to get users on AD

To use it, you can make a GET or POST to the app URL like those samples

## GET

```
curl -X GET http://localhost:7071/api/FunctionForTest?max=2
```

## POST

```
curl -X POST http://localhost:7071/api/FunctionForTest -H "Content-Type: application/json" -d "{ 'max': 2 }"
```

## Response

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