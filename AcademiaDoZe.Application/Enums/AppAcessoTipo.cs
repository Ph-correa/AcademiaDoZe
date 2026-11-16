using System.ComponentModel.DataAnnotations;
namespace AcademiaDoZe.Application.Enums;

// Cada registro de acesso é uma Entrada ou uma Saída.
// No banco só existe a data/hora: dentro do mesmo dia, o 1º registro é entrada, o 2º é saída, e assim por diante.
public enum AppAcessoTipo
{
    [Display(Name = "Entrada")]
    Entrada = 0,
    [Display(Name = "Saída")]
    Saida = 1
}
