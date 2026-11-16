using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

// Entrada/Saída de Colaboradores (Avaliação 03)
public partial class AcessoColaboradorViewModel : BaseViewModel
{
    private readonly IAcessoColaboradorService _acessoService;
    private readonly IColaboradorService _colaboradorService;

    private string _cpf = string.Empty;
    public string Cpf
    {
        get => _cpf;
        set => SetProperty(ref _cpf, value);
    }

    private ColaboradorDto? _colaborador;
    public ColaboradorDto? Colaborador
    {
        get => _colaborador;
        set
        {
            if (SetProperty(ref _colaborador, value))
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

    // Soma de todos os registros de hoje
    private TimeSpan _trabalhadoHoje = TimeSpan.Zero;
    public TimeSpan TrabalhadoHoje
    {
        get => _trabalhadoHoje;
        set
        {
            if (SetProperty(ref _trabalhadoHoje, value))
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

    // Colaboradores que estão dentro da academia agora (entrada sem saída hoje)
    public ObservableCollection<AcessoColaboradorDto> Presentes { get; } = [];

    // Histórico de registros (entradas e saídas de todos os colaboradores) do dia escolhido
    public ObservableCollection<AcessoColaboradorDto> Historico { get; } = [];

    // Propriedades visuais
    public bool HasColaborador => Colaborador != null && Colaborador.Id > 0;
    public bool HasNoColaborador => !HasColaborador;
    public bool Presente => HasColaborador && EstaNaAcademia;
    public bool Ausente => HasColaborador && !EstaNaAcademia;
    public bool HasMensagemSucesso => !string.IsNullOrWhiteSpace(Mensagem) && MensagemSucesso;
    public bool HasMensagemErro => !string.IsNullOrWhiteSpace(Mensagem) && !MensagemSucesso;
    public bool SemPresentes => Presentes.Count == 0;
    public bool SemHistorico => Historico.Count == 0;
    public string PresentesTitulo => $"Colaboradores na academia agora ({Presentes.Count})";
    public string HistoricoTitulo => DataHistorico.Date == DateTime.Today
        ? $"Histórico de registros de hoje ({Historico.Count})"
        : $"Histórico de registros de {DataHistorico:dd/MM/yyyy} ({Historico.Count})";

    // Limite diário: 8 horas (CLT) ou 6 horas (Estágio)
    public int LimiteHoras => Colaborador?.Vinculo == AppColaboradorVinculo.Estagio ? 6 : 8;
    public bool LimiteAtingido => HasColaborador && TrabalhadoHoje >= TimeSpan.FromHours(LimiteHoras);

    // Ex.: "Instrutor • CLT • limite de 8 horas por dia"
    public string VinculoTexto => Colaborador == null
        ? string.Empty
        : $"{Colaborador.Tipo.GetDisplayName()} • {Colaborador.Vinculo.GetDisplayName()} • limite de {LimiteHoras} horas por dia";

    // Ex.: "Trabalhado hoje: 4h 00min de 8 horas"
    public string TrabalhadoTexto => $"Trabalhado hoje: {TrabalhadoHoje.ParaTextoDuracao()} de {LimiteHoras} horas";

    public AcessoColaboradorViewModel(IAcessoColaboradorService acessoService, IColaboradorService colaboradorService)
    {
        _acessoService = acessoService;
        _colaboradorService = colaboradorService;
        Title = "Entrada/Saída de Colaboradores";
    }

    private void NotificarPropriedadesVisuais()
    {
        OnPropertyChanged(nameof(HasColaborador));
        OnPropertyChanged(nameof(HasNoColaborador));
        OnPropertyChanged(nameof(Presente));
        OnPropertyChanged(nameof(Ausente));
        OnPropertyChanged(nameof(HasMensagemSucesso));
        OnPropertyChanged(nameof(HasMensagemErro));
        OnPropertyChanged(nameof(SemPresentes));
        OnPropertyChanged(nameof(SemHistorico));
        OnPropertyChanged(nameof(PresentesTitulo));
        OnPropertyChanged(nameof(HistoricoTitulo));
        OnPropertyChanged(nameof(LimiteHoras));
        OnPropertyChanged(nameof(LimiteAtingido));
        OnPropertyChanged(nameof(VinculoTexto));
        OnPropertyChanged(nameof(TrabalhadoTexto));
    }

    private void DefinirMensagem(string texto, bool sucesso)
    {
        MensagemSucesso = sucesso;
        Mensagem = texto;
        NotificarPropriedadesVisuais();
    }

    // Chamado ao abrir a tela: carrega quem está dentro, o histórico e a situação do colaborador selecionado
    public async Task InitializeAsync()
    {
        await CarregarListasAsync();
        if (HasColaborador)
            await CarregarSituacaoAsync();
    }

    // Recarrega a lista de colaboradores presentes e o histórico do dia escolhido
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

    // Recarrega presença e horas trabalhadas do colaborador selecionado
    private async Task CarregarSituacaoAsync()
    {
        if (!HasColaborador) return;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        int colaboradorId = Colaborador!.Id;

        EstaNaAcademia = await _acessoService.EstaNaAcademiaAsync(colaboradorId, cts.Token);
        TrabalhadoHoje = await _acessoService.ObterHorasTrabalhadasNoDiaAsync(colaboradorId, null, cts.Token);
        NotificarPropriedadesVisuais();
    }

    [RelayCommand]
    public async Task AtualizarAsync()
    {
        try
        {
            await CarregarListasAsync();
            if (HasColaborador)
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
            await Shell.Current.DisplayAlertAsync("Validação", "Informe o CPF do colaborador com 11 dígitos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var colaboradorData = await _colaboradorService.ObterPorCpfAsync(cpfLimpo, cts.Token);

            DefinirMensagem(string.Empty, true);
            if (colaboradorData == null)
            {
                Colaborador = null;
                EstaNaAcademia = false;
                TrabalhadoHoje = TimeSpan.Zero;
                NotificarPropriedadesVisuais();
                await Shell.Current.DisplayAlertAsync("Aviso", "Colaborador não encontrado para o CPF informado.", "OK");
                return;
            }

            Colaborador = colaboradorData;
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
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Toque em um colaborador da lista "na academia agora": carrega o perfil dele no topo da tela
    public async Task SelecionarPresenteAsync(AcessoColaboradorDto presente)
    {
        if (presente?.ColaboradorAcesso == null || IsBusy) return;
        try
        {
            IsBusy = true;
            DefinirMensagem(string.Empty, true);
            Colaborador = presente.ColaboradorAcesso;
            Cpf = presente.ColaboradorAcesso.Cpf;
            await CarregarSituacaoAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Botão "Registrar Saída" de um colaborador da lista "na academia agora"
    public async Task RegistrarSaidaDoPresenteAsync(AcessoColaboradorDto presente)
    {
        if (presente?.ColaboradorAcesso == null || IsBusy) return;
        Colaborador = presente.ColaboradorAcesso;
        Cpf = presente.ColaboradorAcesso.Cpf;
        await RegistrarAsync(AppAcessoTipo.Saida);
    }

    [RelayCommand]
    public Task RegistrarEntradaAsync() => RegistrarAsync(AppAcessoTipo.Entrada);

    [RelayCommand]
    public Task RegistrarSaidaAsync() => RegistrarAsync(AppAcessoTipo.Saida);

    private async Task RegistrarAsync(AppAcessoTipo tipo)
    {
        if (IsBusy) return;
        if (!HasColaborador)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Busque o colaborador pelo CPF antes de registrar.", "OK");
            return;
        }

        string operacao = tipo.GetDisplayName(); // "Entrada" ou "Saída"
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // As regras (limite de 8h CLT / 6h estágio, soma do dia) ficam no AcessoColaboradorService
            var registro = tipo == AppAcessoTipo.Entrada
                ? await _acessoService.RegistrarEntradaAsync(Colaborador!.Id, cts.Token)
                : await _acessoService.RegistrarSaidaAsync(Colaborador!.Id, cts.Token);

            await CarregarSituacaoAsync();
            await CarregarListasAsync();
            DefinirMensagem(registro.Mensagem ?? $"{operacao} registrada.", true);
            await Shell.Current.DisplayAlertAsync($"{operacao} registrada", $"{Colaborador.Nome}\n\n{registro.Mensagem}", "OK");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O registro expirou. Verifique a conexão.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            // regra de negócio não atendida (ex.: limite diário de horas atingido)
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
        Colaborador = null;
        EstaNaAcademia = false;
        TrabalhadoHoje = TimeSpan.Zero;
        DefinirMensagem(string.Empty, true);
    }
}
