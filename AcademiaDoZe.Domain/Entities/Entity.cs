using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Exceptions;
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
        if (id < 0)
            throw new DomainException("ID_NEGATIVO");

        Id = id;
    }
}

