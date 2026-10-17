using System.Collections.ObjectModel;
using System.Windows.Input;
using InventarioDomestico.App.Services;
using InventarioDomestico.Core.Models;
using InventarioDomestico.Core.Services;

namespace InventarioDomestico.App.ViewModels;

public class ProdutosViewModel : BaseViewModel
{
    private readonly IInventarioService _service;
    private readonly IDialogService _dialogService;

    private string _textoBusca = string.Empty;
    private string _categoriaSelecionada = "Todas";
    private string _localSelecionado = "Todos";
    private bool _apenasReposicao;
    private bool _temProdutos;

    public string TextoBusca
    {
        get => _textoBusca;
        set
        {
            if (SetProperty(ref _textoBusca, value))
            {
                _ = CarregarProdutosAsync();
            }
        }
    }

    public string CategoriaSelecionada
    {
        get => _categoriaSelecionada;
        set
        {
            if (SetProperty(ref _categoriaSelecionada, value))
            {
                _ = CarregarProdutosAsync();
            }
        }
    }

    public string LocalSelecionado
    {
        get => _localSelecionado;
        set
        {
            if (SetProperty(ref _localSelecionado, value))
            {
                _ = CarregarProdutosAsync();
            }
        }
    }

    public bool ApenasReposicao
    {
        get => _apenasReposicao;
        set
        {
            if (SetProperty(ref _apenasReposicao, value))
            {
                _ = CarregarProdutosAsync();
            }
        }
    }

    public bool TemProdutos
    {
        get => _temProdutos;
        set => SetProperty(ref _temProdutos, value);
    }

    public ObservableCollection<Produto> Produtos { get; } = new();
    public ObservableCollection<string> CategoriasFiltro { get; } = new();
    public ObservableCollection<string> LocaisFiltro { get; } = new();

    public ICommand CarregarProdutosCommand { get; }
    public ICommand NovoProdutoCommand { get; }
    public ICommand SelecionarProdutoCommand { get; }
    public ICommand LimparFiltrosCommand { get; }

    public ProdutosViewModel(IInventarioService service, IDialogService dialogService)
    {
        _service = service;
        _dialogService = dialogService;
        Title = "Produtos em Estoque";

        CarregarProdutosCommand = new Command(async () => await CarregarProdutosAsync());
        NovoProdutoCommand = new Command(async () => await Shell.Current.GoToAsync("ProdutoFormPage"));
        SelecionarProdutoCommand = new Command<Produto>(async (p) =>
        {
            if (p != null)
            {
                await Shell.Current.GoToAsync($"ProdutoDetalhePage?id={p.Id}");
            }
        });
        LimparFiltrosCommand = new Command(() =>
        {
            _textoBusca = string.Empty;
            OnPropertyChanged(nameof(TextoBusca));
            _categoriaSelecionada = "Todas";
            OnPropertyChanged(nameof(CategoriaSelecionada));
            _localSelecionado = "Todos";
            OnPropertyChanged(nameof(LocalSelecionado));
            _apenasReposicao = false;
            OnPropertyChanged(nameof(ApenasReposicao));
            _ = CarregarProdutosAsync();
        });
    }

    public async Task InicializarFiltrosAsync()
    {
        try
        {
            var cats = await _service.ObterCategoriasAsync();
            CategoriasFiltro.Clear();
            CategoriasFiltro.Add("Todas");
            foreach (var c in cats)
            {
                CategoriasFiltro.Add(c.Nome);
            }

            var locais = await _service.ObterLocaisAsync();
            LocaisFiltro.Clear();
            LocaisFiltro.Add("Todos");
            foreach (var l in locais)
            {
                LocaisFiltro.Add(l.Nome);
            }
        }
        catch { }
    }

    public async Task CarregarProdutosAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (CategoriasFiltro.Count <= 1)
            {
                await InicializarFiltrosAsync();
            }

            var itens = await _service.ObterProdutosAsync(
                busca: TextoBusca,
                categoria: CategoriaSelecionada,
                local: LocalSelecionado,
                apenasReposicao: ApenasReposicao
            );

            Produtos.Clear();
            foreach (var item in itens)
            {
                Produtos.Add(item);
            }

            TemProdutos = Produtos.Count > 0;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Falha ao listar produtos: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
