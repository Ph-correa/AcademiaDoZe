using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
namespace AcademiaDoZe.Infrastructure.Repositories;

/*
Entrada/Saída de colaboradores.
A tabela tb_acesso guarda UM registro por passagem (pessoa_tipo = 1 para colaborador).
Dentro de um mesmo dia os registros se alternam: 1º = entrada, 2º = saída, 3º = entrada, 4º = saída...
*/
public class AcessoColaboradorRepository : BaseRepository, IAcessoColaboradorRepository
{
    private const int PessoaTipoColaborador = 1;

    public AcessoColaboradorRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    private static string BaseSelectQuery => $@"
SELECT
ac.id_acesso, ac.pessoa_tipo, ac.pessoa_id, ac.data_hora,
c.id_colaborador, c.cpf, c.nome, c.nascimento, c.telefone, c.email,
c.logradouro_id, c.numero, c.complemento, c.senha, c.foto, c.admissao, c.tipo, c.vinculo,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_acesso ac
INNER JOIN tb_colaborador c ON ac.pessoa_id = c.id_colaborador
INNER JOIN tb_logradouro l ON c.logradouro_id = l.id_logradouro
WHERE ac.pessoa_tipo = {PessoaTipoColaborador}";

    public static AcessoColaborador Map(DbDataReader reader)
    {
        try
        {
            int id = reader.GetInt32Value("id_acesso");
            DateTime dataHora = reader.GetDateTimeValue("data_hora");
            var colaborador = ColaboradorRepository.Map(reader);
            var result = AcessoColaborador.Criar(id, colaborador, dataHora);
            if (result.IsFailure)
            {
                throw new InfrastructureException("ERRO_DOMINIO_MAPEAMENTO", $"Erro de domínio ao mapear acesso de colaborador ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
            }
            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_COLABORADOR", $"Erro ao mapear dados do acesso de colaborador: {ex.Message}", ex);
        }
    }

    private async Task<List<AcessoColaborador>> ListarAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var acessos = new List<AcessoColaborador>();
        while (await reader.ReadAsync(cancellationToken))
        {
            acessos.Add(Map(reader));
        }
        return acessos;
    }

    #region CRUD (IRepository)

    public async Task<AcessoColaborador?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} AND ac.id_acesso = @Id";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter acesso de colaborador por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoColaborador>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            return await ListarAsync(command, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", $"Erro ao obter todos os acessos de colaboradores: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador> Adicionar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@PessoaTipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            command.AddParameter("@PessoaId", entity.ColaboradorId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_COLABORADOR", "Falha ao obter ID inserido para o acesso do colaborador.", cancellationToken);
            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_COLABORADOR", $"Erro ao adicionar acesso do colaborador ID {entity.ColaboradorId}: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador> Atualizar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"UPDATE tb_acesso SET pessoa_id = @PessoaId, data_hora = @DataHora WHERE id_acesso = @Id AND pessoa_tipo = {PessoaTipoColaborador}";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaId", entity.ColaboradorId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso de colaborador encontrado com o ID {entity.Id} para atualização.");
            }
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_COLABORADOR", $"Erro ao atualizar acesso de colaborador ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = {PessoaTipoColaborador}";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_COLABORADOR", $"Erro ao remover acesso de colaborador ID {id}: {ex.Message}", ex);
        }
    }

    #endregion

    #region Métodos específicos do domínio

    public async Task<IEnumerable<AcessoColaborador>> ObterAcessosPorColaboradorPeriodo(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = BaseSelectQuery;
            if (colaboradorId.HasValue) query += " AND ac.pessoa_id = @ColaboradorId";
            if (inicio.HasValue) query += " AND ac.data_hora >= @Inicio";
            if (fim.HasValue) query += " AND ac.data_hora < @FimExclusivo";
            query += " ORDER BY ac.data_hora, ac.id_acesso";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            if (colaboradorId.HasValue) command.AddParameter("@ColaboradorId", colaboradorId.Value, DbType.Int32);
            // intervalo [início 00:00, dia seguinte ao fim 00:00) -> inclui o dia final inteiro
            if (inicio.HasValue) command.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            if (fim.HasValue) command.AddParameter("@FimExclusivo", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            return await ListarAsync(command, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos de colaboradores por período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador?> ObterUltimoAcesso(int colaboradorId, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} AND ac.pessoa_id = @ColaboradorId ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@ColaboradorId", colaboradorId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter último acesso do colaborador ID {colaboradorId}: {ex.Message}", ex);
        }
    }

    public async Task<TimeSpan> ObterHorasTrabalhadasNoDia(int colaboradorId, DateOnly data, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"SELECT data_hora FROM tb_acesso WHERE pessoa_tipo = {PessoaTipoColaborador} AND pessoa_id = @ColaboradorId AND data_hora >= @Inicio AND data_hora < @FimExclusivo ORDER BY data_hora, id_acesso";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@ColaboradorId", colaboradorId, DbType.Int32);
            command.AddParameter("@Inicio", data.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            command.AddParameter("@FimExclusivo", data.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var horarios = new List<DateTime>();
            while (await reader.ReadAsync(cancellationToken))
            {
                horarios.Add(reader.GetDateTimeValue("data_hora"));
            }

            // soma todos os pares (entrada, saída) do dia
            var total = TimeSpan.Zero;
            for (int i = 0; i + 1 < horarios.Count; i += 2)
            {
                total += horarios[i + 1] - horarios[i];
            }
            // se ainda está dentro (entrada sem saída) e o dia é hoje, conta até o momento atual
            if (horarios.Count % 2 == 1 && data == DateOnly.FromDateTime(DateTime.Today))
            {
                var emAberto = DateTime.Now - horarios[^1];
                if (emAberto > TimeSpan.Zero) total += emAberto;
            }
            return total;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_HORAS_TRABALHADAS", $"Erro ao obter horas trabalhadas do colaborador ID {colaboradorId} em {data:dd/MM/yyyy}: {ex.Message}", ex);
        }
    }

    #endregion
}
