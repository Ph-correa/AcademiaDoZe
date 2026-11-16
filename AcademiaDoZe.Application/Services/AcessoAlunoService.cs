using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
namespace AcademiaDoZe.Application.Services;

/*
Entrada/Saída de Alunos.
Regras (Avaliação 03):
 - Validar se o aluno possui matrícula ativa.
 - Na entrada, mostrar quanto tempo ainda tem de plano.
 - Na saída, mostrar o tempo que permaneceu na academia.
*/
public class AcessoAlunoService : IAcessoAlunoService
{
    private readonly Func<IAcessoAlunoRepository> _acessoRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;
    // relógio injetável (facilita testes); por padrão usa o horário do dispositivo
    private readonly Func<DateTime> _relogio;

    public AcessoAlunoService(Func<IAcessoAlunoRepository> acessoRepoFactory, Func<IAlunoRepository> alunoRepoFactory, Func<IMatriculaRepository> matriculaRepoFactory, Func<DateTime>? relogio = null)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _alunoRepoFactory = alunoRepoFactory ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
        _matriculaRepoFactory = matriculaRepoFactory ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
        _relogio = relogio ?? (() => DateTime.Now);
    }

    // data/hora atual sem frações de segundo
    private DateTime Agora()
    {
        var agora = _relogio();
        return new DateTime(agora.Year, agora.Month, agora.Day, agora.Hour, agora.Minute, agora.Second);
    }

    private async Task<Aluno> ObterAlunoAsync(int alunoId, CancellationToken cancellationToken)
    {
        if (alunoId <= 0)
            throw new InvalidOperationException("Aluno não informado. Busque o aluno pelo CPF.");
        return await _alunoRepoFactory().ObterPorId(alunoId, cancellationToken)
            ?? throw new InvalidOperationException($"Aluno com ID {alunoId} não encontrado.");
    }

    private async Task<List<AcessoAluno>> ObterRegistrosDoDiaAsync(int alunoId, DateOnly dia, CancellationToken cancellationToken)
    {
        var acessos = await _acessoRepoFactory().ObterAcessosPorAlunoPeriodo(alunoId, dia, dia, cancellationToken);
        return [.. acessos.OrderBy(a => a.DataHora).ThenBy(a => a.Id)];
    }

    private static AcessoAluno CriarAcesso(Aluno aluno, DateTime dataHora)
    {
        var result = AcessoAluno.Criar(0, aluno, dataHora);
        if (result.IsFailure)
        {
            if (result.Notifications.Any(n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO"))
                throw new InvalidOperationException($"Registro não permitido às {dataHora:HH:mm}. A academia funciona das 06:00 às 22:00.");
            throw new InvalidOperationException($"Erro de validação ao registrar acesso: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return result.Value!;
    }

    public async Task<AcessoAlunoDto> RegistrarEntradaAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        var aluno = await ObterAlunoAsync(alunoId, cancellationToken);
        var agora = Agora();
        var hoje = DateOnly.FromDateTime(agora);

        // já está dentro? (quantidade ímpar de registros hoje = entrada sem saída)
        var doDia = await ObterRegistrosDoDiaAsync(alunoId, hoje, cancellationToken);
        if (doDia.Count % 2 == 1)
            throw new InvalidOperationException($"{aluno.Nome} já está na academia desde {doDia[^1].DataHora:HH:mm}. Registre a saída antes de uma nova entrada.");

        // Regra: validar se possui matrícula ativa
        var matricula = await _matriculaRepoFactory().ObterMatriculaAtivaPorAluno(alunoId, cancellationToken)
            ?? throw new InvalidOperationException($"{aluno.Nome} não possui matrícula ativa. Entrada não permitida.");
        if (matricula.DataInicio > hoje)
            throw new InvalidOperationException($"A matrícula de {aluno.Nome} só inicia em {matricula.DataInicio:dd/MM/yyyy}. Entrada não permitida.");

        var acesso = await _acessoRepoFactory().Adicionar(CriarAcesso(aluno, agora), cancellationToken);

        // Regra: na entrada, mostrar quanto tempo ainda tem de plano
        int diasRestantes = matricula.DataFim.DayNumber - hoje.DayNumber;
        var plano = matricula.Plano.ToApplication();
        string tempoPlano = diasRestantes switch
        {
            0 => "o plano vence hoje",
            1 => "resta 1 dia de plano",
            _ => $"restam {diasRestantes} dias de plano"
        };

        var dto = acesso.ToDto(aluno.ToDto(), AppAcessoTipo.Entrada);
        dto.Plano = plano;
        dto.DataFimPlano = matricula.DataFim;
        dto.DiasRestantesPlano = diasRestantes;
        dto.Mensagem = $"Entrada registrada às {agora:HH:mm}.\nPlano {plano.GetDisplayName()}: {tempoPlano} (válido até {matricula.DataFim:dd/MM/yyyy}).";
        return dto;
    }

    public async Task<AcessoAlunoDto> RegistrarSaidaAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        var aluno = await ObterAlunoAsync(alunoId, cancellationToken);
        var agora = Agora();
        var hoje = DateOnly.FromDateTime(agora);

        var doDia = await ObterRegistrosDoDiaAsync(alunoId, hoje, cancellationToken);
        if (doDia.Count % 2 == 0)
            throw new InvalidOperationException($"{aluno.Nome} não possui entrada em aberto hoje. Registre a entrada primeiro.");

        var entrada = doDia[^1];
        var acesso = await _acessoRepoFactory().Adicionar(CriarAcesso(aluno, agora), cancellationToken);

        // Regra: na saída, mostrar o tempo que permaneceu na academia
        var permanencia = agora - entrada.DataHora;
        if (permanencia < TimeSpan.Zero) permanencia = TimeSpan.Zero;

        var dto = acesso.ToDto(aluno.ToDto(), AppAcessoTipo.Saida);
        dto.Permanencia = permanencia;
        dto.Mensagem = $"Saída registrada às {agora:HH:mm}.\nTempo de permanência na academia: {permanencia.ParaTextoDuracao()} (entrada às {entrada.DataHora:HH:mm}).";
        return dto;
    }

    public async Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        if (alunoId <= 0) return false;
        var doDia = await ObterRegistrosDoDiaAsync(alunoId, DateOnly.FromDateTime(Agora()), cancellationToken);
        return doDia.Count % 2 == 1;
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterPresentesAsync(CancellationToken cancellationToken = default)
    {
        // quem tem quantidade ímpar de registros hoje entrou e ainda não saiu
        var hoje = DateOnly.FromDateTime(Agora());
        var doDia = await ObterAcessosPorPeriodoAsync(null, hoje, hoje, cancellationToken);
        return [.. doDia.GroupBy(a => a.AlunoAcesso.Id)
            .Where(g => g.Count() % 2 == 1)
            .Select(g => g.Last()) // a entrada em aberto
            .OrderBy(a => a.DataHora)];
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterAcessosDoDiaAsync(int alunoId, DateOnly? data = null, CancellationToken cancellationToken = default)
    {
        var dia = data ?? DateOnly.FromDateTime(Agora());
        return await ObterAcessosPorPeriodoAsync(alunoId, dia, dia, cancellationToken);
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterAcessosPorPeriodoAsync(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        var acessos = (await _acessoRepoFactory().ObterAcessosPorAlunoPeriodo(alunoId, inicio, fim, cancellationToken)).ToList();
        if (acessos.Count == 0) return [];

        // carrega cada aluno uma única vez
        var alunos = new Dictionary<int, AlunoDto>();
        foreach (var id in acessos.Select(a => a.AlunoId).Distinct())
        {
            var aluno = await _alunoRepoFactory().ObterPorId(id, cancellationToken);
            if (aluno != null) alunos[id] = aluno.ToDto();
        }

        // entrada/saída é definida pela posição do registro no dia de cada aluno
        var resultado = new List<AcessoAlunoDto>();
        foreach (var grupo in acessos.Where(a => alunos.ContainsKey(a.AlunoId)).GroupBy(a => (a.AlunoId, a.DataHora.Date)))
        {
            var doDia = grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id).ToList();
            for (int i = 0; i < doDia.Count; i++)
            {
                var dto = doDia[i].ToDto(alunos[grupo.Key.AlunoId], AcessoMappingExtensions.TipoPelaPosicaoNoDia(i));
                if (dto.Tipo == AppAcessoTipo.Saida)
                    dto.Permanencia = doDia[i].DataHora - doDia[i - 1].DataHora;
                resultado.Add(dto);
            }
        }
        return [.. resultado.OrderBy(a => a.DataHora).ThenBy(a => a.Id)];
    }

    public async Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var acesso = await _acessoRepoFactory().ObterPorId(id, cancellationToken);
        if (acesso == null) return false;
        return await _acessoRepoFactory().Remover(id, cancellationToken);
    }
}
