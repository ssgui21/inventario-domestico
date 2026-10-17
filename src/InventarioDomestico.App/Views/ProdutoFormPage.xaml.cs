using InventarioDomestico.App.ViewModels;

namespace InventarioDomestico.App.Views;

public partial class ProdutoFormPage : ContentPage
{
    private readonly ProdutoFormViewModel _viewModel;

    public ProdutoFormPage(ProdutoFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InicializarListasAsync();
    }
}
