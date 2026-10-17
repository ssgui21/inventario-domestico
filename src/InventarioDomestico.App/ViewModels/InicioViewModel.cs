using System.Collections.ObjectModel;
using System.Windows.Input;
using InventarioDomestico.App.Services;
using InventarioDomestico.Core.Models;
using InventarioDomestico.Core.Services;

namespace InventarioDomestico.App.ViewModels;

public class InicioViewModel : BaseViewModel
{
    private readonly IInventarioService _service;
    private readonly IDialogService _dialogService;

    private int _totalProdutos;
    private int _totalReposicao;
    private int _totalZerados;
    private int _totalMovimentacoes;

    public int TotalProdutos
    {
        get => _totalProdutos;
        set => SetProperty(ref _totalProdutos, value);
    }

    public int TotalReposicao
    {
        get => _totalReposicao;
        set => SetProperty(ref _totalReposicao, value);
    }

    public int TotalZerados
    {
        get => _totalZerados;
        set => SetProperty(ref _totalZerados, value);
    }

    public int TotalMovimentacoes
    {
        get => _totalMovimentacoes;
        set => SetProperty(ref _totalMovimentacoes, value);
    }

    public ObservableCollection<Produto> ItensEmAlerta { get; } = new();

    public ICommand CarregarDadosCommand { get; }
    public ICommand NovoProdutoCommand { get; }
    public ICommand VerTodosProdutosCommand { get; }
    public ICommand SelecionarProdutoCommand { get; }

    public InicioViewModel(IInventarioService service, IDialogService dialogService)
    {
        _service = service;
        _dialogService = dialogService;
        Title = "Inventário Doméstico";

        CarregarDadosCommand = new Command(async () => await CarregarDadosAsync());
        NovoProdutoCommand = new Command(async () => await Shell.Current.GoToAsync("ProdutoFormPage"));
        VerTodosProdutosCommand = new Command(async () => await Shell.Current.GoToAsync("//ProdutosPage"));
        SelecionarProdutoCommand = new Command<Produto>(async (p) =>
        {
            if (p != null)
            {
                await Shell.Current.GoToAsync($"ProdutoDetalhePage?id={p.Id}");
            }
        });
    }

    public async Task CarregarDadosAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var resumo = await _service.ObterResumoAsync();
            TotalProdutos = resumo.TotalProdutos;
            TotalReposicao = resumo.TotalItensReposicao;
            TotalZerados = resumo.TotalItensZerados;
            TotalMovimentacoes = resumo.TotalMovimentacoes;

            var emAlerta = await _service.ObterProdutosAsync(apenasReposicao: true);
            ItensEmAlerta.Clear();
            foreach (var item in emAlerta)
            {
                ItensEmAlerta.Add(item);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Falha ao carregar resumo: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
