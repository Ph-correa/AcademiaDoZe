using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Identidade visual da GYM do Pedro: mostra um selo "Ativa" / "Vencida" a partir da DataFim da matrícula.
// Sem parâmetro -> retorna o texto. Com ConverterParameter="cor" -> retorna a cor do selo.
public sealed class MatriculaStatusConverter : IValueConverter
{
    private static readonly Color CorAtiva = Color.FromArgb("#16A34A");
    private static readonly Color CorVencida = Color.FromArgb("#DC2626");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool ativa = value is DateOnly dataFim && dataFim >= DateOnly.FromDateTime(DateTime.Today);

        if (parameter is string p && p.Equals("cor", StringComparison.OrdinalIgnoreCase))
            return ativa ? CorAtiva : CorVencida;

        return ativa ? "ATIVA" : "VENCIDA";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
