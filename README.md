# OwnId server SDK for NET Core 3

## Running locally
Use `dotnet run` or `dotnet run --urls http://0.0.0.0.:5002` to set specific urls.  


### Development certificates for HTTPS binding
#### On Mac (run in terminal):  
`dotnet dev-certs https --check` to check if you have a valid installed certiciate. If nothing was return or you faced a massage that cetificate was not found you need to run next steps.
*Ensure you have enabled privileges*  
`dotnet dev-certs https` then put your keychain password if everything is ok you will see "The HTTPS developer certificate was generated successfully." message.
After that you need to find this cert. in Mac Keychain, open and trust ssl.  
Sometimes you can face a problem that nothing was changed and you still see browser warnings. Try to remove all certificates in Mac Keychain label with `localhost` and repeat the last step.

## Environment variables for applications (OwnIdSdk.NetCore3.Server.Gigya project)

- *OWNID__WEB_APP_URL* - web application url (with relative path if needed)
- *OWNID__CALLBACK_URL* - this(client-app) appliction url to call after user accepts login/register
- *OWNID__PUB_KEY* - path to public rsa key
- *OWNID__PRIVATE_KEY* - path to private rsa key
- *OWNID__DID* - requester did
- *OWNID__NAME* - requester name (will be shown in web-app)
- *OWNID__DESCRIPTION* - requester description (will be shown in web-app)
- *GIGYA__SECRET* - gigya account secret
- *GIGYA__API_KEY* - gigya account api key
- *GIGYA__SEGMENT* - gigya segment
