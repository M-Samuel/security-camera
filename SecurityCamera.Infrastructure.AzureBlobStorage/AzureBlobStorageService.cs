using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecurityCamera.Domain.InfrastructureServices;

namespace SecurityCamera.Infrastructure.AzureBlobStorage;

public class AzureBlobStorageService : IRemoteStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    private readonly ConcurrentDictionary<string, BlobContainerClient> _blobContainerClients = new ();

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        string connectionString = configuration[nameof(EnvVars.AzureStorageConnectionString)] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(EnvVars.AzureStorageConnectionString), "Env var/Args is required");
        _blobServiceClient = new BlobServiceClient(connectionString);
    }
    
    public async Task<RemoteStorageContainer> CreateRemoteStorageContainer(string containerName, CancellationToken cancellationToken)
    {
        try
        {
            BlobContainerClient containerClient =
                await _blobServiceClient.CreateBlobContainerAsync(containerName, cancellationToken: cancellationToken);

            return new RemoteStorageContainer()
            {
                ContainerName = containerClient.Name
            };
        }
        catch (RequestFailedException rfe)
        {
            if(rfe.ErrorCode == "ContainerAlreadyExists")
            {
                return new RemoteStorageContainer()
                {
                    ContainerName = containerName
                };
            }
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadRemoteStorageFile failed");
            throw;
        }
    }

    private async Task<BlobContainerClient> GetContainerClient(string containerName, CancellationToken cancellationToken)
    {
        try
        {
            if (_blobContainerClients.TryGetValue(containerName, out var client))
                return client;
            
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            if (!await containerClient.ExistsAsync(cancellationToken: cancellationToken))
                throw new InvalidOperationException($"Container {containerName} does not exists");

            _blobContainerClients.TryAdd(containerName, containerClient);
            return containerClient;
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadRemoteStorageFile failed");
            throw;
        }
    }

    public async Task<RemoteStorageContainer> DeleteRemoteStorageContainer(string containerName, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _blobServiceClient.DeleteBlobContainerAsync(containerName, cancellationToken: cancellationToken);

            return new RemoteStorageContainer()
            {
                ContainerName = containerName,
                IsDeleted = !response.IsError
            };
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadRemoteStorageFile failed");
            throw;
        }
    }

    public async Task<RemoteStorageFile> UploadRemoteStorageFile(string containerName, string filePath, byte[] fileBytes, CancellationToken cancellationToken)
    {
        try
        {
            
            BlobContainerClient containerClient =
                await GetContainerClient(containerName, cancellationToken: cancellationToken);
            
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            BinaryData binaryData = new BinaryData(fileBytes);
            var response = await blobClient.UploadAsync(binaryData, true, cancellationToken);
            if (response == null)
                throw new InvalidOperationException($"Unable to upload file to {filePath}");
            if (!response.HasValue)
                throw new InvalidOperationException($"Unable to upload file to {filePath}");

            return new RemoteStorageFile()
            {
                ContainerName = containerName,
                FilePath = filePath
            };
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadRemoteStorageFile failed");
            throw;
        }
    }

    public async Task<RemoteStorageFile> UploadRemoteStorageFile(string containerName, string filePath, Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            
            BlobContainerClient containerClient =
                await GetContainerClient(containerName, cancellationToken: cancellationToken);
            
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            var response = await blobClient.UploadAsync(stream, true, cancellationToken);
            stream.Close();
            if (response == null)
                throw new InvalidOperationException($"Unable to upload file to {filePath}");
            if (!response.HasValue)
                throw new InvalidOperationException($"Unable to upload file to {filePath}");

            return new RemoteStorageFile()
            {
                ContainerName = containerName,
                FilePath = filePath
            };
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadRemoteStorageFile failed");
            throw;
        }
    }
    
    public async Task<RemoteStorageFile> UploadRemoteStorageLargeFile(string containerName, string filePath, Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            
            BlobContainerClient containerClient =
                await GetContainerClient(containerName, cancellationToken: cancellationToken);
            
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            
            var transferOptions = new StorageTransferOptions
            {
                // Set the maximum number of parallel transfer workers
                MaximumConcurrency = 2,

                // Set the initial transfer length to 8 MiB
                InitialTransferSize = 8 * 1024 * 1024,

                // Set the maximum length of a transfer to 4 MiB
                MaximumTransferSize = 4 * 1024 * 1024
            };

            var uploadOptions = new BlobUploadOptions()
            {
                TransferOptions = transferOptions
            };
            
            var response = await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);
            stream.Close();
            if (response == null)
                throw new InvalidOperationException($"Unable to upload file to {filePath}");
            if (!response.HasValue)
                throw new InvalidOperationException($"Unable to upload file to {filePath}");

            return new RemoteStorageFile()
            {
                ContainerName = containerName,
                FilePath = filePath
            };
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadRemoteStorageFile failed");
            throw;
        }
    }

    public async Task<RemoteStorageFile> DeleteRemoteStorageFile(string containerName, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            
            BlobContainerClient containerClient =
                await GetContainerClient(containerName, cancellationToken: cancellationToken);
            
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
           
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            if (response == null)
                throw new InvalidOperationException($"Unable to delete file to {filePath}");
            if (!response.HasValue)
                throw new InvalidOperationException($"Unable to delete file to {filePath}");

            return new RemoteStorageFile()
            {
                ContainerName = containerName,
                FilePath = filePath,
                FileDeleted = response.Value
            };
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadRemoteStorageFile failed");
            throw;
        }
    }

    public async IAsyncEnumerable<RemoteStorageFile> ListRemoteStorageFiles(string containerName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        BlobContainerClient containerClient =
            await GetContainerClient(containerName, cancellationToken: cancellationToken);
            
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            yield return new RemoteStorageFile()
            {
                ContainerName = containerName,
                FilePath = blobItem.Name
            };
        }
        
    }

    public async Task<RemoteStorageFile> DownloadRemoteStorageFile(string containerName, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            
            BlobContainerClient containerClient =
                await GetContainerClient(containerName, cancellationToken: cancellationToken);
            
            BlobClient blobClient = containerClient.GetBlobClient(filePath);

            var transferOptions = new StorageTransferOptions
            {
                // Set the maximum number of parallel transfer workers
                MaximumConcurrency = 2,

                // Set the initial transfer length to 1 MiB
                InitialTransferSize = 1 * 1024 * 1024,

                // Set the maximum length of a transfer to 1 MiB
                MaximumTransferSize = 1 * 1024 * 1024
            };

            BlobDownloadToOptions downloadOptions = new BlobDownloadToOptions()
            {
                TransferOptions = transferOptions
            };

            using MemoryStream memoryStream = new MemoryStream();

            var response = await blobClient.DownloadToAsync(memoryStream, downloadOptions, cancellationToken: cancellationToken);
            if (response == null)
                throw new InvalidOperationException($"Unable to download file to {filePath}");
            if (response.IsError)
                throw new InvalidOperationException($"Unable to download file to {filePath}");

            return new RemoteStorageFile()
            {
                ContainerName = containerName,
                FilePath = filePath,
                FileContent = memoryStream.ToArray()
            };
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogError($"HTTP error code {rfe.Status}: {rfe.ErrorCode}");
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DownloadRemoteStorageFile failed");
            throw;
        }
    }
}