using AcademiaDoZe.Application.DTOs;
namespace AcademiaDoZe.Application.Interfaces;
/// <summary>
/// Contrato de serviço para o registro de entrada e saída de Alunos na academia.
/// </summary>
public interface IAcessoAlunoService
{
    /// <summary>
    /// Registra a ENTRADA do aluno. Valida se possui matrícula ativa e devolve quanto tempo ainda tem de plano.
    /// </summary>
    Task<AcessoAlunoDto> RegistrarEntradaAsync(int alunoId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Registra a SAÍDA do aluno e devolve o tempo que permaneceu na academia.
    /// </summary>
    Task<AcessoAlunoDto> RegistrarSaidaAsync(int alunoId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Informa se o aluno está dentro da academia (possui entrada sem saída hoje).
    /// </summary>
    Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os alunos que estão dentro da academia agora (entrada sem saída hoje). Cada item é o registro da entrada em aberto.
    /// </summary>
    Task<IEnumerable<AcessoAlunoDto>> ObterPresentesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os registros (entradas e saídas) do aluno em um dia. Sem data = hoje.
    /// </summary>
    Task<IEnumerable<AcessoAlunoDto>> ObterAcessosDoDiaAsync(int alunoId, DateOnly? data = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os registros por período, opcionalmente filtrando por aluno.
    /// </summary>
    Task<IEnumerable<AcessoAlunoDto>> ObterAcessosPorPeriodoAsync(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Remove um registro de acesso pelo ID.
    /// </summary>
    Task<bool> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
