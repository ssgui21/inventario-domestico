using InventarioDomestico.Core.Models;

namespace InventarioDomestico.Core.Data;

public interface IInventarioDatabase
{
    Task InitializeAsync();
    Task<List<Produto>> GetProdutosAsync(string? busca = null, string? categoria = null, string? local = null, bool apenasReposicao = false);
    Task<Produto?> GetProdutoByIdAsync(int id);
    Task<int> SaveProdutoAsync(Produto produto);
    Task<int> DeleteProdutoAsync(int id);
    Task<List<MovimentacaoEstoque>> GetMovimentacoesByProdutoIdAsync(int produtoId);
    Task<List<MovimentacaoEstoque>> GetAllMovimentacoesAsync(int limit = 100);
    Task<int> SaveMovimentacaoAsync(MovimentacaoEstoque movimentacao);
    Task RegistrarMovimentacaoComProdutoAsync(Produto produto, MovimentacaoEstoque movimentacao);
    Task<List<CategoriaItem>> GetCategoriasAsync();
    Task<int> SaveCategoriaAsync(CategoriaItem categoria);
    Task<List<LocalItem>> GetLocaisAsync();
    Task<int> SaveLocalAsync(LocalItem local);
    Task<ResumoInventario> GetResumoAsync();
    Task CloseAsync();
}
