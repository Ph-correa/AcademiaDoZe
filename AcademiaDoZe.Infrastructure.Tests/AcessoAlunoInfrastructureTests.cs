using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoAlunoInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _logradouroRepo;
    private readonly AlunoRepository _alunoRepo;
    private readonly AcessoAlunoRepository _acessoRepo;

    public AcessoAlunoInfrastructureTests()
    {
        _logradouroRepo = new LogradouroRepository(ConnectionString, DatabaseType);
        _alunoRepo = new AlunoRepository(ConnectionString, DatabaseType);
        _acessoRepo = new AcessoAlunoRepository(ConnectionString, DatabaseType);
    }

    private async Task<AcessoAluno> CriarEInserirAcessoAsync(Aluno aluno, DateTime dataHora)
    {
        var result = AcessoAluno.Criar(0, aluno, dataHora);
        if (result.IsFailure)
        {
            throw new Exception($"Falha ao criar AcessoAluno no Helper: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return await _acessoRepo.Adicionar(result.Value!);
    }

    // MÉTODOS DOS TESTES AQUI
    [Fact]
    public async Task AcessoAluno_Adicionar_E_ObterPorId_Sucesso()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var dataHora = DateTime.Today.AddHours(8);

        var inserido = await CriarEInserirAcessoAsync(aluno, dataHora);

        Assert.NotNull(inserido);
        Assert.True(inserido.Id > 0);
        var obtido = await _acessoRepo.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(aluno.Id, obtido.AlunoId);
        Assert.Equal(dataHora, obtido.DataHora);
    }

    [Fact]
    public async Task AcessoAluno_ObterPorId_Inexistente_RetornaNulo()
    {
        var obtido = await _acessoRepo.ObterPorId(int.MaxValue);
        Assert.Null(obtido);
    }

    [Fact]
    public async Task AcessoAluno_ObterTodos_ContemInserido()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var inserido = await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(9));

        var todos = await _acessoRepo.ObterTodos();

        Assert.Contains(todos, a => a.Id == inserido.Id);
    }

    [Fact]
    public async Task AcessoAluno_Atualizar_Sucesso()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var inserido = await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(8));
        var novaDataHora = DateTime.Today.AddHours(10).AddMinutes(30);
        var alterado = AcessoAluno.Criar(inserido.Id, aluno, novaDataHora).Value!;

        await _acessoRepo.Atualizar(alterado);

        var obtido = await _acessoRepo.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(novaDataHora, obtido.DataHora);
    }

    [Fact]
    public async Task AcessoAluno_Remover_Sucesso()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var inserido = await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(8));

        var removido = await _acessoRepo.Remover(inserido.Id);

        Assert.True(removido);
        Assert.Null(await _acessoRepo.ObterPorId(inserido.Id));
    }

    [Fact]
    public async Task AcessoAluno_EstaNaAcademia_AlternaEntreEntradaESaida()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        Assert.False(await _acessoRepo.EstaNaAcademia(aluno.Id)); // sem registros

        await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(8)); // entrada
        Assert.True(await _acessoRepo.EstaNaAcademia(aluno.Id));

        await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(9)); // saída
        Assert.False(await _acessoRepo.EstaNaAcademia(aluno.Id));
    }

    [Fact]
    public async Task AcessoAluno_ObterUltimoAcesso_RetornaMaisRecente()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        Assert.Null(await _acessoRepo.ObterUltimoAcesso(aluno.Id));

        await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddDays(-1).AddHours(8));
        var ultimo = await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(7));

        var obtido = await _acessoRepo.ObterUltimoAcesso(aluno.Id);

        Assert.NotNull(obtido);
        Assert.Equal(ultimo.Id, obtido.Id);
    }

    [Fact]
    public async Task AcessoAluno_ObterAcessosPorAlunoPeriodo_FiltraPorDia()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddDays(-2).AddHours(8));
        await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(8));
        await CriarEInserirAcessoAsync(aluno, DateTime.Today.AddHours(21).AddMinutes(59));

        var deHoje = (await _acessoRepo.ObterAcessosPorAlunoPeriodo(aluno.Id, hoje, hoje)).ToList();
        var todosDoAluno = (await _acessoRepo.ObterAcessosPorAlunoPeriodo(aluno.Id)).ToList();

        Assert.Equal(2, deHoje.Count);
        Assert.True(deHoje[0].DataHora < deHoje[1].DataHora); // ordem cronológica
        Assert.Equal(3, todosDoAluno.Count);
        Assert.All(todosDoAluno, a => Assert.Equal(aluno.Id, a.AlunoId));
    }

    [Fact]
    public async Task AcessoAluno_Relatorios_HorarioMaisProcurado_E_PermanenciaMedia()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var entrada = DateTime.Today.AddHours(7).AddMinutes(15);
        await CriarEInserirAcessoAsync(aluno, entrada);            // entrada 07:15
        await CriarEInserirAcessoAsync(aluno, entrada.AddHours(1)); // saída 08:15

        var horarios = await _acessoRepo.ObterHorarioMaisProcuradoPorMes(entrada.Month);
        var permanencia = await _acessoRepo.ObterPermanenciaMediaPorMes(entrada.Month);

        Assert.True(horarios.ContainsKey(new TimeOnly(7, 0)));
        Assert.True(horarios[new TimeOnly(7, 0)] >= 1);
        Assert.True(permanencia.ContainsKey(entrada.Month));
        Assert.True(permanencia[entrada.Month] > TimeSpan.Zero);
    }

    [Fact]
    public async Task AcessoAluno_ObterAlunosSemAcessoNosUltimosDias()
    {
        var alunoSemAcesso = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var alunoComAcesso = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        await CriarEInserirAcessoAsync(alunoComAcesso, DateTime.Today.AddHours(8));

        var semAcesso = (await _acessoRepo.ObterAlunosSemAcessoNosUltimosDias(7)).ToList();

        Assert.Contains(semAcesso, a => a.Id == alunoSemAcesso.Id);
        Assert.DoesNotContain(semAcesso, a => a.Id == alunoComAcesso.Id);
    }
}
