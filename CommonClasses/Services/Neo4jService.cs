using Microsoft.Extensions.Configuration;
using Neo4j.Driver;

public class Neo4jService : IDisposable
{
    private readonly IDriver _driver; 


    public Neo4jService(IConfiguration configuration)
    {
        var neo4jUri = configuration.GetConnectionString("Uri");
        var neo4jUser = configuration.GetConnectionString("Username");
        var neo4jPassword = configuration.GetConnectionString("Password");

        _driver = GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPassword));
        
    }

    
    
    public async Task<List <object>> ReadQuery(string query, Dictionary<string,object> parameters)
    {
        var session = _driver.AsyncSession();
        var results = new List<object>();

        try
        {
            var result = await session.RunAsync(query, parameters);

            await result.ForEachAsync(record =>
            {
                var row = new object();

                foreach (var key in record.Keys)
                {
                    var value = record[key];

                    // If the value is a node, add its properties to the row
                    if (value is INode node)
                    {
                        var nodeProperties = new Dictionary<string, object>();

                        // Add each property from the node into the nodeProperties dictionary
                        foreach (var property in node.Properties)
                        {
                            nodeProperties[property.Key] = property.Value;
                        }

                        // Store the properties dictionary under the key
                        row = nodeProperties;
                    }
                    else
                    {
                        // For non-node values, add them directly to the row
                        row = value;
                    }
                }

                results.Add(row);
            });
        }
        finally
        {
            await session.CloseAsync();
        }

        return results;
    }

    public async void WriteQuery(string query, Dictionary<string,object> parameters)
    {
        var session = _driver.AsyncSession();
        try
        {
            await session.RunAsync(query, parameters);

        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}