namespace InventarioDomestico.Core.Exceptions;

public class InventarioException : Exception
{
    public InventarioException(string message) : base(message) { }
    public InventarioException(string message, Exception innerException) : base(message, innerException) { }
}

public class ProdutoInvalidoException : InventarioException
{
    public ProdutoInvalidoException(string message) : base(message) { }
}

public class EstoqueInsuficienteException : InventarioException
{
    public decimal QuantidadeAtual { get; }
    public decimal QuantidadeSolicitada { get; }

    public EstoqueInsuficienteException(decimal atual, decimal solicitada)
        : base($"Estoque insuficiente: tentativa de retirar {solicitada:0.##}, mas há apenas {atual:0.##} disponível.")
    {
        QuantidadeAtual = atual;
        QuantidadeSolicitada = solicitada;
    }
}

public class QuantidadeInvalidaException : InventarioException
{
    public decimal Quantidade { get; }

    public QuantidadeInvalidaException(decimal quantidade, string motivo)
        : base($"Quantidade inválida ({quantidade:0.##}): {motivo}")
    {
        Quantidade = quantidade;
    }
}

public class UnidadeIncompativelException : InventarioException
{
    public string UnidadeProduto { get; }
    public string UnidadeMovimentacao { get; }

    public UnidadeIncompativelException(string unidadeProduto, string unidadeMovimentacao)
        : base($"Unidades incompatíveis: o produto usa '{unidadeProduto}', não é permitido movimentar com '{unidadeMovimentacao}'.")
    {
        UnidadeProduto = unidadeProduto;
        UnidadeMovimentacao = unidadeMovimentacao;
    }
}

public class OperacaoInvalidaException : InventarioException
{
    public OperacaoInvalidaException(string message) : base(message) { }
}
