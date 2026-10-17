using InventarioDomestico.Core.Models;

namespace InventarioDomestico.Core.Services;

public interface IInventarioService
{
    Task InicializarAsync();
    Task<List<Produto>> ObterProdutosAsync(string? busca = null, string? categoria = null, string? local = null, bool apenasReposicao = false);
    Task<Produto?> ObterProdutoPorIdAsync(int id);
    Task<Produto> CadastrarProdutoAsync(Produto produto, decimal quantidadeInicial = 0m, string? observacaoInicial = null);
    Task<Produto> AtualizarProdutoAsync(Produto produto);
    Task<bool> ExcluirProdutoAsync(int produtoId);
    Task<MovimentacaoEstoque> RegistrarEntradaAsync(int produtoId, decimal quantidade, string? unidadeMedida = null, string? observacao = null);
    Task<MovimentacaoEstoque> RegistrarSaidaAsync(int produtoId, decimal quantidade, string? unidadeMedida = null, string? observacao = null);
    Task<MovimentacaoEstoque> RegistrarAjusteAsync(int produtoId, decimal novaQuantidade, string? unidadeMedida = null, string? observacao = null);
    Task<List<MovimentacaoEstoque>> ObterMovimentacoesPorProdutoAsync(int produtoId);
    Task<List<MovimentacaoEstoque>> ObterHistoricoGeralAsync(int limite = 100);
    Task<ResumoInventario> ObterResumoAsync();
    Task<List<CategoriaItem>> ObterCategoriasAsync();
    Task<CategoriaItem> AdicionarCategoriaAsync(string nome);
    Task<List<LocalItem>> ObterLocaisAsync();
    Task<LocalItem> AdicionarLocalAsync(string nome);
    Task<bool> ValidarConsistenciaEstoqueAsync(int produtoId);
}
