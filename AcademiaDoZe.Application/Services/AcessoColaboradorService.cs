using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;
namespace AcademiaDoZe.Application.Services;

/*
Entrada/Saída de Colaboradores.
Regras (Avaliação 03):
 - Validar se já não ultrapassa o limite de: 8 horas se for CLT, 6 horas se for estágio.
 - Na saída, mostrar o tempo que permaneceu na academia, somando todos os registros do dia.
*/
public class AcessoColaboradorService : IAcessoColaboradorService
{
    private static readonly TimeSpan LimiteClt = TimeSpan.FromHours(8);
    private static readonly TimeSpan LimiteEstagio = TimeSpan.FromHours(6);

    private readonly Func<IAcessoColaboradorRepository> _acessoRepoFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepoFactory;
    // relógio injetável (facilita testes); por padrão usa o horário do dispositivo
    private readonly Func<DateTime> _relogio;

    public AcessoColaboradorService(Func<IAcessoColaboradorRepository> acessoRepoFactory, Func<IColaboradorRepository> colaboradorRepoFactory, Func<DateTime>? relogio = null)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _colaboradorRepoFactory = colaboradorRepoFactory ?? throw new ArgumentNullException(nameof(colaboradorRepoFactory));
        _relogio = relogio ?? (() => DateTime.Now);
    }

    // data/hora atual sem frações de segundo
    private DateTime Agora()
    {
        var agora = _relogio();
        return new DateTime(agora.Year, agora.Month, agora.Day, agora.Hour, agora.Minute, agora.Second);
    }

    public static TimeSpan ObterLimiteDiario(ColaboradorVinculo vinculo) => vinculo == ColaboradorVinculo.Estagio ? LimiteEstagio : LimiteClt;

    private async Task<Colaborador> ObterColaboradorAsync(int colaboradorId, CancellationToken cancellationToken)
    {
        if (colaboradorId <= 0)
            throw new InvalidOperationException("Colaborador não informado. Busque o colaborador pelo CPF.");
        return await _colaboradorRepoFactory().ObterPorId(colaboradorId, cancellationToken)
            ?? throw new InvalidOperationException($"Colaborador com ID {colaboradorId} não encontrado.");
    }

    private async Task<List<AcessoColaborador>> ObterRegistrosDoDiaAsync(int colaboradorId, DateOnly dia, CancellationToken cancellationToken)
    {
        var acessos = await _acessoRepoFactory().ObterAcessosPorColaboradorPeriodo(colaboradorId, dia, dia, cancellationToken);
        return [.. acessos.OrderBy(a => a.DataHora).ThenBy(a => a.Id)];
    }

    // soma todos os pares (entrada, saída) já fechados do dia
    private static TimeSpan SomarPeriodosFechados(List<AcessoColaborador> doDia)
    {
        var total = TimeSpan.Zero;
        for (int i = 0; i + 1 < doDia.Count; i += 2)
        {
            total += doDia[i + 1].DataHora - doDia[i].DataHora;
        }
        return total;
    }

    private static AcessoColaborador CriarAcesso(Colaborador colaborador, DateTime dataHora)
    {
        var result = AcessoColaborador.Criar(0, colaborador, dataHora);
        if (result.IsFailure)
        {
            if (result.Notifications.Any(n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO"))
                throw new InvalidOperationException($"Registro não permitido às {dataHora:HH:mm}. A academia funciona das 06:00 às 22:00.");
            throw new InvalidOperationException($"Erro de validação ao registrar acesso: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return result.Value!;
    }

    public async Task<AcessoColaboradorDto> RegistrarEntradaAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var colaborador = await ObterColaboradorAsync(colaboradorId, cancellationToken);
        var agora = Agora();
        var hoje = DateOnly.FromDateTime(agora);
        var limite = ObterLimiteDiario(colaborador.Vinculo);
        string vinculo = colaborador.Vinculo.ToApplication().GetDisplayName();

        // já está dentro? (quantidade ímpar de registros hoje = entrada sem saída)
        var doDia = await ObterRegistrosDoDiaAsync(colaboradorId, hoje, cancellationToken);
        if (doDia.Count % 2 == 1)
            throw new InvalidOperationException($"{colaborador.Nome} já está na academia desde {doDia[^1].DataHora:HH:mm}. Registre a saída antes de uma nova entrada.");

        // Regra: validar se já não ultrapassa o limite diário (8h CLT, 6h Estágio)
        var trabalhado = SomarPeriodosFechados(doDia);
        if (trabalhado >= limite)
            throw new InvalidOperationException($"{colaborador.Nome} já atingiu o limite diário de {limite.TotalHours:0} horas ({vinculo}). Trabalhado hoje: {trabalhado.ParaTextoDuracao()}. Entrada não permitida.");

        var acesso = await _acessoRepoFactory().Adicionar(CriarAcesso(colaborador, agora), cancellationToken);

        var dto = acesso.ToDto(colaborador.ToDto(), AppAcessoTipo.Entrada);
        dto.LimiteDiario = limite;
        dto.TotalNoDia = trabalhado;
        dto.Mensagem = $"Entrada registrada às {agora:HH:mm}.\nVínculo {vinculo}: limite de {limite.TotalHours:0} horas por dia. Trabalhado hoje: {trabalhado.ParaTextoDuracao()} (restam {(limite - trabalhado).ParaTextoDuracao()}).";
        return dto;
    }

    public async Task<AcessoColaboradorDto> RegistrarSaidaAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var colaborador = await ObterColaboradorAsync(colaboradorId, cancellationToken);
        var agora = Agora();
        var hoje = DateOnly.FromDateTime(agora);
        var limite = ObterLimiteDiario(colaborador.Vinculo);
        string vinculo = colaborador.Vinculo.ToApplication().GetDisplayName();

        var doDia = await ObterRegistrosDoDiaAsync(colaboradorId, hoje, cancellationToken);
        if (doDia.Count % 2 == 0)
            throw new InvalidOperationException($"{colaborador.Nome} não possui entrada em aberto hoje. Registre a entrada primeiro.");

        var entrada = doDia[^1];
        var acesso = await _acessoRepoFactory().Adicionar(CriarAcesso(colaborador, agora), cancellationToken);

        // Regra: na saída, mostrar o tempo que permaneceu, somando todos os registros do dia
        var permanencia = agora - entrada.DataHora;
        if (permanencia < TimeSpan.Zero) permanencia = TimeSpan.Zero;
        var totalNoDia = SomarPeriodosFechados(doDia) + permanencia;
        bool ultrapassou = totalNoDia > limite;

        var dto = acesso.ToDto(colaborador.ToDto(), AppAcessoTipo.Saida);
        dto.LimiteDiario = limite;
        dto.Permanencia = permanencia;
        dto.TotalNoDia = totalNoDia;
        dto.UltrapassouLimite = ultrapassou;
        dto.Mensagem = $"Saída registrada às {agora:HH:mm}.\nNeste período: {permanencia.ParaTextoDuracao()} (entrada às {entrada.DataHora:HH:mm}).\nTotal do dia (soma de todos os registros): {totalNoDia.ParaTextoDuracao()} de {limite.TotalHours:0} horas ({vinculo}).";
        if (ultrapassou)
            dto.Mensagem += $"\nAtenção: o limite diário foi ultrapassado em {(totalNoDia - limite).ParaTextoDuracao()}.";
        return dto;
    }

    public async Task<bool> EstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        if (colaboradorId <= 0) return false;
        var doDia = await ObterRegistrosDoDiaAsync(colaboradorId, DateOnly.FromDateTime(Agora()), cancellationToken);
        return doDia.Count % 2 == 1;
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterPresentesAsync(CancellationToken cancellationToken = default)
    {
        // quem tem quantidade ímpar de registros hoje entrou e ainda não saiu
        var hoje = DateOnly.FromDateTime(Agora());
        var doDia = await ObterAcessosPorPeriodoAsync(null, hoje, hoje, cancellationToken);
        return [.. doDia.GroupBy(a => a.ColaboradorAcesso.Id)
            .Where(g => g.Count() % 2 == 1)
            .Select(g => g.Last()) // a entrada em aberto
            .OrderBy(a => a.DataHora)];
    }

    public async Task<TimeSpan> ObterHorasTrabalhadasNoDiaAsync(int colaboradorId, DateOnly? data = null, CancellationToken cancellationToken = default)
    {
        var dia = data ?? DateOnly.FromDateTime(Agora());
        return await _acessoRepoFactory().ObterHorasTrabalhadasNoDia(colaboradorId, dia, cancellationToken);
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosDoDiaAsync(int colaboradorId, DateOnly? data = null, CancellationToken cancellationToken = default)
    {
        var dia = data ?? DateOnly.FromDateTime(Agora());
        return await ObterAcessosPorPeriodoAsync(colaboradorId, dia, dia, cancellationToken);
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosPorPeriodoAsync(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        var acessos = (await _acessoRepoFactory().ObterAcessosPorColaboradorPeriodo(colaboradorId, inicio, fim, cancellationToken)).ToList();
        if (acessos.Count == 0) return [];

        // carrega cada colaborador uma única vez
        var colaboradores = new Dictionary<int, Colaborador>();
        foreach (var id in acessos.Select(a => a.ColaboradorId).Distinct())
        {
            var colaborador = await _colaboradorRepoFactory().ObterPorId(id, cancellationToken);
            if (colaborador != null) colaboradores[id] = colaborador;
        }

        // entrada/saída é definida pela posição do registro no dia de cada colaborador
        var resultado = new List<AcessoColaboradorDto>();
        foreach (var grupo in acessos.Where(a => colaboradores.ContainsKey(a.ColaboradorId)).GroupBy(a => (a.ColaboradorId, a.DataHora.Date)))
        {
            var colaborador = colaboradores[grupo.Key.ColaboradorId];
            var colaboradorDto = colaborador.ToDto();
            var limite = ObterLimiteDiario(colaborador.Vinculo);
            var doDia = grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id).ToList();
            var acumulado = TimeSpan.Zero;
            for (int i = 0; i < doDia.Count; i++)
            {
                var dto = doDia[i].ToDto(colaboradorDto, AcessoMappingExtensions.TipoPelaPosicaoNoDia(i));
                dto.LimiteDiario = limite;
                if (dto.Tipo == AppAcessoTipo.Saida)
                {
                    dto.Permanencia = doDia[i].DataHora - doDia[i - 1].DataHora;
                    acumulado += dto.Permanencia.Value;
                }
                dto.TotalNoDia = acumulado;
                dto.UltrapassouLimite = acumulado > limite;
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
