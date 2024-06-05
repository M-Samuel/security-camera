using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using SecurityCamera.Domain.ObjectDetectionDomain;

namespace SecurityCamera.Infrastructure.AzureStorageTable;

public class TableClientsProvider
{
    private IDictionary<string, TableClient> _tableClientsDictionary = new Dictionary<string, TableClient>();

    public TableClientsProvider(IConfiguration configuration)
    {
        TableServiceClient tableServiceClient = new TableServiceClient(configuration[nameof(EnvVars.AzureTableStorageConnectionString)]);
        InitializeTableClients(tableServiceClient);

    }

    private void InitializeTableClients(TableServiceClient tableServiceClient)
    {
        _tableClientsDictionary.Add($"{nameof(ImageDetection)}s", tableServiceClient.GetTableClient(tableName: $"{nameof(ImageDetection)}s"));

        foreach(TableClient tableClient in _tableClientsDictionary.Values)
            tableClient.CreateIfNotExists();
    }

    public TableClient GetTableClientByKey(string key)
    {
        _tableClientsDictionary.TryGetValue(key, out TableClient? tableClient);
        if(tableClient == null)
            throw new KeyNotFoundException(key);
        return tableClient;
    }
}