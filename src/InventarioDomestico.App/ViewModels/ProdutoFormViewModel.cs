using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using InventarioDomestico.App.Services;
using InventarioDomestico.Core.Exceptions;
using InventarioDomestico.Core.Models;
using InventarioDomestico.Core.Services;

namespace InventarioDomestico.App.ViewModels;

[QueryProperty(nameof(ProdutoId), "id")]
public class ProdutoFormViewModel : BaseViewModel
{
    private readonly IInventarioService _service;
    private readonly IDialogService _dialogService;

    private int _produtoId;
    private bool _isEdicao;
    private string _nome = string.Empty;
    private string _categoria = string.Empty;
    private string _localArmazenamento = string.Empty;
    private string _unidadeMedida = UnidadeMedidaConsts.Unidade;
    private string _quantidadeInicialTexto = "0";
    private string _quantidadeMinimaTexto = string.Empty;

    public int ProdutoId
    {
        get => _produtoId;
        set
        {
            if (SetProperty(ref _produtoId, value))
            {
                IsEdicao = _produtoId > 0;
                Title = IsEdicao ? "Editar Produto" : "Novo Produto";
                if (IsEdicao)
                {
                    _ = CarregarProdutoParaEdicaoAsync(_produtoId);
                }
            }
        }
    }

    public bool IsEdicao
    {
        get => _isEdicao;
        set => SetProperty(ref _isEdicao, value);
    }

    public bool IsNovo => !IsEdicao;

    public string Nome
    {
        get => _nome;
        set => SetProperty(ref _nome, value);
    }

    public string Categoria
    {
        get => _categoria;
        set => SetProperty(ref _categoria, value);
    }

    public string LocalArmazenamento
    {
        get => _localArmazenamento;
        set => SetProperty(ref _localArmazenamento, value);
    }

    public string UnidadeMedida
    {
        get => _unidadeMedida;
        set => SetProperty(ref _unidadeMedida, value);
    }

    public string QuantidadeInicialTexto
    {
        get => _quantidadeInicialTexto;
        set => SetProperty(ref _quantidadeInicialTexto, value);
    }

    public string QuantidadeMinimaTexto
    {
        get => _quantidadeMinimaTexto;
        set => SetProperty(ref _quantidadeMinimaTexto, value);
    }

    public ObservableCollection<string> CategoriasDisponiveis { get; } = new();
    public ObservableCollection<string> LocaisDisponiveis { get; } = new();
    public ObservableCollection<string> UnidadesDisponiveis { get; } = new();

    public ICommand SalvarCommand { get; }
    public ICommand CancelarCommand { get; }
    public ICommand AdicionarCategoriaCommand { get; }
    public ICommand AdicionarLocalCommand { get; }

    public ProdutoFormViewModel(IInventarioService service, IDialogService dialogService)
    {
        _service = service;
        _dialogService = dialogService;
        Title = "Novo Produto";

        foreach (var u in UnidadeMedidaConsts.TodasUnidades)
        {
            UnidadesDisponiveis.Add(u);
        }

        SalvarCommand = new Command(async () => await SalvarAsync());
        CancelarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        AdicionarCategoriaCommand = new Command(async () => await AdicionarNovaCategoriaAsync());
        AdicionarLocalCommand = new Command(async () => await AdicionarNovoLocalAsync());
    }

    public async Task InicializarListasAsync()
    {
        try
        {
            var cats = await _service.ObterCategoriasAsync();
            CategoriasDisponiveis.Clear();
            foreach (var c in cats)
            {
                CategoriasDisponiveis.Add(c.Nome);
            }
            if (string.IsNullOrEmpty(Categoria) && CategoriasDisponiveis.Count > 0)
            {
                Categoria = CategoriasDisponiveis[0];
            }

            var locais = await _service.ObterLocaisAsync();
            LocaisDisponiveis.Clear();
            foreach (var l in locais)
            {
                LocaisDisponiveis.Add(l.Nome);
            }
            if (string.IsNullOrEmpty(LocalArmazenamento) && LocaisDisponiveis.Count > 0)
            {
                LocalArmazenamento = LocaisDisponiveis[0];
            }
        }
        catch { }
    }

    private async Task CarregarProdutoParaEdicaoAsync(int id)
    {
        try
        {
            await InicializarListasAsync();
            var p = await _service.ObterProdutoPorIdAsync(id);
            if (p != null)
            {
                Nome = p.Nome;
                Categoria = p.Categoria;
                LocalArmazenamento = p.LocalArmazenamento;
                UnidadeMedida = p.UnidadeMedida;
                QuantidadeMinimaTexto = p.QuantidadeMinima?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Falha ao carregar produto para edição: {ex.Message}");
        }
    }

    private async Task SalvarAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Nome))
        {
            await _dialogService.ShowAlertAsync("Aviso", "O campo 'Nome do Produto' é obrigatório.");
            return;
        }

        decimal? qtdMinima = null;
        if (!string.IsNullOrWhiteSpace(QuantidadeMinimaTexto))
        {
            if (!TryParseDecimal(QuantidadeMinimaTexto, out var min) || min < 0m)
            {
                await _dialogService.ShowAlertAsync("Aviso", "A quantidade mínima deve ser um número válido e não negativo.");
                return;
            }
            qtdMinima = min;
        }

        decimal qtdInicial = 0m;
        if (IsNovo && !string.IsNullOrWhiteSpace(QuantidadeInicialTexto))
        {
            if (!TryParseDecimal(QuantidadeInicialTexto, out var ini) || ini < 0m)
            {
                await _dialogService.ShowAlertAsync("Aviso", "A quantidade inicial deve ser um número válido e não negativo.");
                return;
            }
            qtdInicial = ini;
        }

        IsBusy = true;
        try
        {
            if (IsEdicao)
            {
                var produto = new Produto
                {
                    Id = ProdutoId,
                    Nome = Nome.Trim(),
                    Categoria = Categoria,
                    LocalArmazenamento = LocalArmazenamento,
                    UnidadeMedida = UnidadeMedida,
                    QuantidadeMinima = qtdMinima
                };

                await _service.AtualizarProdutoAsync(produto);
                await _dialogService.ShowAlertAsync("Sucesso", "Produto atualizado com sucesso!");
            }
            else
            {
                var produto = new Produto
                {
                    Nome = Nome.Trim(),
                    Categoria = Categoria,
                    LocalArmazenamento = LocalArmazenamento,
                    UnidadeMedida = UnidadeMedida,
                    QuantidadeMinima = qtdMinima
                };

                await _service.CadastrarProdutoAsync(produto, quantidadeInicial: qtdInicial);
                await _dialogService.ShowAlertAsync("Sucesso", "Produto cadastrado com sucesso!");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (InventarioException ex)
        {
            await _dialogService.ShowAlertAsync("Erro de Validação", ex.Message);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Ocorreu um erro ao salvar: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AdicionarNovaCategoriaAsync()
    {
        var nome = await _dialogService.ShowPromptAsync("Nova Categoria", "Informe o nome da nova categoria:", placeholder: "Ex: Frios");
        if (!string.IsNullOrWhiteSpace(nome))
        {
            try
            {
                var cat = await _service.AdicionarCategoriaAsync(nome);
                if (!CategoriasDisponiveis.Contains(cat.Nome))
                {
                    CategoriasDisponiveis.Add(cat.Nome);
                }
                Categoria = cat.Nome;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erro", ex.Message);
            }
        }
    }

    private async Task AdicionarNovoLocalAsync()
    {
        var nome = await _dialogService.ShowPromptAsync("Novo Local", "Informe o local de armazenamento:", placeholder: "Ex: Armário Superior");
        if (!string.IsNullOrWhiteSpace(nome))
        {
            try
            {
                var loc = await _service.AdicionarLocalAsync(nome);
                if (!LocaisDisponiveis.Contains(loc.Nome))
                {
                    LocaisDisponiveis.Add(loc.Nome);
                }
                LocalArmazenamento = loc.Nome;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Erro", ex.Message);
            }
        }
    }

    private static bool TryParseDecimal(string input, out decimal result)
    {
        var normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}
