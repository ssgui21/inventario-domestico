using System.Collections.ObjectModel;
using System.Windows.Input;
using InventarioDomestico.App.Services;
using InventarioDomestico.Core.Models;
using InventarioDomestico.Core.Services;

namespace InventarioDomestico.App.ViewModels;

[QueryProperty(nameof(ProdutoId), "id")]
public class HistoricoViewModel : BaseViewModel
{
    private readonly IInventarioService _service;
    private readonly IDialogService _dialogService;

    private int _produtoId;
    private Produto? _produto;
    private string _subtitulo = string.Empty;
    private bool _temMovimentacoes;

    public int ProdutoId
    {
        get => _produtoId;
        set
        {
            if (SetProperty(ref _produtoId, value))
            {
                _ = CarregarHistoricoAsync(_produtoId);
            }
        }
    }

    public Produto? Produto
    {
        get => _produto;
        set => SetProperty(ref _produto, value);
    }

    public string Subtitulo
    {
        get => _subtitulo;
        set => SetProperty(ref _subtitulo, value);
    }

    public bool TemMovimentacoes
    {
        get => _temMovimentacoes;
        set => SetProperty(ref _temMovimentacoes, value);
    }

    public ObservableCollection<MovimentacaoEstoque> Movimentacoes { get; } = new();

    public ICommand CarregarCommand { get; }
    public ICommand VoltarCommand { get; }

    public HistoricoViewModel(IInventarioService service, IDialogService dialogService)
    {
        _service = service;
        _dialogService = dialogService;
        Title = "Histórico de Movimentações";

        CarregarCommand = new Command(async () => await CarregarHistoricoAsync(ProdutoId));
        VoltarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public async Task CarregarHistoricoAsync(int id)
    {
        IsBusy = true;
        try
        {
            List<MovimentacaoEstoque> lista;
            if (id > 0)
            {
                Produto = await _service.ObterProdutoPorIdAsync(id);
                if (Produto != null)
                {
                    Title = $"Histórico: {Produto.Nome}";
                    Subtitulo = $"Saldo atual: {Produto.QuantidadeFormatada}";
                }
                lista = await _service.ObterMovimentacoesPorProdutoAsync(id);
            }
            else
            {
                Title = "Histórico Geral";
                Subtitulo = "Todas as movimentações recentes";
                lista = await _service.ObterHistoricoGeralAsync();
            }

            Movimentacoes.Clear();
            foreach (var m in lista)
            {
                Movimentacoes.Add(m);
            }

            TemMovimentacoes = Movimentacoes.Count > 0;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Falha ao carregar histórico: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
