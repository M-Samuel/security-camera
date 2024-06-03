using Azure.Data.Tables;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Infrastructure.AzureStorageTable;

public class TransactionManager : IUnitOfWork
{

    private List<TableTransactionAction> transactionActions = new List<TableTransactionAction>();
    public Task<int> SaveAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}