## Publishing to Azure function
Simply read more here: https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-javascript#install-the-azure-functions-core-tools

But basically, run 
```
func azure functionapp publish <FunctionAppName>
```

__NB! Remember to login via__ `az login`

Could also be a good idea to fetch app settings:
```
func azure functionapp fetch-app-settings <FunctionAppName>
```
