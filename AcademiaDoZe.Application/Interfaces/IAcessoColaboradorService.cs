using AcademiaDoZe.Application.DTOs;
namespace AcademiaDoZe.Application.Interfaces;
/// <summary>
/// Contrato de serviço para o registro de entrada e saída de Colaboradores na academia.
/// </summary>
public interface IAcessoColaboradorService
{
    /// <summary>
    /// Registra a ENTRADA do colaborador. Valida se já não ultrapassou o limite diário (8h CLT, 6h Estágio).
    /// </summary>
    Task<AcessoColaboradorDto> RegistrarEntradaAsync(int colaboradorId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Registra a SAÍDA do colaborador e devolve o tempo que permaneceu, somando todos os registros do dia.
    /// </summary>
    Task<AcessoColaboradorDto> RegistrarSaidaAsync(int colaboradorId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Informa se o colaborador está dentro da academia (possui entrada sem saída hoje).
    /// </summary>
    Task<bool> EstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os colaboradores que estão dentro da academia agora (entrada sem saída hoje). Cada item é o registro da entrada em aberto.
    /// </summary>
    Task<IEnumerable<AcessoColaboradorDto>> ObterPresentesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Total de horas trabalhadas pelo colaborador em um dia (soma de todos os registros). Sem data = hoje.
    /// </summary>
    Task<TimeSpan> ObterHorasTrabalhadasNoDiaAsync(int colaboradorId, DateOnly? data = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os registros (entradas e saídas) do colaborador em um dia. Sem data = hoje.
    /// </summary>
    Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosDoDiaAsync(int colaboradorId, DateOnly? data = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os registros por período, opcionalmente filtrando por colaborador.
    /// </summary>
    Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosPorPeriodoAsync(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Remove um registro de acesso pelo ID.
    /// </summary>
    Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
