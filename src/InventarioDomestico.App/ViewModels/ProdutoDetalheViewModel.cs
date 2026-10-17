using System.Collections.ObjectModel;
using System.Windows.Input;
using InventarioDomestico.App.Services;
using InventarioDomestico.Core.Models;
using InventarioDomestico.Core.Services;

namespace InventarioDomestico.App.ViewModels;

[QueryProperty(nameof(ProdutoId), "id")]
public class ProdutoDetalheViewModel : BaseViewModel
{
    private readonly IInventarioService _service;
    private readonly IDialogService _dialogService;

    private int _produtoId;
    private Produto? _produto;

    public int ProdutoId
    {
        get => _produtoId;
        set
        {
            if (SetProperty(ref _produtoId, value))
            {
                _ = CarregarDetalhesAsync(_produtoId);
            }
        }
    }

    public Produto? Produto
    {
        get => _produto;
        set => SetProperty(ref _produto, value);
    }

    public ObservableCollection<MovimentacaoEstoque> UltimasMovimentacoes { get; } = new();

    public ICommand CarregarCommand { get; }
    public ICommand EntradaCommand { get; }
    public ICommand SaidaCommand { get; }
    public ICommand AjusteCommand { get; }
    public ICommand EditarCommand { get; }
    public ICommand ExcluirCommand { get; }
    public ICommand VerHistoricoCommand { get; }

    public ProdutoDetalheViewModel(IInventarioService service, IDialogService dialogService)
    {
        _service = service;
        _dialogService = dialogService;
        Title = "Detalhes do Produto";

        CarregarCommand = new Command(async () => await CarregarDetalhesAsync(ProdutoId));
        EntradaCommand = new Command(async () =>
        {
            if (Produto != null)
                await Shell.Current.GoToAsync($"MovimentacaoPage?id={Produto.Id}&tipo=Entrada");
        });
        SaidaCommand = new Command(async () =>
        {
            if (Produto != null)
                await Shell.Current.GoToAsync($"MovimentacaoPage?id={Produto.Id}&tipo=Saida");
        });
        AjusteCommand = new Command(async () =>
        {
            if (Produto != null)
                await Shell.Current.GoToAsync($"MovimentacaoPage?id={Produto.Id}&tipo=Ajuste");
        });
        EditarCommand = new Command(async () =>
        {
            if (Produto != null)
                await Shell.Current.GoToAsync($"ProdutoFormPage?id={Produto.Id}");
        });
        VerHistoricoCommand = new Command(async () =>
        {
            if (Produto != null)
                await Shell.Current.GoToAsync($"HistoricoPage?id={Produto.Id}");
        });
        ExcluirCommand = new Command(async () => await ExcluirAsync());
    }

    public async Task CarregarDetalhesAsync(int id)
    {
        if (id <= 0) return;
        IsBusy = true;

        try
        {
            var p = await _service.ObterProdutoPorIdAsync(id);
            if (p != null)
            {
                Produto = p;
                Title = p.Nome;

                var movs = await _service.ObterMovimentacoesPorProdutoAsync(id);
                UltimasMovimentacoes.Clear();
                foreach (var m in movs.Take(5))
                {
                    UltimasMovimentacoes.Add(m);
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Falha ao carregar detalhes: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExcluirAsync()
    {
        if (Produto == null) return;

        var confirmar = await _dialogService.ShowConfirmAsync(
            "Confirmar Exclusão",
            $"Tem certeza de que deseja excluir o produto '{Produto.Nome}'?\n\nEsta operação é definitiva.",
            accept: "Excluir",
            cancel: "Cancelar"
        );

        if (confirmar)
        {
            try
            {
                await _service.ExcluirProdutoAsync(Produto.Id);
                await _dialogService.ShowAlertAsync("Sucesso", "Produto excluído com sucesso.");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erro", $"Falha ao excluir produto: {ex.Message}");
            }
        }
    }
}
