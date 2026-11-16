using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class AcessoColaboradorPage : ContentPage
{
    public AcessoColaboradorPage(AcessoColaboradorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (BindingContext is AcessoColaboradorViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", $"Erro ao carregar a tela: {ex.Message}", "OK");
        }
    }

    // Toque no card de quem está na academia: carrega o perfil no topo da tela
    private async void OnPresenteTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject item && item.BindingContext is AcessoColaboradorDto presente && BindingContext is AcessoColaboradorViewModel viewModel)
        {
            await viewModel.SelecionarPresenteAsync(presente);
        }
    }

    // Botão "Registrar Saída" direto na lista de quem está na academia
    private async void OnSaidaPresenteClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is AcessoColaboradorDto presente && BindingContext is AcessoColaboradorViewModel viewModel)
        {
            await viewModel.RegistrarSaidaDoPresenteAsync(presente);
        }
    }

    // Troca do dia do histórico
    private async void HistoricoDatePicker_DateSelected(object? sender, DateChangedEventArgs e)
    {
        if (BindingContext is not AcessoColaboradorViewModel viewModel) return;
        if (HistoricoDatePicker.Date is DateTime dia && dia.Date != viewModel.DataHistorico.Date)
        {
            viewModel.DataHistorico = dia.Date;
            await viewModel.AtualizarCommand.ExecuteAsync(null);
        }
    }
}
