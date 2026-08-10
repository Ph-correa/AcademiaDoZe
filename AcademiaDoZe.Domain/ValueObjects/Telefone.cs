using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Service;
using System;
using System.Collections.Generic;
using System.Text;
//Pedro Henrique correa
namespace AcademiaDoZe.Domain.ValueObjects;

public record Telefone
{
    public string Valor { get; }

    private Telefone(string valor)
    {
        Valor = valor;
    }

    public static Result<Telefone> Criar(string valor)
    {
        var notifications = new List<Notification>();

        if (NormalizadoService.TextoVazioOuNulo(valor))
        {
            notifications.Add(new Notification("Telefone", "TELEFONE_OBRIGATORIO"));
            return Result<Telefone>.Failure(notifications);
        }

        
        var textoLimpo = NormalizadoService.LimparEDigitos(valor);

       
        if (textoLimpo.Length != 10 && textoLimpo.Length != 11)
        {
            notifications.Add(new Notification("Telefone", "TELEFONE_DIGITOS_INVALIDOS"));
        }

        if (notifications.Count != 0)
            return Result<Telefone>.Failure(notifications);

        return Result<Telefone>.Success(new Telefone(textoLimpo));
    }

    public override string ToString() => Valor;
}