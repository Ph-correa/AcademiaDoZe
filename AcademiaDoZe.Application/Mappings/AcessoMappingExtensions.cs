using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Domain.Entities;
namespace AcademiaDoZe.Application.Mappings;

public static class AcessoMappingExtensions
{
    public static AcessoAlunoDto ToDto(this AcessoAluno acesso, AlunoDto alunoDto, AppAcessoTipo tipo)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(alunoDto);
        return new AcessoAlunoDto
        {
            Id = acesso.Id,
            AlunoAcesso = alunoDto,
            DataHora = acesso.DataHora,
            Tipo = tipo
        };
    }

    public static AcessoColaboradorDto ToDto(this AcessoColaborador acesso, ColaboradorDto colaboradorDto, AppAcessoTipo tipo)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(colaboradorDto);
        return new AcessoColaboradorDto
        {
            Id = acesso.Id,
            ColaboradorAcesso = colaboradorDto,
            DataHora = acesso.DataHora,
            Tipo = tipo
        };
    }

    // Dentro do mesmo dia os registros se alternam: posição 0 = entrada, 1 = saída, 2 = entrada...
    public static AppAcessoTipo TipoPelaPosicaoNoDia(int posicao) => posicao % 2 == 0 ? AppAcessoTipo.Entrada : AppAcessoTipo.Saida;

    // Ex.: "2h 05min", "12min 30s", "45s"
    public static string ParaTextoDuracao(this TimeSpan duracao)
    {
        if (duracao < TimeSpan.Zero) duracao = TimeSpan.Zero;
        if (duracao.TotalHours >= 1) return $"{(int)duracao.TotalHours}h {duracao.Minutes:00}min";
        if (duracao.TotalMinutes >= 1) return $"{duracao.Minutes}min {duracao.Seconds:00}s";
        return $"{duracao.Seconds}s";
    }
}
