using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.Exceptions;

public sealed class DomainException(string message) : Exception(message)
{
}

