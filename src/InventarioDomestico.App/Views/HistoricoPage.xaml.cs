using InventarioDomestico.App.ViewModels;

namespace InventarioDomestico.App.Views;

public partial class HistoricoPage : ContentPage
{
    private readonly HistoricoViewModel _viewModel;

    public HistoricoPage(HistoricoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarHistoricoAsync(_viewModel.ProdutoId);
    }
}
