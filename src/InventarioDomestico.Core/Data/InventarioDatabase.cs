using SQLite;
using InventarioDomestico.Core.Models;

namespace InventarioDomestico.Core.Data;

public class InventarioDatabase : IInventarioDatabase
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _connection;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public InventarioDatabase(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
    }

    public async Task InitializeAsync()
    {
        if (_initialized && _connection != null)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized && _connection != null)
                return;

            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
            _connection = new SQLiteAsyncConnection(_dbPath, flags);

            // Enable WAL mode for performance & reliability
            await _connection.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL;");

            await _connection.CreateTableAsync<Produto>();
            await _connection.CreateTableAsync<MovimentacaoEstoque>();
            await _connection.CreateTableAsync<CategoriaItem>();
            await _connection.CreateTableAsync<LocalItem>();

            await SeedDefaultDataAsync();

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SeedDefaultDataAsync()
    {
        if (_connection == null) return;

        var totalCategorias = await _connection.Table<CategoriaItem>().CountAsync();
        if (totalCategorias == 0)
        {
            var defaultCategorias = new[]
            {
                new CategoriaItem { Nome = "Alimentos" },
                new CategoriaItem { Nome = "Bebidas" },
                new CategoriaItem { Nome = "Limpeza" },
                new CategoriaItem { Nome = "Higiene Pessoal" },
                new CategoriaItem { Nome = "Outros" }
            };
            await _connection.InsertAllAsync(defaultCategorias);
        }

        var totalLocais = await _connection.Table<LocalItem>().CountAsync();
        if (totalLocais == 0)
        {
            var defaultLocais = new[]
            {
                new LocalItem { Nome = "Cozinha" },
                new LocalItem { Nome = "Geladeira" },
                new LocalItem { Nome = "Despensa" },
                new LocalItem { Nome = "Banheiro" },
                new LocalItem { Nome = "Lavanderia" },
                new LocalItem { Nome = "Outro" }
            };
            await _connection.InsertAllAsync(defaultLocais);
        }
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (!_initialized || _connection == null)
        {
            await InitializeAsync();
        }
        return _connection!;
    }

    public async Task<List<Produto>> GetProdutosAsync(
        string? busca = null, 
        string? categoria = null, 
        string? local = null, 
        bool apenasReposicao = false)
    {
        var conn = await GetConnectionAsync();
        var query = conn.Table<Produto>();

        var produtos = await query.ToListAsync();

        IEnumerable<Produto> filtered = produtos;

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim().ToLowerInvariant();
            filtered = filtered.Where(p => p.Nome.ToLowerInvariant().Contains(b));
        }

        if (!string.IsNullOrWhiteSpace(categoria) && !categoria.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(p => string.Equals(p.Categoria, categoria, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(local) && !local.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(p => string.Equals(p.LocalArmazenamento, local, StringComparison.OrdinalIgnoreCase));
        }

        if (apenasReposicao)
        {
            filtered = filtered.Where(p => p.PrecisaReposicao);
        }

        return filtered.OrderBy(p => p.Nome).ToList();
    }

    public async Task<Produto?> GetProdutoByIdAsync(int id)
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<Produto>().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> SaveProdutoAsync(Produto produto)
    {
        var conn = await GetConnectionAsync();
        if (produto.Id != 0)
        {
            produto.DataUltimaAtualizacao = DateTime.Now;
            return await conn.UpdateAsync(produto);
        }
        else
        {
            produto.DataCriacao = DateTime.Now;
            return await conn.InsertAsync(produto);
        }
    }

    public async Task<int> DeleteProdutoAsync(int id)
    {
        var conn = await GetConnectionAsync();
        return await conn.DeleteAsync<Produto>(id);
    }

    public async Task<List<MovimentacaoEstoque>> GetMovimentacoesByProdutoIdAsync(int produtoId)
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<MovimentacaoEstoque>()
            .Where(m => m.ProdutoId == produtoId)
            .OrderByDescending(m => m.DataHora)
            .ToListAsync();
    }

    public async Task<List<MovimentacaoEstoque>> GetAllMovimentacoesAsync(int limit = 100)
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<MovimentacaoEstoque>()
            .OrderByDescending(m => m.DataHora)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> SaveMovimentacaoAsync(MovimentacaoEstoque movimentacao)
    {
        var conn = await GetConnectionAsync();
        if (movimentacao.Id != 0)
        {
            return await conn.UpdateAsync(movimentacao);
        }
        return await conn.InsertAsync(movimentacao);
    }

    public async Task RegistrarMovimentacaoComProdutoAsync(Produto produto, MovimentacaoEstoque movimentacao)
    {
        var conn = await GetConnectionAsync();
        await conn.RunInTransactionAsync(syncConn =>
        {
            produto.DataUltimaAtualizacao = DateTime.Now;
            syncConn.Update(produto);
            syncConn.Insert(movimentacao);
        });
    }

    public async Task<List<CategoriaItem>> GetCategoriasAsync()
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<CategoriaItem>().OrderBy(c => c.Nome).ToListAsync();
    }

    public async Task<int> SaveCategoriaAsync(CategoriaItem categoria)
    {
        var conn = await GetConnectionAsync();
        var existente = await conn.Table<CategoriaItem>()
            .FirstOrDefaultAsync(c => c.Nome.ToLower() == categoria.Nome.ToLower());
        if (existente != null)
            return existente.Id;

        return await conn.InsertAsync(categoria);
    }

    public async Task<List<LocalItem>> GetLocaisAsync()
    {
        var conn = await GetConnectionAsync();
        return await conn.Table<LocalItem>().OrderBy(l => l.Nome).ToListAsync();
    }

    public async Task<int> SaveLocalAsync(LocalItem local)
    {
        var conn = await GetConnectionAsync();
        var existente = await conn.Table<LocalItem>()
            .FirstOrDefaultAsync(l => l.Nome.ToLower() == local.Nome.ToLower());
        if (existente != null)
            return existente.Id;

        return await conn.InsertAsync(local);
    }

    public async Task<ResumoInventario> GetResumoAsync()
    {
        var conn = await GetConnectionAsync();
        var produtos = await conn.Table<Produto>().ToListAsync();
        var totalMovimentacoes = await conn.Table<MovimentacaoEstoque>().CountAsync();
        var totalCategorias = await conn.Table<CategoriaItem>().CountAsync();
        var totalLocais = await conn.Table<LocalItem>().CountAsync();

        return new ResumoInventario
        {
            TotalProdutos = produtos.Count,
            TotalItensReposicao = produtos.Count(p => p.PrecisaReposicao),
            TotalItensZerados = produtos.Count(p => p.QuantidadeAtual <= 0),
            TotalMovimentacoes = totalMovimentacoes,
            TotalCategorias = totalCategorias,
            TotalLocais = totalLocais
        };
    }

    public async Task CloseAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection = null;
            _initialized = false;
        }
    }
}
