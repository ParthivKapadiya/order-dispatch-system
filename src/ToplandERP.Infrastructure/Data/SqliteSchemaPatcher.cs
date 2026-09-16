using Microsoft.EntityFrameworkCore;

namespace ToplandERP.Infrastructure.Data;

public static class SqliteSchemaPatcher
{
    public static async Task ApplyAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlite())
        {
            return;
        }

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await AddColumnIfMissingAsync(dbContext, "Customers", "CustomerCode", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Customers", "BillingCity", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Customers", "BillingState", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Customers", "BillingPincode", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Customers", "DeliveryAddress", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Customers", "DeliveryCity", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Customers", "DeliveryState", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Customers", "DeliveryPincode", "TEXT NOT NULL DEFAULT ''");

            await AddColumnIfMissingAsync(dbContext, "Products", "Category", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Products", "ModelNumber", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Products", "Unit", "TEXT NULL");

            await AddColumnIfMissingAsync(dbContext, "Transporters", "ContactPerson", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Transporters", "Address", "TEXT NULL");

            await AddColumnIfMissingAsync(dbContext, "AspNetUsers", "EmployeeCode", "TEXT NOT NULL DEFAULT ''");

            await AddColumnIfMissingAsync(dbContext, "Orders", "TransporterId", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "CreatedByUserId", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "CustomerCode", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "CustomerName", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "CustomerMobile", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BillingAddress", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BillingCity", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BillingState", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BillingPincode", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "DeliveryAddress", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "DeliveryCity", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "DeliveryState", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "DeliveryPincode", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BillAmount", "TEXT NOT NULL DEFAULT '0'");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BookingNumber", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BookingDate", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BookingFrom", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BookingTo", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "BookingDetails", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Orders", "SpecialInstructions", "TEXT NULL");

            await AddColumnIfMissingAsync(dbContext, "OrderItems", "ProductCode", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "OrderItems", "ProductName", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "OrderItems", "ModelNumber", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "OrderItems", "Unit", "TEXT NULL");

            await AddColumnIfMissingAsync(dbContext, "OrderModificationRequests", "RequestedByName", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "OrderModificationRequests", "CurrentSnapshotJson", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "OrderModificationRequests", "RequestedChangesJson", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "OrderModificationRequests", "ReviewedByUserId", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "OrderModificationRequests", "ReviewedByName", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "OrderModificationRequests", "ReviewedAt", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "OrderModificationRequests", "RejectionReason", "TEXT NULL");

            await AddColumnIfMissingAsync(dbContext, "Dispatches", "DispatchDate", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Dispatches", "DispatchedByUserId", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Dispatches", "DispatchPersonName", "TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(dbContext, "Dispatches", "LrNumber", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Dispatches", "BookingNumber", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Dispatches", "IsCompleted", "INTEGER NOT NULL DEFAULT 0");

            await AddColumnIfMissingAsync(dbContext, "DispatchDocuments", "UploadedByUserId", "TEXT NULL");

            await AddColumnIfMissingAsync(dbContext, "Notifications", "Type", "INTEGER NOT NULL DEFAULT 0");
            await AddColumnIfMissingAsync(dbContext, "Notifications", "RelatedEntityType", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Notifications", "RelatedEntityId", "TEXT NULL");
            await AddColumnIfMissingAsync(dbContext, "Notifications", "EventKey", "TEXT NULL");

            await CreateIndexIfMissingAsync(dbContext, "IX_Customers_CompanyId_CustomerCode",
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_Customers_CompanyId_CustomerCode ON Customers (CompanyId, CustomerCode)");
            await CreateIndexIfMissingAsync(dbContext, "IX_Products_CompanyId_Code",
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_Products_CompanyId_Code ON Products (CompanyId, Code)");
            await CreateIndexIfMissingAsync(dbContext, "IX_PaymentConditions_CompanyId_Name",
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentConditions_CompanyId_Name ON PaymentConditions (CompanyId, Name)");
            await CreateIndexIfMissingAsync(dbContext, "IX_AspNetUsers_CompanyId_EmployeeCode",
                "CREATE INDEX IF NOT EXISTS IX_AspNetUsers_CompanyId_EmployeeCode ON AspNetUsers (CompanyId, EmployeeCode)");
            await CreateIndexIfMissingAsync(dbContext, "IX_Notifications_UserId_IsRead_CreatedAt",
                "CREATE INDEX IF NOT EXISTS IX_Notifications_UserId_IsRead_CreatedAt ON Notifications (UserId, IsRead, CreatedAt)");
            await CreateIndexIfMissingAsync(dbContext, "IX_Notifications_EventKey",
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_Notifications_EventKey ON Notifications (EventKey)");
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static async Task AddColumnIfMissingAsync(
        ApplicationDbContext dbContext,
        string table,
        string column,
        string definition)
    {
        if (await ColumnExistsAsync(dbContext, table, column))
        {
            return;
        }

#pragma warning disable EF1002
        await dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition}");
#pragma warning restore EF1002
    }

    private static async Task CreateIndexIfMissingAsync(ApplicationDbContext dbContext, string indexName, string sql)
    {
        if (await IndexExistsAsync(dbContext, indexName))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task<bool> ColumnExistsAsync(ApplicationDbContext dbContext, string table, string column)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table})";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> IndexExistsAsync(ApplicationDbContext dbContext, string indexName)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND name = @name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = indexName;
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync();
        return result is string;
    }
}
