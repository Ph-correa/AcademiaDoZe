using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AlunoListViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService; // Assumindo que você tem IAlunoService

    public ObservableCollection<string> FilterTypes { get; } = ["Nome", "CPF", "Id", "Email"];

    private ObservableCollection<AlunoDto> _alunos = new();
    public ObservableCollection<AlunoDto> Alunos
    {
        get => _alunos;
        set => SetProperty(ref _alunos, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Nome";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public AlunoListViewModel(IAlunoService alunoService)
    {
        _alunoService = alunoService;
        Title = "Alunos";
    }

    [RelayCommand]
    private async Task AddAlunoAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("aluno");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task EditAlunoAsync(AlunoDto aluno)
    {
        try
        {
            if (aluno == null) return;
            await Shell.Current.GoToAsync($"aluno?Id={aluno.Id}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de edição: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task LoadAlunosAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Alunos.Clear();
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunosList = await _alunoService.ObterTodosAsync(cts.Token); // Mudar para ObterTodosAsync ou similar

            if (alunosList != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var aluno in alunosList)
                    {
                        Alunos.Add(aluno);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar alunos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadAlunosAsync();
    }

    [RelayCommand]
    private async Task DeleteAlunoAsync(AlunoDto aluno)
    {
        if (aluno == null) return;
        bool confirm = await Shell.Current.DisplayAlertAsync("Confirmar Exclusão", $"Deseja realmente excluir o aluno {aluno.Nome}?", "Sim", "Não");

        if (!confirm) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _alunoService.RemoverAsync(aluno.Id, cts.Token);

            if (success)
            {
                Alunos.Remove(aluno);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno excluído com sucesso!", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao excluir aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }

    }
    [RelayCommand]
    private async Task SearchAlunosAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // Busca todos no banco de dados primeiro
            var todosAlunos = await _alunoService.ObterTodosAsync(cts.Token);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Alunos.Clear();
                if (todosAlunos == null) return;

                var filtrados = todosAlunos.AsEnumerable();

                // Se houver texto digitado, aplica o filtro
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var termo = SearchText.Trim().ToLower();

                    filtrados = SelectedFilterType switch
                    {
                        "Nome" => filtrados.Where(a => a.Nome != null && a.Nome.ToLower().Contains(termo)),
                        "CPF" => filtrados.Where(a => a.Cpf != null && a.Cpf.Contains(termo)),
                        "Id" => filtrados.Where(a => a.Id.ToString() == termo),
                        "Email" => filtrados.Where(a => a.Email != null && a.Email.ToLower().Contains(termo)),
                        _ => filtrados
                    };
                }

                // Adiciona o resultado filtrado na tela
                foreach (var aluno in filtrados)
                {
                    Alunos.Add(aluno);
                }
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar alunos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Nota: O SearchAlunosCommand seria semelhante ao do colaborador, 
    // precisando apenas iterar pelos campos conforme o SelectedFilterType.
}