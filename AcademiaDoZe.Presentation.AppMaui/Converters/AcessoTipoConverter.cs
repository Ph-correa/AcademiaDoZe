using AcademiaDoZe.Application.Enums;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Converte o tipo do registro (Entrada/Saída) para exibição na lista.
// Sem parâmetro -> texto. ConverterParameter="cor" -> cor do selo. ConverterParameter="icone" -> glifo MaterialIcons.
public sealed class AcessoTipoConverter : IValueConverter
{
    private static readonly Color CorEntrada = Color.FromArgb("#16A34A"); // verde
    private static readonly Color CorSaida = Color.FromArgb("#D9822B");   // laranja escuro da marca

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool entrada = value is AppAcessoTipo tipo && tipo == AppAcessoTipo.Entrada;
        string modo = (parameter as string ?? string.Empty).ToLowerInvariant();

        return modo switch
        {
            "cor" => entrada ? CorEntrada : CorSaida,
            "icone" => entrada ? "" : "", // login / logout
            _ => entrada ? "ENTRADA" : "SAÍDA"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
