using InventarioDomestico.App.Views;

namespace InventarioDomestico.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("ProdutoFormPage", typeof(ProdutoFormPage));
        Routing.RegisterRoute("ProdutoDetalhePage", typeof(ProdutoDetalhePage));
        Routing.RegisterRoute("MovimentacaoPage", typeof(MovimentacaoPage));
        Routing.RegisterRoute("HistoricoPage", typeof(HistoricoPage));
    }
}
