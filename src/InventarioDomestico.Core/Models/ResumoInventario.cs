namespace InventarioDomestico.Core.Models;

public class ResumoInventario
{
    public int TotalProdutos { get; set; }
    public int TotalItensReposicao { get; set; }
    public int TotalItensZerados { get; set; }
    public int TotalMovimentacoes { get; set; }
    public int TotalCategorias { get; set; }
    public int TotalLocais { get; set; }
}
