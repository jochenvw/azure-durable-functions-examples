using System;
using System.Collections.Generic;
using System.IO;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TransformationFunctions.DTOs;

namespace TransformationFunctions.Activities
{
    public static class Input
    {
        [FunctionName("ReadInputFileFromBlob")]
        public static InputFormat[] ReadInputFileFromBlob([ActivityTrigger] string unused, ILogger log)
        {
            // NOTE: Make sure the function app MSI has "Storage Blob Data Reader" (or more) rights
            //       on the storage container. Right now, the deployment script does *not* do this yet.
            var filename = Environment.GetEnvironmentVariable("DataInStorageFileName");
            var storageAccountName = Environment.GetEnvironmentVariable("DataInStorageAccount");
            var storageContainerName = Environment.GetEnvironmentVariable("DataInStorageContainerName");
            var isRunningLocally = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                .Contains("UseDevelopmentStorage=true");


            log.LogInformation($"ReadInputFileFromBlob is going to read from: account location: " +
                               $"{storageAccountName} container: {storageContainerName} file: {filename}");

            var containerClient = isRunningLocally ?
                new BlobContainerClient(storageAccountName, storageContainerName) :
                new BlobContainerClient(new Uri($"{storageAccountName}{storageContainerName}"), new DefaultAzureCredential());

            var blobClient = containerClient.GetBlobClient(filename);
            var result = new List<InputFormat>();
            if (blobClient.Exists())
            {
                var response = blobClient.Download();
                using var streamReader = new StreamReader(response.Value.Content);
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    result.Add(new InputFormat(line));
                }
            } else
            {
                var msg = $"Could not find file {filename} in container {storageAccountName}/{storageContainerName}";
                log.LogError(msg);
                throw new Exception(msg);
            }
            log.LogInformation($"BLOB file successfully read - found {result.Count} lines");
            log.LogMetric("LinesRead", result.Count);
            return result.ToArray();
        }
    }
}