using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccess.Infrastructure;

public static class SqlAppLock
{
    public static async Task WithExclusiveLockAsync(
        string masterConnectionString,
        string resourceName,
        Func<CancellationToken, Task> action,
        int timeoutMs = 60000,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(masterConnectionString);
        await conn.OpenAsync(ct);

        await using (var cmd = new SqlCommand("sp_getapplock", conn) { CommandType = CommandType.StoredProcedure })
        {
            cmd.Parameters.AddWithValue("@Resource", resourceName);
            cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
            cmd.Parameters.AddWithValue("@LockOwner", "Session");
            cmd.Parameters.AddWithValue("@LockTimeout", timeoutMs);

            var ret = cmd.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
            ret.Direction = ParameterDirection.ReturnValue;

            await cmd.ExecuteNonQueryAsync(ct);

            var code = (int)(ret.Value ?? -999);
            if (code < 0)
                throw new TimeoutException($"sp_getapplock failed ({code}) for '{resourceName}'.");
        }

        try
        {
            await action(ct);
        }
        finally
        {
            await using var rel = new SqlCommand("sp_releaseapplock", conn) { CommandType = CommandType.StoredProcedure };
            rel.Parameters.AddWithValue("@Resource", resourceName);
            rel.Parameters.AddWithValue("@LockOwner", "Session");
            await rel.ExecuteNonQueryAsync(ct);
        }
    }
}
