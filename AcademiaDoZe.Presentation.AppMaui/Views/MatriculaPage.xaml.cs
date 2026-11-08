using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class MatriculaPage : ContentPage
{
    public MatriculaPage(MatriculaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MatriculaViewModel viewModel)
        {
            await viewModel.InitializeAsync();

            // garante que a data fim esteja calculada ao carregar (novo ou edição)
            CalculateFimFromPlanoAndInicio();
        }
    }

    private void PlanoPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        CalculateFimFromPlanoAndInicio();
    }

    private void InicioDatePicker_DateSelected(object? sender, DateChangedEventArgs e)
    {
        CalculateFimFromPlanoAndInicio();
    }

    // Data fim é sempre calculada automaticamente a partir do plano + data de início
    private void CalculateFimFromPlanoAndInicio()
    {
        if (BindingContext is not MatriculaViewModel vm) return;

        // Lê direto dos controles (evita depender da ordem em que o binding atualiza o DTO)
        if (PlanoPicker.SelectedItem is AppMatriculaPlano planoSelecionado)
            vm.Matricula.Plano = planoSelecionado;
        if (InicioDatePicker.Date is DateTime inicioSelecionado)
            vm.Matricula.DataInicio = DateOnly.FromDateTime(inicioSelecionado);

        var fim = MatriculaViewModel.CalcularDataFim(vm.Matricula.Plano, vm.Matricula.DataInicio);

        // atualiza o viewmodel e o DatePicker somente-leitura
        vm.Matricula.DataFim = fim;
        try
        {
            FimDatePicker.Date = fim.ToDateTime(TimeOnly.MinValue);
        }
        catch { }
    }
}
