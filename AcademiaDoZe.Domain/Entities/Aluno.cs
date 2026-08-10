using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;
//Pedro Henrique correa
namespace AcademiaDoZe.Domain.Entities;
public class Aluno : Pessoa
{
    
    private Aluno(
        int id,
        string nome,
        Cpf cpf,
        DateOnly dataNascimento,
        Telefone telefone,
        Email email,
        Endereco endereco,
        Senha senha,
        Arquivo foto,
        DateOnly dataMatricula,
        Arquivo laudoMedico)
        : base(id, nome, cpf, dataNascimento, telefone, email, endereco, senha, foto)
    {
            
    }
}

