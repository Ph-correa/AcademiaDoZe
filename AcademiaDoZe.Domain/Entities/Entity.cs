using AcademiaDoZe.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
//Pedro Henrique correa
namespace AcademiaDoZe.Domain.Entities;

public abstract class Entity
{
    public int Id { get; protected set; }

    protected Entity(int id = 0)
    {
        Id = id;
    }

    public static Result<int> ValidarId(int id)
    {
        var notifications = new List<Notification>();

        if (id < 0)
            notifications.Add(new Notification("Id", "ID_INVALIDO"));

        if (notifications.Count != 0)
            return Result<int>.Failure(notifications);

        return Result<int>.Success(id);
    }
}


