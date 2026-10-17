using InventarioDomestico.Core.Data;
using InventarioDomestico.Core.Exceptions;
using InventarioDomestico.Core.Models;

namespace InventarioDomestico.Core.Services;

public class InventarioService : IInventarioService
{
    private readonly IInventarioDatabase _database;

    public InventarioService(IInventarioDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task InicializarAsync()
    {
        await _database.InitializeAsync();
    }

    public async Task<List<Produto>> ObterProdutosAsync(
        string? busca = null, 
        string? categoria = null, 
        string? local = null, 
        bool apenasReposicao = false)
    {
        await InicializarAsync();
        return await _database.GetProdutosAsync(busca, categoria, local, apenasReposicao);
    }

    public async Task<Produto?> ObterProdutoPorIdAsync(int id)
    {
        await InicializarAsync();
        return await _database.GetProdutoByIdAsync(id);
    }

    public async Task<Produto> CadastrarProdutoAsync(Produto produto, decimal quantidadeInicial = 0m, string? observacaoInicial = null)
    {
        await InicializarAsync();

        if (produto == null)
            throw new ArgumentNullException(nameof(produto));

        if (string.IsNullOrWhiteSpace(produto.Nome))
            throw new ProdutoInvalidoException("O nome do produto é obrigatório e não pode ser vazio.");

        produto.Nome = produto.Nome.Trim();

        if (quantidadeInicial < 0m)
            throw new QuantidadeInvalidaException(quantidadeInicial, "A quantidade inicial não pode ser negativa.");

        if (produto.QuantidadeMinima.HasValue && produto.QuantidadeMinima.Value < 0m)
            throw new QuantidadeInvalidaException(produto.QuantidadeMinima.Value, "A quantidade mínima não pode ser negativa.");

        produto.UnidadeMedida = UnidadeMedidaConsts.Normalizar(produto.UnidadeMedida);
        if (string.IsNullOrWhiteSpace(produto.UnidadeMedida))
        {
            produto.UnidadeMedida = UnidadeMedidaConsts.Unidade;
        }

        if (string.IsNullOrWhiteSpace(produto.Categoria))
        {
            produto.Categoria = "Outros";
        }

        if (string.IsNullOrWhiteSpace(produto.LocalArmazenamento))
        {
            produto.LocalArmazenamento = "Outro";
        }

        produto.QuantidadeAtual = 0m;
        produto.DataCriacao = DateTime.Now;
        produto.DataUltimaAtualizacao = null;

        await _database.SaveProdutoAsync(produto);

        if (quantidadeInicial > 0m)
        {
            var mov = new MovimentacaoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = quantidadeInicial,
                QuantidadeAnterior = 0m,
                QuantidadeResultante = quantidadeInicial,
                UnidadeMedida = produto.UnidadeMedida,
                DataHora = DateTime.Now,
                Observacao = string.IsNullOrWhiteSpace(observacaoInicial) ? "Estoque inicial no cadastro" : observacaoInicial
            };

            produto.QuantidadeAtual = quantidadeInicial;
            await _database.RegistrarMovimentacaoComProdutoAsync(produto, mov);
        }

        return produto;
    }

    public async Task<Produto> AtualizarProdutoAsync(Produto produto)
    {
        await InicializarAsync();

        if (produto == null)
            throw new ArgumentNullException(nameof(produto));

        if (produto.Id <= 0)
            throw new ProdutoInvalidoException("Identificador de produto inválido para atualização.");

        if (string.IsNullOrWhiteSpace(produto.Nome))
            throw new ProdutoInvalidoException("O nome do produto é obrigatório.");

        var existente = await _database.GetProdutoByIdAsync(produto.Id);
        if (existente == null)
            throw new ProdutoInvalidoException($"Produto com ID {produto.Id} não foi encontrado.");

        if (produto.QuantidadeMinima.HasValue && produto.QuantidadeMinima.Value < 0m)
            throw new QuantidadeInvalidaException(produto.QuantidadeMinima.Value, "A quantidade mínima não pode ser negativa.");

        // Atualiza campos cadastrais preservando a quantidade atual (a quantidade só muda por movimentações)
        existente.Nome = produto.Nome.Trim();
        existente.Categoria = string.IsNullOrWhiteSpace(produto.Categoria) ? "Outros" : produto.Categoria.Trim();
        existente.LocalArmazenamento = string.IsNullOrWhiteSpace(produto.LocalArmazenamento) ? "Outro" : produto.LocalArmazenamento.Trim();
        existente.QuantidadeMinima = produto.QuantidadeMinima;

        var unidadeNormalizada = UnidadeMedidaConsts.Normalizar(produto.UnidadeMedida);
        if (!string.IsNullOrWhiteSpace(unidadeNormalizada))
        {
            existente.UnidadeMedida = unidadeNormalizada;
        }

        existente.DataUltimaAtualizacao = DateTime.Now;
        await _database.SaveProdutoAsync(existente);

        return existente;
    }

    public async Task<bool> ExcluirProdutoAsync(int produtoId)
    {
        await InicializarAsync();
        var existente = await _database.GetProdutoByIdAsync(produtoId);
        if (existente == null)
            return false;

        var rows = await _database.DeleteProdutoAsync(produtoId);
        return rows > 0;
    }

    public async Task<MovimentacaoEstoque> RegistrarEntradaAsync(
        int produtoId, 
        decimal quantidade, 
        string? unidadeMedida = null, 
        string? observacao = null)
    {
        await InicializarAsync();

        if (quantidade <= 0m)
            throw new QuantidadeInvalidaException(quantidade, "A quantidade de entrada deve ser estritamente maior que zero.");

        var produto = await _database.GetProdutoByIdAsync(produtoId);
        if (produto == null)
            throw new ProdutoInvalidoException($"Produto com ID {produtoId} não encontrado.");

        if (!string.IsNullOrWhiteSpace(unidadeMedida) && !UnidadeMedidaConsts.SaoCompativeis(produto.UnidadeMedida, unidadeMedida))
        {
            throw new UnidadeIncompativelException(produto.UnidadeMedida, unidadeMedida);
        }

        var qtdAnterior = produto.QuantidadeAtual;
        var qtdResultante = qtdAnterior + quantidade;

        var movimentacao = new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = TipoMovimentacao.Entrada,
            Quantidade = quantidade,
            QuantidadeAnterior = qtdAnterior,
            QuantidadeResultante = qtdResultante,
            UnidadeMedida = produto.UnidadeMedida,
            DataHora = DateTime.Now,
            Observacao = observacao?.Trim()
        };

        produto.QuantidadeAtual = qtdResultante;
        await _database.RegistrarMovimentacaoComProdutoAsync(produto, movimentacao);

        return movimentacao;
    }

    public async Task<MovimentacaoEstoque> RegistrarSaidaAsync(
        int produtoId, 
        decimal quantidade, 
        string? unidadeMedida = null, 
        string? observacao = null)
    {
        await InicializarAsync();

        if (quantidade <= 0m)
            throw new QuantidadeInvalidaException(quantidade, "A quantidade de saída deve ser estritamente maior que zero.");

        var produto = await _database.GetProdutoByIdAsync(produtoId);
        if (produto == null)
            throw new ProdutoInvalidoException($"Produto com ID {produtoId} não encontrado.");

        if (!string.IsNullOrWhiteSpace(unidadeMedida) && !UnidadeMedidaConsts.SaoCompativeis(produto.UnidadeMedida, unidadeMedida))
        {
            throw new UnidadeIncompativelException(produto.UnidadeMedida, unidadeMedida);
        }

        if (produto.QuantidadeAtual < quantidade)
        {
            throw new EstoqueInsuficienteException(produto.QuantidadeAtual, quantidade);
        }

        var qtdAnterior = produto.QuantidadeAtual;
        var qtdResultante = qtdAnterior - quantidade;

        if (qtdResultante < 0m)
        {
            throw new OperacaoInvalidaException("O estoque resultante não pode ficar negativo.");
        }

        var movimentacao = new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = TipoMovimentacao.Saida,
            Quantidade = quantidade,
            QuantidadeAnterior = qtdAnterior,
            QuantidadeResultante = qtdResultante,
            UnidadeMedida = produto.UnidadeMedida,
            DataHora = DateTime.Now,
            Observacao = observacao?.Trim()
        };

        produto.QuantidadeAtual = qtdResultante;
        await _database.RegistrarMovimentacaoComProdutoAsync(produto, movimentacao);

        return movimentacao;
    }

    public async Task<MovimentacaoEstoque> RegistrarAjusteAsync(
        int produtoId, 
        decimal novaQuantidade, 
        string? unidadeMedida = null, 
        string? observacao = null)
    {
        await InicializarAsync();

        if (novaQuantidade < 0m)
            throw new QuantidadeInvalidaException(novaQuantidade, "A nova quantidade do estoque não pode ser negativa.");

        var produto = await _database.GetProdutoByIdAsync(produtoId);
        if (produto == null)
            throw new ProdutoInvalidoException($"Produto com ID {produtoId} não encontrado.");

        if (!string.IsNullOrWhiteSpace(unidadeMedida) && !UnidadeMedidaConsts.SaoCompativeis(produto.UnidadeMedida, unidadeMedida))
        {
            throw new UnidadeIncompativelException(produto.UnidadeMedida, unidadeMedida);
        }

        var qtdAnterior = produto.QuantidadeAtual;
        var delta = Math.Abs(novaQuantidade - qtdAnterior);

        var mov = new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = TipoMovimentacao.Ajuste,
            Quantidade = delta,
            QuantidadeAnterior = qtdAnterior,
            QuantidadeResultante = novaQuantidade,
            UnidadeMedida = produto.UnidadeMedida,
            DataHora = DateTime.Now,
            Observacao = string.IsNullOrWhiteSpace(observacao) 
                ? $"Ajuste de saldo de {qtdAnterior:0.##} para {novaQuantidade:0.##}" 
                : observacao.Trim()
        };

        produto.QuantidadeAtual = novaQuantidade;
        await _database.RegistrarMovimentacaoComProdutoAsync(produto, mov);

        return mov;
    }

    public async Task<List<MovimentacaoEstoque>> ObterMovimentacoesPorProdutoAsync(int produtoId)
    {
        await InicializarAsync();
        return await _database.GetMovimentacoesByProdutoIdAsync(produtoId);
    }

    public async Task<List<MovimentacaoEstoque>> ObterHistoricoGeralAsync(int limite = 100)
    {
        await InicializarAsync();
        return await _database.GetAllMovimentacoesAsync(limite);
    }

    public async Task<ResumoInventario> ObterResumoAsync()
    {
        await InicializarAsync();
        return await _database.GetResumoAsync();
    }

    public async Task<List<CategoriaItem>> ObterCategoriasAsync()
    {
        await InicializarAsync();
        return await _database.GetCategoriasAsync();
    }

    public async Task<CategoriaItem> AdicionarCategoriaAsync(string nome)
    {
        await InicializarAsync();
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome da categoria não pode ser vazio.", nameof(nome));

        var categoria = new CategoriaItem { Nome = nome.Trim() };
        var id = await _database.SaveCategoriaAsync(categoria);
        categoria.Id = id;
        return categoria;
    }

    public async Task<List<LocalItem>> ObterLocaisAsync()
    {
        await InicializarAsync();
        return await _database.GetLocaisAsync();
    }

    public async Task<LocalItem> AdicionarLocalAsync(string nome)
    {
        await InicializarAsync();
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do local não pode ser vazio.", nameof(nome));

        var local = new LocalItem { Nome = nome.Trim() };
        var id = await _database.SaveLocalAsync(local);
        local.Id = id;
        return local;
    }

    public async Task<bool> ValidarConsistenciaEstoqueAsync(int produtoId)
    {
        await InicializarAsync();
        var produto = await _database.GetProdutoByIdAsync(produtoId);
        if (produto == null) return false;

        var movs = await _database.GetMovimentacoesByProdutoIdAsync(produtoId);
        if (movs.Count == 0)
        {
            return produto.QuantidadeAtual == 0m;
        }

        // Ordenadas cronologicamente
        var cronologico = movs.OrderBy(m => m.DataHora).ThenBy(m => m.Id).ToList();

        decimal saldoCalculado = 0m;
        foreach (var m in cronologico)
        {
            if (m.Tipo == TipoMovimentacao.Entrada)
            {
                saldoCalculado += m.Quantidade;
            }
            else if (m.Tipo == TipoMovimentacao.Saida)
            {
                saldoCalculado -= m.Quantidade;
            }
            else if (m.Tipo == TipoMovimentacao.Ajuste)
            {
                saldoCalculado = m.QuantidadeResultante;
            }
        }

        return saldoCalculado == produto.QuantidadeAtual;
    }
}
