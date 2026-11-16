using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

// Entrada/Saída de Alunos (Avaliação 03)
public partial class AcessoAlunoViewModel : BaseViewModel
{
    private readonly IAcessoAlunoService _acessoService;
    private readonly IAlunoService _alunoService;
    private readonly IMatriculaService _matriculaService;

    private string _cpf = string.Empty;
    public string Cpf
    {
        get => _cpf;
        set => SetProperty(ref _cpf, value);
    }

    private AlunoDto? _aluno;
    public AlunoDto? Aluno
    {
        get => _aluno;
        set
        {
            if (SetProperty(ref _aluno, value))
                NotificarPropriedadesVisuais();
        }
    }

    private MatriculaDto? _matriculaAtiva;
    public MatriculaDto? MatriculaAtiva
    {
        get => _matriculaAtiva;
        set
        {
            if (SetProperty(ref _matriculaAtiva, value))
                NotificarPropriedadesVisuais();
        }
    }

    private bool _estaNaAcademia;
    public bool EstaNaAcademia
    {
        get => _estaNaAcademia;
        set
        {
            if (SetProperty(ref _estaNaAcademia, value))
                NotificarPropriedadesVisuais();
        }
    }

    // Resultado do último registro (mensagem exibida no card de resultado)
    private string _mensagem = string.Empty;
    public string Mensagem
    {
        get => _mensagem;
        set
        {
            if (SetProperty(ref _mensagem, value))
                NotificarPropriedadesVisuais();
        }
    }

    private bool _mensagemSucesso;
    public bool MensagemSucesso
    {
        get => _mensagemSucesso;
        set
        {
            if (SetProperty(ref _mensagemSucesso, value))
                NotificarPropriedadesVisuais();
        }
    }

    // Dia exibido no histórico (por padrão, hoje)
    private DateTime _dataHistorico = DateTime.Today;
    public DateTime DataHistorico
    {
        get => _dataHistorico;
        set
        {
            if (SetProperty(ref _dataHistorico, value))
                NotificarPropriedadesVisuais();
        }
    }

    // Alunos que estão dentro da academia agora (entrada sem saída hoje)
    public ObservableCollection<AcessoAlunoDto> Presentes { get; } = [];

    // Histórico de registros (entradas e saídas de todos os alunos) do dia escolhido
    public ObservableCollection<AcessoAlunoDto> Historico { get; } = [];

    // Propriedades visuais
    public bool HasAluno => Aluno != null && Aluno.Id > 0;
    public bool HasNoAluno => !HasAluno;
    public bool TemMatriculaAtiva => HasAluno && MatriculaAtiva != null;
    public bool SemMatriculaAtiva => HasAluno && MatriculaAtiva == null;
    public bool Presente => HasAluno && EstaNaAcademia;
    public bool Ausente => HasAluno && !EstaNaAcademia;
    public bool HasMensagemSucesso => !string.IsNullOrWhiteSpace(Mensagem) && MensagemSucesso;
    public bool HasMensagemErro => !string.IsNullOrWhiteSpace(Mensagem) && !MensagemSucesso;
    public bool SemPresentes => Presentes.Count == 0;
    public bool SemHistorico => Historico.Count == 0;
    public string PresentesTitulo => $"Alunos na academia agora ({Presentes.Count})";
    public string HistoricoTitulo => DataHistorico.Date == DateTime.Today
        ? $"Histórico de registros de hoje ({Historico.Count})"
        : $"Histórico de registros de {DataHistorico:dd/MM/yyyy} ({Historico.Count})";

    // Ex.: "Plano Mensal • válido até 15/12/2026 • restam 30 dias"
    public string MatriculaTexto
    {
        get
        {
            if (MatriculaAtiva == null) return "Sem matrícula ativa";
            int dias = MatriculaAtiva.DataFim.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
            string restante = dias switch
            {
                <= 0 => "vence hoje",
                1 => "resta 1 dia",
                _ => $"restam {dias} dias"
            };
            return $"Plano {MatriculaAtiva.Plano.GetDisplayName()} • válido até {MatriculaAtiva.DataFim:dd/MM/yyyy} • {restante}";
        }
    }

    public AcessoAlunoViewModel(IAcessoAlunoService acessoService, IAlunoService alunoService, IMatriculaService matriculaService)
    {
        _acessoService = acessoService;
        _alunoService = alunoService;
        _matriculaService = matriculaService;
        Title = "Entrada/Saída de Alunos";
    }

    private void NotificarPropriedadesVisuais()
    {
        OnPropertyChanged(nameof(HasAluno));
        OnPropertyChanged(nameof(HasNoAluno));
        OnPropertyChanged(nameof(TemMatriculaAtiva));
        OnPropertyChanged(nameof(SemMatriculaAtiva));
        OnPropertyChanged(nameof(Presente));
        OnPropertyChanged(nameof(Ausente));
        OnPropertyChanged(nameof(HasMensagemSucesso));
        OnPropertyChanged(nameof(HasMensagemErro));
        OnPropertyChanged(nameof(SemPresentes));
        OnPropertyChanged(nameof(SemHistorico));
        OnPropertyChanged(nameof(PresentesTitulo));
        OnPropertyChanged(nameof(HistoricoTitulo));
        OnPropertyChanged(nameof(MatriculaTexto));
    }

    private void DefinirMensagem(string texto, bool sucesso)
    {
        MensagemSucesso = sucesso;
        Mensagem = texto;
        NotificarPropriedadesVisuais();
    }

    // Chamado ao abrir a tela: carrega quem está dentro, o histórico e a situação do aluno selecionado
    public async Task InitializeAsync()
    {
        await CarregarListasAsync();
        if (HasAluno)
            await CarregarSituacaoAsync();
    }

    // Recarrega a lista de alunos presentes e o histórico do dia escolhido
    private async Task CarregarListasAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var dia = DateOnly.FromDateTime(DataHistorico);

        var presentes = await _acessoService.ObterPresentesAsync(cts.Token);
        var historico = await _acessoService.ObterAcessosPorPeriodoAsync(null, dia, dia, cts.Token);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Presentes.Clear();
            foreach (var presente in presentes)
                Presentes.Add(presente);

            Historico.Clear();
            // mais recente primeiro
            foreach (var registro in historico.OrderByDescending(a => a.DataHora).ThenByDescending(a => a.Id))
                Historico.Add(registro);

            NotificarPropriedadesVisuais();
        });
    }

    // Recarrega matrícula ativa e presença do aluno selecionado
    private async Task CarregarSituacaoAsync()
    {
        if (!HasAluno) return;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        int alunoId = Aluno!.Id;

        MatriculaAtiva = await _matriculaService.ObterMatriculaAtivaPorAlunoAsync(alunoId, cts.Token);
        EstaNaAcademia = await _acessoService.EstaNaAcademiaAsync(alunoId, cts.Token);
        NotificarPropriedadesVisuais();
    }

    [RelayCommand]
    public async Task AtualizarAsync()
    {
        try
        {
            await CarregarListasAsync();
            if (HasAluno)
                await CarregarSituacaoAsync();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A atualização expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao atualizar os registros: {ex.Message}", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task SearchByCpfAsync()
    {
        if (IsBusy) return;

        var cpfLimpo = new string([.. (Cpf ?? string.Empty).Where(char.IsDigit)]);
        if (cpfLimpo.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "Informe o CPF do aluno com 11 dígitos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunoData = await _alunoService.ObterPorCpfAsync(cpfLimpo, cts.Token);

            DefinirMensagem(string.Empty, true);
            if (alunoData == null)
            {
                Aluno = null;
                MatriculaAtiva = null;
                EstaNaAcademia = false;
                NotificarPropriedadesVisuais();
                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno não encontrado para o CPF informado.", "OK");
                return;
            }

            Aluno = alunoData;
            await CarregarSituacaoAsync();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca por CPF expirou. Verifique a conexão.", "OK");
        }
        catch (ArgumentException)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "CPF inválido.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Toque em um aluno da lista "na academia agora": carrega o perfil dele no topo da tela
    public async Task SelecionarPresenteAsync(AcessoAlunoDto presente)
    {
        if (presente?.AlunoAcesso == null || IsBusy) return;
        try
        {
            IsBusy = true;
            DefinirMensagem(string.Empty, true);
            Aluno = presente.AlunoAcesso;
            Cpf = presente.AlunoAcesso.Cpf;
            await CarregarSituacaoAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Botão "Registrar Saída" de um aluno da lista "na academia agora"
    public async Task RegistrarSaidaDoPresenteAsync(AcessoAlunoDto presente)
    {
        if (presente?.AlunoAcesso == null || IsBusy) return;
        Aluno = presente.AlunoAcesso;
        Cpf = presente.AlunoAcesso.Cpf;
        await RegistrarAsync(AppAcessoTipo.Saida);
    }

    [RelayCommand]
    public Task RegistrarEntradaAsync() => RegistrarAsync(AppAcessoTipo.Entrada);

    [RelayCommand]
    public Task RegistrarSaidaAsync() => RegistrarAsync(AppAcessoTipo.Saida);

    private async Task RegistrarAsync(AppAcessoTipo tipo)
    {
        if (IsBusy) return;
        if (!HasAluno)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Busque o aluno pelo CPF antes de registrar.", "OK");
            return;
        }

        string operacao = tipo.GetDisplayName(); // "Entrada" ou "Saída"
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // As regras (matrícula ativa, tempo de plano, tempo de permanência) ficam no AcessoAlunoService
            var registro = tipo == AppAcessoTipo.Entrada
                ? await _acessoService.RegistrarEntradaAsync(Aluno!.Id, cts.Token)
                : await _acessoService.RegistrarSaidaAsync(Aluno!.Id, cts.Token);

            await CarregarSituacaoAsync();
            await CarregarListasAsync();
            DefinirMensagem(registro.Mensagem ?? $"{operacao} registrada.", true);
            await Shell.Current.DisplayAlertAsync($"{operacao} registrada", $"{Aluno.Nome}\n\n{registro.Mensagem}", "OK");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O registro expirou. Verifique a conexão.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            // regra de negócio não atendida (ex.: aluno sem matrícula ativa)
            await CarregarSituacaoAsync();
            DefinirMensagem(ex.Message, false);
            await Shell.Current.DisplayAlertAsync($"{operacao} não permitida", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao registrar {operacao.ToLower()}: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Limpar()
    {
        Cpf = string.Empty;
        Aluno = null;
        MatriculaAtiva = null;
        EstaNaAcademia = false;
        DefinirMensagem(string.Empty, true);
    }
}
