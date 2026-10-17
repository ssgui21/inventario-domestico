using System.Globalization;
using System.Windows.Input;
using InventarioDomestico.App.Services;
using InventarioDomestico.Core.Exceptions;
using InventarioDomestico.Core.Models;
using InventarioDomestico.Core.Services;

namespace InventarioDomestico.App.ViewModels;

[QueryProperty(nameof(ProdutoId), "id")]
[QueryProperty(nameof(TipoMovimentacaoTexto), "tipo")]
public class MovimentacaoViewModel : BaseViewModel
{
    private readonly IInventarioService _service;
    private readonly IDialogService _dialogService;

    private int _produtoId;
    private string _tipoMovimentacaoTexto = "Entrada";
    private Produto? _produto;
    private string _quantidadeTexto = string.Empty;
    private string _observacao = string.Empty;
    private decimal _saldoPrevisto;
    private string _alertaValidacao = string.Empty;

    public int ProdutoId
    {
        get => _produtoId;
        set
        {
            if (SetProperty(ref _produtoId, value))
            {
                _ = CarregarProdutoAsync(_produtoId);
            }
        }
    }

    public string TipoMovimentacaoTexto
    {
        get => _tipoMovimentacaoTexto;
        set
        {
            if (SetProperty(ref _tipoMovimentacaoTexto, value))
            {
                AtualizarTituloEOperacao();
                AtualizarPrevisaoSaldo();
            }
        }
    }

    public Produto? Produto
    {
        get => _produto;
        set
        {
            if (SetProperty(ref _produto, value))
            {
                AtualizarPrevisaoSaldo();
            }
        }
    }

    public string QuantidadeTexto
    {
        get => _quantidadeTexto;
        set
        {
            if (SetProperty(ref _quantidadeTexto, value))
            {
                AtualizarPrevisaoSaldo();
            }
        }
    }

    public string Observacao
    {
        get => _observacao;
        set => SetProperty(ref _observacao, value);
    }

    public decimal SaldoPrevisto
    {
        get => _saldoPrevisto;
        set => SetProperty(ref _saldoPrevisto, value);
    }

    public string AlertaValidacao
    {
        get => _alertaValidacao;
        set => SetProperty(ref _alertaValidacao, value);
    }

    public bool TemAlertaValidacao => !string.IsNullOrEmpty(AlertaValidacao);

    public ICommand ConfirmarCommand { get; }
    public ICommand CancelarCommand { get; }

    public MovimentacaoViewModel(IInventarioService service, IDialogService dialogService)
    {
        _service = service;
        _dialogService = dialogService;
        AtualizarTituloEOperacao();

        ConfirmarCommand = new Command(async () => await ConfirmarAsync());
        CancelarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    private void AtualizarTituloEOperacao()
    {
        Title = TipoMovimentacaoTexto switch
        {
            "Saida" => "Registrar Saída de Estoque",
            "Ajuste" => "Ajustar Saldo de Estoque",
            _ => "Registrar Entrada de Estoque"
        };
    }

    private async Task CarregarProdutoAsync(int id)
    {
        try
        {
            var p = await _service.ObterProdutoPorIdAsync(id);
            Produto = p;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Falha ao carregar produto: {ex.Message}");
        }
    }

    private void AtualizarPrevisaoSaldo()
    {
        if (Produto == null) return;

        AlertaValidacao = string.Empty;

        if (string.IsNullOrWhiteSpace(QuantidadeTexto))
        {
            SaldoPrevisto = Produto.QuantidadeAtual;
            OnPropertyChanged(nameof(TemAlertaValidacao));
            return;
        }

        if (!TryParseDecimal(QuantidadeTexto, out var qtd) || (TipoMovimentacaoTexto != "Ajuste" && qtd <= 0m) || (TipoMovimentacaoTexto == "Ajuste" && qtd < 0m))
        {
            AlertaValidacao = "Informe um valor numérico válido maior que zero.";
            SaldoPrevisto = Produto.QuantidadeAtual;
            OnPropertyChanged(nameof(TemAlertaValidacao));
            return;
        }

        if (TipoMovimentacaoTexto.Equals("Entrada", StringComparison.OrdinalIgnoreCase))
        {
            SaldoPrevisto = Produto.QuantidadeAtual + qtd;
        }
        else if (TipoMovimentacaoTexto.Equals("Saida", StringComparison.OrdinalIgnoreCase))
        {
            if (qtd > Produto.QuantidadeAtual)
            {
                AlertaValidacao = $"Quantidade indisponível! Estoque atual é de apenas {Produto.QuantidadeAtual:0.##}.";
                SaldoPrevisto = Produto.QuantidadeAtual;
            }
            else
            {
                SaldoPrevisto = Produto.QuantidadeAtual - qtd;
            }
        }
        else if (TipoMovimentacaoTexto.Equals("Ajuste", StringComparison.OrdinalIgnoreCase))
        {
            SaldoPrevisto = qtd;
        }

        OnPropertyChanged(nameof(TemAlertaValidacao));
    }

    private async Task ConfirmarAsync()
    {
        if (IsBusy || Produto == null) return;

        if (!TryParseDecimal(QuantidadeTexto, out var qtd))
        {
            await _dialogService.ShowAlertAsync("Aviso", "Informe uma quantidade válida.");
            return;
        }

        IsBusy = true;
        try
        {
            if (TipoMovimentacaoTexto.Equals("Entrada", StringComparison.OrdinalIgnoreCase))
            {
                await _service.RegistrarEntradaAsync(Produto.Id, qtd, observacao: Observacao);
                await _dialogService.ShowAlertAsync("Sucesso", $"Entrada de {qtd:0.##} registrada com sucesso!");
            }
            else if (TipoMovimentacaoTexto.Equals("Saida", StringComparison.OrdinalIgnoreCase))
            {
                await _service.RegistrarSaidaAsync(Produto.Id, qtd, observacao: Observacao);
                await _dialogService.ShowAlertAsync("Sucesso", $"Saída de {qtd:0.##} registrada com sucesso!");
            }
            else if (TipoMovimentacaoTexto.Equals("Ajuste", StringComparison.OrdinalIgnoreCase))
            {
                var confirmar = await _dialogService.ShowConfirmAsync(
                    "Confirmar Ajuste",
                    $"Deseja alterar o estoque de {Produto.QuantidadeAtual:0.##} para {qtd:0.##}?",
                    accept: "Confirmar Ajuste",
                    cancel: "Cancelar"
                );

                if (!confirmar) return;

                await _service.RegistrarAjusteAsync(Produto.Id, qtd, observacao: Observacao);
                await _dialogService.ShowAlertAsync("Sucesso", $"Saldo ajustado para {qtd:0.##} com sucesso!");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (EstoqueInsuficienteException ex)
        {
            await _dialogService.ShowAlertAsync("Estoque Insuficiente", ex.Message);
        }
        catch (InventarioException ex)
        {
            await _dialogService.ShowAlertAsync("Erro de Validação", ex.Message);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Erro", $"Erro ao registrar movimentação: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool TryParseDecimal(string input, out decimal result)
    {
        var normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}
