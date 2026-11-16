using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.DTOs;

public class AcessoAlunoDto
{
    public int Id { get; set; }
    public required AlunoDto AlunoAcesso { get; set; }
    public required DateTime DataHora { get; set; }
    public required AppAcessoTipo Tipo { get; set; }

    // Preenchido na ENTRADA: quanto tempo ainda tem de plano
    public AppMatriculaPlano? Plano { get; set; }
    public DateOnly? DataFimPlano { get; set; }
    public int? DiasRestantesPlano { get; set; }

    // Preenchido na SAÍDA: tempo que permaneceu na academia
    public TimeSpan? Permanencia { get; set; }

    // Texto pronto para exibir ao usuário
    public string? Mensagem { get; set; }
}
