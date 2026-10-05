using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.Messaging;
namespace AcademiaDoZe.Presentation.AppMaui.Configuration;

public static class ConfigurationHelper
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var (connectionString, databaseType) = ObterConfiguracaoAtual();
        var repoConfig = new RepositoryConfig
        {
            ConnectionString = connectionString,
            DatabaseType = databaseType.ToInfrastructure()
        };
        // Configura a fábrica de repositórios com a string de conexão e tipo de banco
        services.AddSingleton(repoConfig);
        // Assina mensagens de alteração de banco de dados para atualizar o RepositoryConfig diretamente
        WeakReferenceMessenger.Default.Register<RepositoryConfig, BancoPreferencesUpdatedMessage>(repoConfig, (r, m) =>
        {
            var (novaConnStr, novoDbType) = ObterConfiguracaoAtual();
            r.ConnectionString = novaConnStr;
            r.DatabaseType = novoDbType.ToInfrastructure();
        });
        // Configura os serviços da camada de aplicação
        services.AddApplicationServices();
    }
    /// <summary>
    /// Obtém a Connection String e o AppDatabaseType ativos a partir das Preferences do usuário,
    /// com valores padrão seguros para cada um dos 3 gerenciadores (Sqlite, MySql e SqlServer).
    /// </summary>
    public static (string ConnectionString, AppDatabaseType DatabaseType) ObterConfiguracaoAtual()
    {
        var databaseTypeStr = Preferences.Get("DatabaseType", AppDatabaseType.Sqlite.ToString());
        if (!Enum.TryParse(databaseTypeStr, out AppDatabaseType databaseType))
        {
            databaseType = AppDatabaseType.Sqlite;
        }

        string connectionString;
        if (databaseType == AppDatabaseType.Sqlite)
        {
            // CORREÇÃO 1: Usa sempre a AppDataDirectory que garante a criação do ficheiro sem erros
            var defaultDbPath = Path.Combine(FileSystem.AppDataDirectory, "db_academia_do_ze.db");

            var dbPath = Preferences.Get("Sqlite_Caminho", Preferences.Get("SqliteCaminho", defaultDbPath));
            if (string.IsNullOrWhiteSpace(dbPath))
                dbPath = defaultDbPath;

            var complemento = Preferences.Get("Sqlite_Complemento", Preferences.Get("Complemento", "Default Timeout=5;"));
            connectionString = $"Data Source={dbPath};{complemento}";
        }
        else
        {
            var prefix = databaseType == AppDatabaseType.SqlServer ? "SqlServer" : "MySql";

            // CORREÇÃO 2: Altera para localhost (a sua máquina local). 
            // Nota: Se usar o emulador Android, mude "localhost" para "10.0.2.2"
            var defaultServer = "localhost";

            var defaultUser = databaseType == AppDatabaseType.SqlServer ? "sa" : "root";
            var defaultComplemento = databaseType == AppDatabaseType.SqlServer
            ? "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;Connection Timeout=5;"
            : "Connection Timeout=5;Default Command Timeout=30;";

            var dbServer = Preferences.Get($"{prefix}_Servidor", Preferences.Get("Servidor", defaultServer));
            var dbDatabase = Preferences.Get($"{prefix}_Banco", Preferences.Get("Banco", "db_academia_do_ze"));
            var dbUser = Preferences.Get($"{prefix}_Usuario", Preferences.Get("Usuario", defaultUser));
            var dbPassword = Preferences.Get($"{prefix}_Senha", Preferences.Get("Senha", "abcBolinhas12345")); // Verifique se esta é mesmo a sua palavra-passe
            var dbComplemento = Preferences.Get($"{prefix}_Complemento", Preferences.Get("Complemento", defaultComplemento));

            connectionString = $"Server={dbServer};Database={dbDatabase};User Id={dbUser};Password={dbPassword};{dbComplemento}";
        }
        return (connectionString, databaseType);
    } 
}