using SQLite;

namespace InventarioDomestico.Core.Models;

[Table("MovimentacoesEstoque")]
public class MovimentacaoEstoque
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ProdutoId { get; set; }

    public TipoMovimentacao Tipo { get; set; }

    public decimal Quantidade { get; set; }

    public decimal QuantidadeAnterior { get; set; }

    public decimal QuantidadeResultante { get; set; }

    public string UnidadeMedida { get; set; } = string.Empty;

    public DateTime DataHora { get; set; } = DateTime.Now;

    public string? Observacao { get; set; }

    [Ignore]
    public string TipoDescricao => Tipo switch
    {
        TipoMovimentacao.Entrada => "Entrada (+)",
        TipoMovimentacao.Saida => "Saída (-)",
        TipoMovimentacao.Ajuste => "Ajuste de Saldo",
        _ => Tipo.ToString()
    };

    [Ignore]
    public string QuantidadeFormatada
    {
        get
        {
            var sinal = Tipo == TipoMovimentacao.Entrada ? "+" : (Tipo == TipoMovimentacao.Saida ? "-" : "");
            return $"{sinal}{Quantidade:0.##}";
        }
    }

    [Ignore]
    public string ResumoMovimentacao => 
        $"{TipoDescricao}: {QuantidadeFormatada} (Saldo: {QuantidadeAnterior:0.##} ➔ {QuantidadeResultante:0.##})";
}
