//using EtherGizmos.Common;
//using EtherGizmos.Common.Abstractions;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;

//namespace EtherGizmos.Common;

//internal class Tests
//{
//    [Test]
//    public async Task Test()
//    {
//        var services = new ServiceCollection();

//        services.AddConnectionResolver()
//            .WithPostgreSql();

//        var config = new ConfigurationManager();
//        config.AddInMemoryCollection(new Dictionary<string, string?>()
//        {
//            ["Connections:TestDb:Type"] = "Database",
//            ["Connections:TestDb:PostgreSql:ConnectionString"] = "host=fake_host;port=1234;database=fake_db;user id=fake_user;password=fake_password",
//        });

//        services.AddSingleton<IConfiguration>(config);

//        var provider = services.BuildServiceProvider();

//        var resolver = provider.GetRequiredService<IConnectionResolver>();

//        var connection1 = resolver.GetDatabaseConnection("TestDb");

//        if (connection1.IsPostgreSql(out var postgres))
//        {
//            //I am PostgreSQL
//        }

//        //Was added via extension members, otherwise there are no valid connection types
//        var iAmNew = ConnectionType.Database;

//        var connection2 = resolver.CreateDbConnection("TestDb");
//    }
//}
