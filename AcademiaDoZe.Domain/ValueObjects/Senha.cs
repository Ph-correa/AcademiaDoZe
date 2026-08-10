using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Service;
using System;
using System.Collections.Generic;
using System.Text;
//Pedro Henrique correa
namespace AcademiaDoZe.Domain.ValueObjects;

public record Senha
{
    public string Valor { get; }

    private Senha(string valor)
    {
        Valor = valor;
    }

    public static Result<Senha> Criar(string valor)
    {
        var notifications = new List<Notification>();

       
        if (NormalizadoService.TextoVazioOuNulo(valor))
        {
            notifications.Add(new Notification("Senha", "SENHA_OBRIGATORIA"));
        }
        else
        {
      
            if (valor.Length < 6)
            {
                notifications.Add(new Notification("Senha", "SENHA_TAMANHO_MINIMO_INVALIDO"));
            }

       
            if (valor.Length > 100)
            {
                notifications.Add(new Notification("Senha", "SENHA_TAMANHO_EXCESSIVO"));
            }
        }

        if (notifications.Count != 0)
            return Result<Senha>.Failure(notifications);

        return Result<Senha>.Success(new Senha(valor));
    }
}

