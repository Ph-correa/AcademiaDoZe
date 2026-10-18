using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(AlunoId), "Id")]
public partial class AlunoViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;
    private readonly ILogradouroService _logradouroService;

    private AlunoDto _aluno = CriarAlunoPadrao();
    public AlunoDto Aluno
    {
        get => _aluno;
        set => SetProperty(ref _aluno, value);
    }

    private int _alunoId;
    public int AlunoId
    {
        get => _alunoId;
        set => SetProperty(ref _alunoId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha
    {
        get => _confirmarSenha;
        set => SetProperty(ref _confirmarSenha, value);
    }

    public bool HasEnderecoVinculado => Aluno?.Endereco != null && Aluno.Endereco.Id > 0;
    public bool HasNoEnderecoVinculado => !HasEnderecoVinculado;

    public AlunoViewModel(IAlunoService alunoService, ILogradouroService logradouroService)
    {
        _alunoService = alunoService;
        _logradouroService = logradouroService;
        Title = "Detalhes do Aluno";
    }

    public async Task InitializeAsync()
    {
        ConfirmarSenha = string.Empty;

        if (AlunoId > 0)
        {
            IsEditMode = true;
            Title = "Editar Aluno";
            await LoadAlunoAsync();
        }
        else
        {
            IsEditMode = false;
            Title = "Novo Aluno";
            Aluno = CriarAlunoPadrao();
            NotificarStatusEndereco();
        }
    }

    private void NotificarStatusEndereco()
    {
        OnPropertyChanged(nameof(HasEnderecoVinculado));
        OnPropertyChanged(nameof(HasNoEnderecoVinculado));
    }

    private static AlunoDto CriarAlunoPadrao() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Email = string.Empty,
        Senha = string.Empty,
        Endereco = new LogradouroDto
        {
            Cep = string.Empty,
            Nome = string.Empty,
            Bairro = string.Empty,
            Cidade = string.Empty,
            Estado = string.Empty,
            Pais = "Brasil"
        },
        Numero = string.Empty,
        Complemento = string.Empty
    };

    public async Task LoadAlunoAsync()
    {
        if (AlunoId <= 0) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunoData = await _alunoService.ObterPorIdAsync(AlunoId, cts.Token);

            if (alunoData != null)
            {
                alunoData.Endereco ??= new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = "Brasil"
                };

                alunoData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;
                Aluno = alunoData;
                NotificarStatusEndereco();
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task SearchByCpfAsync()
    {
        if (string.IsNullOrWhiteSpace(Aluno.Cpf))
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Informe o CPF para realizar a busca.", "OK");
            return;
        }

        var apenasDigitos = new string([.. Aluno.Cpf.Where(char.IsDigit)]);
        if (apenasDigitos.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "O CPF deve conter exatamente 11 dígitos numéricos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunoData = await _alunoService.ObterPorCpfAsync(apenasDigitos, cts.Token);

            if (alunoData != null)
            {
                alunoData.Endereco ??= new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = "Brasil"
                };

                alunoData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;
                Aluno = alunoData;
                AlunoId = alunoData.Id;
                IsEditMode = true;
                Title = "Editar Aluno";
                NotificarStatusEndereco();
                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno já cadastrado! Dados carregados para edição.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "CPF não encontrado.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca por CPF expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar CPF: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchByCepAsync()
    {
        if (Aluno.Endereco == null || string.IsNullOrWhiteSpace(Aluno.Endereco.Cep))
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Informe o CEP para realizar a busca.", "OK");
            return;
        }

        var apenasDigitos = new string([.. Aluno.Endereco.Cep.Where(char.IsDigit)]);
        if (apenasDigitos.Length != 8)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "O CEP deve conter exatamente 8 dígitos numéricos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var logradouroData = await _logradouroService.ObterPorCepAsync(apenasDigitos, cts.Token);

            if (logradouroData != null)
            {
                Aluno.Endereco = logradouroData;
                NotificarStatusEndereco();
                OnPropertyChanged(nameof(Aluno));
                await Shell.Current.DisplayAlertAsync("Aviso", "CEP encontrado! Endereço preenchido automaticamente.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "CEP não encontrado no cadastro de logradouros. Cadastre o logradouro primeiro.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca de CEP expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar CEP: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelecionarFotoAsync()
    {
        try
        {
            string escolha = await Shell.Current.DisplayActionSheetAsync("Origem da Imagem", "Cancelar", "Remover Foto", "Galeria", "Câmera");
            if (escolha == "Remover Foto")
            {
                Aluno.Foto = null;
                OnPropertyChanged(nameof(Aluno));
                return;
            }

            FileResult? result = null;

            if (escolha == "Galeria")
            {
                result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selecione uma imagem", FileTypes = FilePickerFileType.Images });
            }
            else if (escolha == "Câmera")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    result = await MediaPicker.Default.CapturePhotoAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Erro", "Captura de foto não suportada neste dispositivo.", "OK");
                    return;
                }
            }

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                // Adaptação: Se o seu DTO for array de bytes direto:
                // Aluno.Foto = ms.ToArray(); 

                // Se no seu projeto a Foto for da classe ArquivoDto (igual ao pdf do Colaborador):
                Aluno.Foto = new ArquivoDto { Conteudo = ms.ToArray() };

                OnPropertyChanged(nameof(Aluno));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar imagem: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task SaveAlunoAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Se o endereço for digitado mas não tem ID ainda, tenta buscar pelo CEP
            if (Aluno.Endereco != null && Aluno.Endereco.Id <= 0 && !string.IsNullOrWhiteSpace(Aluno.Endereco.Cep))
            {
                var cepLimpo = new string([.. Aluno.Endereco.Cep.Where(char.IsDigit)]);
                if (cepLimpo.Length == 8)
                {
                    var logradouroData = await _logradouroService.ObterPorCepAsync(cepLimpo, cts.Token);
                    if (logradouroData != null)
                    {
                        Aluno.Endereco = logradouroData;
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync("Endereço Obrigatório", "O CEP informado não está cadastrado como Logradouro. Cadastre-o previamente.", "OK");
                        return;
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Informe um CEP válido com 8 dígitos para o endereço.", "OK");
                    return;
                }
            }

            // Normaliza os dados antes de persistir
            Aluno.Nome = Aluno.Nome?.Trim() ?? string.Empty;
            Aluno.Cpf = new string([.. Aluno.Cpf?.Where(char.IsDigit) ?? []]);
            Aluno.Telefone = new string([.. Aluno.Telefone?.Where(char.IsDigit) ?? []]);
            Aluno.Numero = Aluno.Numero?.Trim() ?? string.Empty;
            Aluno.Complemento = string.IsNullOrWhiteSpace(Aluno.Complemento) ? string.Empty : Aluno.Complemento.Trim();
            Aluno.Email = string.IsNullOrWhiteSpace(Aluno.Email) ? string.Empty : Aluno.Email.Trim();
            Aluno.Senha = string.IsNullOrWhiteSpace(Aluno.Senha) ? string.Empty : Aluno.Senha.Trim();

            if (IsEditMode)
            {
                await _alunoService.AtualizarAsync(Aluno, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno atualizado com sucesso!", "OK");
            }
            else
            {
                await _alunoService.AdicionarAsync(Aluno, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno criado com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A gravação do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            await Shell.Current.DisplayAlertAsync("Regra de Negócio", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            await Shell.Current.DisplayAlertAsync("Validação", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao salvar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}