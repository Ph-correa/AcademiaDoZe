using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Domain.Enums;
namespace AcademiaDoZe.Application.Mappings;

public static class MatriculaEnumMappingExtensions
{
    // ATENÇÃO: os números dos dois enums são DIFERENTES
    //   Domain:      Mensal = 1, Trimestral = 2, Semestral = 3, Anual = 4
    //   Application: Mensal = 0, Trimestral = 1, Semestral = 2, Anual = 3
    // Por isso a conversão é feita pelo NOME do plano, e não por cast direto do número.
    public static MatriculaPlano ToDomain(this AppMatriculaPlano appPlano)
    {
        return appPlano switch
        {
            AppMatriculaPlano.Mensal => MatriculaPlano.Mensal,
            AppMatriculaPlano.Trimestral => MatriculaPlano.Trimestral,
            AppMatriculaPlano.Semestral => MatriculaPlano.Semestral,
            AppMatriculaPlano.Anual => MatriculaPlano.Anual,
            _ => throw new InvalidOperationException($"Plano de matrícula inválido: {appPlano}")
        };
    }
    public static AppMatriculaPlano ToApplication(this MatriculaPlano domainPlano)
    {
        return domainPlano switch
        {
            MatriculaPlano.Mensal => AppMatriculaPlano.Mensal,
            MatriculaPlano.Trimestral => AppMatriculaPlano.Trimestral,
            MatriculaPlano.Semestral => AppMatriculaPlano.Semestral,
            MatriculaPlano.Anual => AppMatriculaPlano.Anual,
            // Recorrente e Diaria existem no Domain mas não na tela: trata como Mensal para não quebrar a listagem
            _ => AppMatriculaPlano.Mensal
        };
    }
    public static MatriculaRestricoes ToDomain(this AppMatriculaRestricoes appRestricoes)
    {
        return (MatriculaRestricoes)appRestricoes;
    }
    public static AppMatriculaRestricoes ToApplication(this MatriculaRestricoes domainRestricoes)
    {
        return (AppMatriculaRestricoes)domainRestricoes;
    }
}
