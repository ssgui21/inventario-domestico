using InventarioDomestico.App.ViewModels;

namespace InventarioDomestico.App.Views;

public partial class ProdutoDetalhePage : ContentPage
{
    private readonly ProdutoDetalheViewModel _viewModel;

    public ProdutoDetalhePage(ProdutoDetalheViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.ProdutoId > 0)
        {
            await _viewModel.CarregarDetalhesAsync(_viewModel.ProdutoId);
        }
    }
}
