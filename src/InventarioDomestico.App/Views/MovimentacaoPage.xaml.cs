using InventarioDomestico.App.ViewModels;

namespace InventarioDomestico.App.Views;

public partial class MovimentacaoPage : ContentPage
{
    public MovimentacaoPage(MovimentacaoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
