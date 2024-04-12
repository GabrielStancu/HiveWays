using Azure.Storage.Blobs;
using HiveWays.VehicleEdge.Configuration;
using HttpMultipartParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HiveWays.VehicleEdge;

public class CarDataReceiver
{
    private readonly StorageAccountConfiguration _saConfiguration;
    private readonly ILogger<CarDataReceiver> _logger;

    public CarDataReceiver(StorageAccountConfiguration saConfiguration,
        ILogger<CarDataReceiver> logger)
    {
        _saConfiguration = saConfiguration;
        _logger = logger;
    }

    [Function("CarDataReceiver")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var parsedFormBody = MultipartFormDataParser.ParseAsync(req.Body);
        var file = parsedFormBody.Result.Files[0];

        _logger.LogInformation("Received file {ReceivedFile}", file.FileName);

        var blobClient = new BlobContainerClient(_saConfiguration.ConnectionString, _saConfiguration.ContainerName);

        await blobClient.UploadBlobAsync(file.FileName, file.Data);

        _logger.LogInformation("Uploaded file {ReceivedFile} to blob storage", file.FileName);

        return new OkResult();
    }
}