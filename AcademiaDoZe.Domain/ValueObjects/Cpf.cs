using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Service;
using System;
using System.Collections.Generic;
using System.Text;
//Pedro Henrique correa
namespace AcademiaDoZe.Domain.ValueObjects;

public record Cpf
{
    public string Valor { get; }

    private Cpf(string valor)
    {
        Valor = valor;
    }

    public static Result<Cpf> Criar(string valor)
    {
        var notifications = new List<Notification>();

        if (NormalizadoService.TextoVazioOuNulo(valor))
            notifications.Add(new Notification("Cpf", "CPF_OBRIGATORIO"));
        else
            valor = NormalizadoService.LimparEspacos(valor);

        if (notifications.Count != 0)
            return Result<Cpf>.Failure(notifications);

        return Result<Cpf>.Success(new Cpf(valor));
    }
}