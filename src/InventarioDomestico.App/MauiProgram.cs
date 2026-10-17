using InventarioDomestico.App.Services;
using InventarioDomestico.App.ViewModels;
using InventarioDomestico.App.Views;
using InventarioDomestico.Core.Data;
using InventarioDomestico.Core.Services;
using Microsoft.Extensions.Logging;

namespace InventarioDomestico.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configuração do SQLite no diretório de dados privados do aplicativo (offline, R$ 0)
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "inventario_domestico.db");
        builder.Services.AddSingleton<IInventarioDatabase>(sp => new InventarioDatabase(dbPath));

        // Registro de Serviços da Arquitetura
        builder.Services.AddSingleton<IInventarioService, InventarioService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();

        // Registro de ViewModels (MVVM)
        builder.Services.AddTransient<InicioViewModel>();
        builder.Services.AddTransient<ProdutosViewModel>();
        builder.Services.AddTransient<ProdutoFormViewModel>();
        builder.Services.AddTransient<ProdutoDetalheViewModel>();
        builder.Services.AddTransient<MovimentacaoViewModel>();
        builder.Services.AddTransient<HistoricoViewModel>();

        // Registro das Views
        builder.Services.AddTransient<InicioPage>();
        builder.Services.AddTransient<ProdutosPage>();
        builder.Services.AddTransient<ProdutoFormPage>();
        builder.Services.AddTransient<ProdutoDetalhePage>();
        builder.Services.AddTransient<MovimentacaoPage>();
        builder.Services.AddTransient<HistoricoPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
