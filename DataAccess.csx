public class SqlConnectionPoolInfo
{
    public Address Address;
    public int TotalObjects;
    public int MinPoolSize;
    public int MaxPoolSize;
    public List<SqlConnection> ActiveConnections;
    public SqlConnectionOptions ConnectionOptions;
}

public class SqlConnectionOptions
{
    public Address Address;
    public string ConnectionString;
}

public class SqlConnection
{
    public Address Address;
    public ConnectionState State;
    public Address OwningObject;
}

[Flags()]
public enum ConnectionState
{
    Closed = 0,
    Open = 1,
    Connecting = 2,
    Executing = 4,
    Fetching = 8,
    Broken = 16,
}

public static List<SqlConnectionPoolInfo> GetDbConnections(this ClrRuntime clrRuntime)
{
    var pools = new List<SqlConnectionPoolInfo>();

    // Get System.Data.ProviderBase.DbConnectionPool and Microsoft.Data.ProviderBase.DbConnectionPool types
    foreach (var poolClrObject in clrRuntime.Heap.EnumerateObjects())
    {
        // If heap corruption, continue past this object.
        if (!poolClrObject.IsValid || !poolClrObject.IsOneOf("System.Data.ProviderBase.DbConnectionPool", "Microsoft.Data.ProviderBase.DbConnectionPool"))
            continue;

        var poolOptionsClrObject = poolClrObject.ReadObjectField("_connectionPoolGroupOptions");
        var connectionOptionsClrObject = poolClrObject.ReadObjectField("_connectionPoolGroup").ReadObjectField("_connectionOptions");

        var objectListClrObject = poolClrObject.ReadObjectField("_objectList");
        var activeConnectionsSize = objectListClrObject.ReadField<int>("_size");
        var activeConnectionClrObjects = objectListClrObject.ReadObjectField("_items").AsArray().ReadValues<ulong>(0, activeConnectionsSize);
        var activeConnections = new List<SqlConnection>(activeConnectionClrObjects.Length);

        foreach (var activeConnectionClrObjectAddress in activeConnectionClrObjects)
        {
            var activeConnectionClrObject = clrRuntime.Heap.GetObject(activeConnectionClrObjectAddress);

            activeConnections.Add(new()
            {
                Address = activeConnectionClrObjectAddress,
                State = activeConnectionClrObject.ReadField<ConnectionState>("_state"),
                OwningObject = activeConnectionClrObject.ReadObjectField("_owningObject").GetWeakReferenceTargetAddress(),
            });
        }

        var connectionPoolGroupClrObject = poolClrObject.ReadObjectField("_connectionPoolGroup");

        pools.Add(new()
        {
            Address = poolClrObject.Address,
            TotalObjects = poolClrObject.ReadField<int>("_totalObjects"),
            MinPoolSize = poolOptionsClrObject.ReadField<int>("_minPoolSize"),
            MaxPoolSize = poolOptionsClrObject.ReadField<int>("_maxPoolSize"),
            ConnectionOptions = new()
            {
                Address = connectionPoolGroupClrObject.ReadField<ulong>("_connectionOptions"),
                ConnectionString = connectionPoolGroupClrObject.ReadObjectField("_connectionOptions").ReadStringField("_usersConnectionString"),
            },
            ActiveConnections = activeConnections,
        }); ;
    }

    return pools;
}
