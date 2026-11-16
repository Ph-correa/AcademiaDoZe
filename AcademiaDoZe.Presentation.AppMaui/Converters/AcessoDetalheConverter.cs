using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Mappings;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Monta a linha de detalhe de cada registro da lista "Registros de hoje".
// Aluno:       saída -> "Permaneceu 1h 15min"
// Colaborador: saída -> "Período de 4h 00min • total do dia 8h 00min"
public sealed class AcessoDetalheConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case AcessoAlunoDto aluno:
                return aluno.Tipo == AppAcessoTipo.Saida && aluno.Permanencia.HasValue
                    ? $"Permaneceu {aluno.Permanencia.Value.ParaTextoDuracao()}"
                    : "Chegada à academia";

            case AcessoColaboradorDto colaborador:
                if (colaborador.Tipo == AppAcessoTipo.Saida && colaborador.Permanencia.HasValue)
                {
                    string total = (colaborador.TotalNoDia ?? colaborador.Permanencia.Value).ParaTextoDuracao();
                    return $"Período de {colaborador.Permanencia.Value.ParaTextoDuracao()} • total do dia {total}";
                }
                return "Início do período de trabalho";

            default:
                return string.Empty;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
