using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
namespace AcademiaDoZe.Infrastructure.Repositories;

/*
Entrada/Saída de alunos.
A tabela tb_acesso guarda UM registro por passagem na catraca (pessoa_tipo = 0 para aluno).
Dentro de um mesmo dia os registros se alternam: 1º = entrada, 2º = saída, 3º = entrada, 4º = saída...
Por isso: quantidade ímpar de registros no dia = aluno está dentro da academia.
*/
public class AcessoAlunoRepository : BaseRepository, IAcessoAlunoRepository
{
    private const int PessoaTipoAluno = 0;

    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    private static string BaseSelectQuery => $@"
SELECT
ac.id_acesso, ac.pessoa_tipo, ac.pessoa_id, ac.data_hora,
a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email,
a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_acesso ac
INNER JOIN tb_aluno a ON ac.pessoa_id = a.id_aluno
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
WHERE ac.pessoa_tipo = {PessoaTipoAluno}";

    public static AcessoAluno Map(DbDataReader reader)
    {
        try
        {
            int id = reader.GetInt32Value("id_acesso");
            DateTime dataHora = reader.GetDateTimeValue("data_hora");
            var aluno = AlunoRepository.Map(reader);
            var result = AcessoAluno.Criar(id, aluno, dataHora);
            if (result.IsFailure)
            {
                throw new InfrastructureException("ERRO_DOMINIO_MAPEAMENTO", $"Erro de domínio ao mapear acesso de aluno ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
            }
            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_ALUNO", $"Erro ao mapear dados do acesso de aluno: {ex.Message}", ex);
        }
    }

    private async Task<List<AcessoAluno>> ListarAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var acessos = new List<AcessoAluno>();
        while (await reader.ReadAsync(cancellationToken))
        {
            acessos.Add(Map(reader));
        }
        return acessos;
    }

    #region CRUD (IRepository)

    public async Task<AcessoAluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
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
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter acesso de aluno por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            return await ListarAsync(command, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", $"Erro ao obter todos os acessos de alunos: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Adicionar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@PessoaTipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_ALUNO", "Falha ao obter ID inserido para o acesso do aluno.", cancellationToken);
            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_ALUNO", $"Erro ao adicionar acesso do aluno ID {entity.AlunoId}: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Atualizar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"UPDATE tb_acesso SET pessoa_id = @PessoaId, data_hora = @DataHora WHERE id_acesso = @Id AND pessoa_tipo = {PessoaTipoAluno}";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso de aluno encontrado com o ID {entity.Id} para atualização.");
            }
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_ALUNO", $"Erro ao atualizar acesso de aluno ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = {PessoaTipoAluno}";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_ALUNO", $"Erro ao remover acesso de aluno ID {id}: {ex.Message}", ex);
        }
    }

    #endregion

    #region Métodos específicos do domínio

    public async Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = BaseSelectQuery;
            if (alunoId.HasValue) query += " AND ac.pessoa_id = @AlunoId";
            if (inicio.HasValue) query += " AND ac.data_hora >= @Inicio";
            if (fim.HasValue) query += " AND ac.data_hora < @FimExclusivo";
            query += " ORDER BY ac.data_hora, ac.id_acesso";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            if (alunoId.HasValue) command.AddParameter("@AlunoId", alunoId.Value, DbType.Int32);
            // intervalo [início 00:00, dia seguinte ao fim 00:00) -> inclui o dia final inteiro
            if (inicio.HasValue) command.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            if (fim.HasValue) command.AddParameter("@FimExclusivo", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            return await ListarAsync(command, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos de alunos por período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} AND ac.pessoa_id = @AlunoId ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter último acesso do aluno ID {alunoId}: {ex.Message}", ex);
        }
    }

    public async Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            // registros de hoje: quantidade ímpar = entrou e ainda não saiu
            var hoje = DateTime.Today;
            string query = $"SELECT COUNT(1) FROM tb_acesso WHERE pessoa_tipo = {PessoaTipoAluno} AND pessoa_id = @AlunoId AND data_hora >= @Inicio AND data_hora < @FimExclusivo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            command.AddParameter("@Inicio", hoje, DbType.DateTime);
            command.AddParameter("@FimExclusivo", hoje.AddDays(1), DbType.DateTime);
            var total = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return total % 2 == 1;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ESTA_NA_ACADEMIA", $"Erro ao verificar se o aluno ID {alunoId} está na academia: {ex.Message}", ex);
        }
    }

    // Lê apenas (aluno, data/hora) do mês informado, sem JOIN, para os relatórios
    private async Task<List<(int AlunoId, DateTime DataHora)>> ObterRegistrosDoMes(int mes, CancellationToken cancellationToken)
    {
        string query = $"SELECT pessoa_id, data_hora FROM tb_acesso WHERE pessoa_tipo = {PessoaTipoAluno} AND {GetDateMonthExpression("data_hora")} = @Mes ORDER BY pessoa_id, data_hora, id_acesso";
        await using var command = await CreateCommandAsync(query, cancellationToken);
        command.AddParameter("@Mes", mes, DbType.Int32);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var registros = new List<(int, DateTime)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            registros.Add((reader.GetInt32Value("pessoa_id"), reader.GetDateTimeValue("data_hora")));
        }
        return registros;
    }

    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var registros = await ObterRegistrosDoMes(mes, cancellationToken);
            // somente as ENTRADAS (posições ímpares de cada aluno em cada dia), agrupadas pela hora cheia
            return registros
                .GroupBy(r => (r.AlunoId, r.DataHora.Date))
                .SelectMany(g => g.Where((_, indice) => indice % 2 == 0))
                .GroupBy(r => new TimeOnly(r.DataHora.Hour, 0))
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_HORARIO_MAIS_PROCURADO", $"Erro ao obter horário mais procurado do mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var registros = await ObterRegistrosDoMes(mes, cancellationToken);
            // cada par (entrada, saída) do mesmo aluno no mesmo dia é uma permanência
            var permanencias = new List<TimeSpan>();
            foreach (var grupo in registros.GroupBy(r => (r.AlunoId, r.DataHora.Date)))
            {
                var doDia = grupo.ToList();
                for (int i = 0; i + 1 < doDia.Count; i += 2)
                {
                    permanencias.Add(doDia[i + 1].DataHora - doDia[i].DataHora);
                }
            }
            var media = permanencias.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)permanencias.Average(p => p.Ticks));
            return new Dictionary<int, TimeSpan> { [mes] = media };
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_PERMANENCIA_MEDIA", $"Erro ao obter permanência média do mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(int dias, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $@"
SELECT
a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email,
a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_aluno a
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
WHERE NOT EXISTS (
    SELECT 1 FROM tb_acesso ac
    WHERE ac.pessoa_tipo = {PessoaTipoAluno} AND ac.pessoa_id = a.id_aluno AND ac.data_hora >= @Limite)
ORDER BY a.nome";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Limite", DateTime.Today.AddDays(-dias), DbType.DateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var alunos = new List<Aluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                alunos.Add(AlunoRepository.Map(reader));
            }
            return alunos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ALUNOS_SEM_ACESSO", $"Erro ao obter alunos sem acesso nos últimos {dias} dias: {ex.Message}", ex);
        }
    }

    #endregion
}
