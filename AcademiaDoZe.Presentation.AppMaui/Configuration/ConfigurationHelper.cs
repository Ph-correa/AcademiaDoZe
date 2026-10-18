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
    /// Obtém a Connection String e o AppDatabaseType ativos configurados exclusivamente para o SQLite.
    /// </summary>
    public static (string ConnectionString, AppDatabaseType DatabaseType) ObterConfiguracaoAtual()
    {
        // Força o tipo de banco para Sqlite
        var databaseType = AppDatabaseType.Sqlite;

        // Garante que o Preferences armazene o SQLite como padrão
        Preferences.Set("DatabaseType", databaseType.ToString());

        // Obtém o caminho do arquivo físico (.db) na pasta de dados isolada do dispositivo
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "db_academia_do_ze.db");

        // String de conexão limpa (sem parâmetros incompatíveis como Default Timeout)
        string connectionString = $"Data Source={dbPath};";

        return (connectionString, databaseType);
    }
}