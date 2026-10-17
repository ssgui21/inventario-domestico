using SQLite;

namespace InventarioDomestico.Core.Models;

[Table("Produtos")]
public class Produto
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, NotNull]
    public string Nome { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal QuantidadeAtual { get; set; } = 0m;

    public string UnidadeMedida { get; set; } = UnidadeMedidaConsts.Unidade;

    public string LocalArmazenamento { get; set; } = string.Empty;

    public decimal? QuantidadeMinima { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public DateTime? DataUltimaAtualizacao { get; set; }

    [Ignore]
    public bool PrecisaReposicao => QuantidadeMinima.HasValue && QuantidadeAtual <= QuantidadeMinima.Value;

    [Ignore]
    public string QuantidadeFormatada => $"{QuantidadeAtual:0.##} {ObterSiglaUnidade(UnidadeMedida)}";

    [Ignore]
    public string QuantidadeMinimaFormatada => QuantidadeMinima.HasValue 
        ? $"{QuantidadeMinima.Value:0.##} {ObterSiglaUnidade(UnidadeMedida)}" 
        : "Não configurado";

    [Ignore]
    public string StatusEstoqueTexto
    {
        get
        {
            if (!QuantidadeMinima.HasValue)
                return "Estoque normal (sem mínimo)";

            if (QuantidadeAtual <= 0)
                return "ESTOQUE ZERADO — Requer reposição imediata";

            if (QuantidadeAtual <= QuantidadeMinima.Value)
                return $"ESTOQUE BAIXO — Abaixo do mínimo de {QuantidadeMinima.Value:0.##}";

            return "Estoque suficiente";
        }
    }

    [Ignore]
    public string IndicadorAviso => PrecisaReposicao ? "⚠️ Reposição Necessária" : "✓ OK";

    private static string ObterSiglaUnidade(string unidade)
    {
        if (string.IsNullOrWhiteSpace(unidade)) return "";
        var inicio = unidade.IndexOf('(');
        var fim = unidade.IndexOf(')');
        if (inicio >= 0 && fim > inicio)
        {
            return unidade.Substring(inicio + 1, fim - inicio - 1);
        }
        return unidade;
    }
}
