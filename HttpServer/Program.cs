using HttpServer.Framework.Http;
using Npgsql;

//раскоментируйте перед метод снизу первым запуском!
//CreateDatabase();

HTTPServer server = new HTTPServer();

server.ManageServer();

static void CreateDatabase()
{
    string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=piskoed228";
    string targetDbName = "myDataBase";

    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var checkCommand = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{targetDbName}'", connection);
        if (checkCommand.ExecuteScalar() == null)
        {
            using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{targetDbName}\"", connection);
            createCommand.ExecuteNonQuery();
            Console.WriteLine($"База {targetDbName} создана");
        }
        else
        {
            Console.WriteLine($"База {targetDbName} уже есть");
        }

        string targerConnectionString =  $"Host=localhost;Port=5432;Username=postgres;Password=piskoed228;Database={targetDbName}";
        using var targetConnection = new NpgsqlConnection(targerConnectionString);
        targetConnection.Open();

        if (File.Exists("init.sql"))
        {
            string sqlScript = File.ReadAllText("init.sql");
            using var initCommand = new NpgsqlCommand(sqlScript, targetConnection);
            initCommand.ExecuteNonQuery();
            Console.WriteLine("Таблицы и данные init.sql загружены");
        }
        else { Console.WriteLine("файл init.sql не найдет"); }
    }
    catch(Exception ex)
    {
        Console.WriteLine($"ошибка создании бд: {ex.Message}");
    }
}