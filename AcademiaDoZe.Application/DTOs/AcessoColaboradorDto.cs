using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.DTOs;

public class AcessoColaboradorDto
{
    public int Id { get; set; }
    public required ColaboradorDto ColaboradorAcesso { get; set; }
    public required DateTime DataHora { get; set; }
    public required AppAcessoTipo Tipo { get; set; }

    // Limite diário conforme o vínculo: 8 horas (CLT) ou 6 horas (Estágio)
    public TimeSpan? LimiteDiario { get; set; }

    // Preenchido na SAÍDA: tempo deste período (da última entrada até a saída)
    public TimeSpan? Permanencia { get; set; }

    // Soma de todos os registros (entrada/saída) do dia
    public TimeSpan? TotalNoDia { get; set; }

    // True quando a soma do dia passou do limite do vínculo
    public bool UltrapassouLimite { get; set; }

    // Texto pronto para exibir ao usuário
    public string? Mensagem { get; set; }
}
