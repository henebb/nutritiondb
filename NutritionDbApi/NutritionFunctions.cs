using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace NutritionDbApi;

public static class NutritionFunctions
{
    [Function("Get all nutritions")]
    public static HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "nutritions")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString("Welcome to Azure Functions!");
        //var conn = System.Environment.GetEnvironmentVariable("CosmosDb:ConnectionString");
        //response.WriteString(conn!);
        return response;
    }
}
