using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoColaboradorInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _logradouroRepo;
    private readonly ColaboradorRepository _colaboradorRepo;
    private readonly AcessoColaboradorRepository _acessoRepo;

    public AcessoColaboradorInfrastructureTests()
    {
        _logradouroRepo = new LogradouroRepository(ConnectionString, DatabaseType);
        _colaboradorRepo = new ColaboradorRepository(ConnectionString, DatabaseType);
        _acessoRepo = new AcessoColaboradorRepository(ConnectionString, DatabaseType);
    }

    private async Task<AcessoColaborador> CriarEInserirAcessoAsync(Colaborador colaborador, DateTime dataHora)
    {
        var result = AcessoColaborador.Criar(0, colaborador, dataHora);
        if (result.IsFailure)
        {
            throw new Exception($"Falha ao criar AcessoColaborador no Helper: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return await _acessoRepo.Adicionar(result.Value!);
    }

    // MÉTODOS DOS TESTES AQUI
    [Fact]
    public async Task AcessoColaborador_Adicionar_E_ObterPorId_Sucesso()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        var dataHora = DateTime.Today.AddHours(8);

        var inserido = await CriarEInserirAcessoAsync(colaborador, dataHora);

        Assert.NotNull(inserido);
        Assert.True(inserido.Id > 0);
        var obtido = await _acessoRepo.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(colaborador.Id, obtido.ColaboradorId);
        Assert.Equal(dataHora, obtido.DataHora);
    }

    [Fact]
    public async Task AcessoColaborador_ObterPorId_Inexistente_RetornaNulo()
    {
        var obtido = await _acessoRepo.ObterPorId(int.MaxValue);
        Assert.Null(obtido);
    }

    [Fact]
    public async Task AcessoColaborador_NaoMistura_Com_AcessoDeAluno()
    {
        // tb_acesso é compartilhada: o registro de colaborador não pode aparecer como acesso de aluno
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        var inserido = await CriarEInserirAcessoAsync(colaborador, DateTime.Today.AddHours(8));
        var acessoAlunoRepo = new AcessoAlunoRepository(ConnectionString, DatabaseType);

        Assert.Null(await acessoAlunoRepo.ObterPorId(inserido.Id));
        Assert.False(await acessoAlunoRepo.Remover(inserido.Id));
        Assert.NotNull(await _acessoRepo.ObterPorId(inserido.Id));
    }

    [Fact]
    public async Task AcessoColaborador_Atualizar_E_Remover_Sucesso()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        var inserido = await CriarEInserirAcessoAsync(colaborador, DateTime.Today.AddHours(8));
        var novaDataHora = DateTime.Today.AddHours(11);

        await _acessoRepo.Atualizar(AcessoColaborador.Criar(inserido.Id, colaborador, novaDataHora).Value!);
        var obtido = await _acessoRepo.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(novaDataHora, obtido.DataHora);

        Assert.True(await _acessoRepo.Remover(inserido.Id));
        Assert.Null(await _acessoRepo.ObterPorId(inserido.Id));
    }

    [Fact]
    public async Task AcessoColaborador_ObterUltimoAcesso_RetornaMaisRecente()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        Assert.Null(await _acessoRepo.ObterUltimoAcesso(colaborador.Id));

        await CriarEInserirAcessoAsync(colaborador, DateTime.Today.AddDays(-1).AddHours(8));
        var ultimo = await CriarEInserirAcessoAsync(colaborador, DateTime.Today.AddHours(7));

        var obtido = await _acessoRepo.ObterUltimoAcesso(colaborador.Id);

        Assert.NotNull(obtido);
        Assert.Equal(ultimo.Id, obtido.Id);
    }

    [Fact]
    public async Task AcessoColaborador_ObterAcessosPorColaboradorPeriodo_FiltraPorDia()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        var ontem = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await CriarEInserirAcessoAsync(colaborador, DateTime.Today.AddDays(-1).AddHours(8));
        await CriarEInserirAcessoAsync(colaborador, DateTime.Today.AddDays(-1).AddHours(12));
        await CriarEInserirAcessoAsync(colaborador, DateTime.Today.AddHours(8));

        var deOntem = (await _acessoRepo.ObterAcessosPorColaboradorPeriodo(colaborador.Id, ontem, ontem)).ToList();
        var todos = (await _acessoRepo.ObterAcessosPorColaboradorPeriodo(colaborador.Id)).ToList();

        Assert.Equal(2, deOntem.Count);
        Assert.Equal(3, todos.Count);
    }

    [Fact]
    public async Task AcessoColaborador_ObterHorasTrabalhadasNoDia_SomaTodosOsRegistros()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        var dia = DateTime.Today.AddDays(-1);
        await CriarEInserirAcessoAsync(colaborador, dia.AddHours(8));                 // entrada 08:00
        await CriarEInserirAcessoAsync(colaborador, dia.AddHours(12));                // saída   12:00 -> 4h
        await CriarEInserirAcessoAsync(colaborador, dia.AddHours(13));                // entrada 13:00
        await CriarEInserirAcessoAsync(colaborador, dia.AddHours(15).AddMinutes(30)); // saída   15:30 -> 2h30

        var total = await _acessoRepo.ObterHorasTrabalhadasNoDia(colaborador.Id, DateOnly.FromDateTime(dia));
        var diaSemRegistro = await _acessoRepo.ObterHorasTrabalhadasNoDia(colaborador.Id, DateOnly.FromDateTime(dia.AddDays(-5)));

        Assert.Equal(new TimeSpan(6, 30, 0), total);
        Assert.Equal(TimeSpan.Zero, diaSemRegistro);
    }
}
