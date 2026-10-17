using InventarioDomestico.Core.Data;
using InventarioDomestico.Core.Models;
using InventarioDomestico.Core.Services;
using Xunit;

namespace InventarioDomestico.Tests;

public class PersistenciaSQLiteTests : TestBase
{
    [Fact]
    public async Task Persistencia_AoFecharEReabrirBanco_DadosDevemPermanecerIntegrou()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Macarrão Espaguete 500g",
            Categoria = "Alimentos",
            LocalArmazenamento = "Despensa",
            UnidadeMedida = UnidadeMedidaConsts.Pacote,
            QuantidadeMinima = 2m
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 4m);
        await Service.RegistrarSaidaAsync(salvo.Id, 1m, observacao: "Almoço de domingo");

        // Act: Fechar a conexão atual
        await Database.CloseAsync();

        // Reabrir o mesmo arquivo de banco em uma nova instância do serviço/repositório
        var novoDb = new InventarioDatabase(DbPath);
        var novoService = new InventarioService(novoDb);

        var produtos = await novoService.ObterProdutosAsync();
        var produtoRecuperado = await novoService.ObterProdutoPorIdAsync(salvo.Id);
        var historico = await novoService.ObterMovimentacoesPorProdutoAsync(salvo.Id);

        // Assert
        Assert.Single(produtos);
        Assert.NotNull(produtoRecuperado);
        Assert.Equal("Macarrão Espaguete 500g", produtoRecuperado.Nome);
        Assert.Equal(3m, produtoRecuperado.QuantidadeAtual); // 4 - 1
        Assert.Equal(2, historico.Count); // Entrada inicial + Saída

        await novoDb.CloseAsync();
    }

    [Fact]
    public async Task PesquisaEFiltros_DevemFiltrarPorNomeCategoriaELocal()
    {
        // Arrange
        await Service.CadastrarProdutoAsync(new Produto { Nome = "Arroz Branco", Categoria = "Alimentos", LocalArmazenamento = "Despensa" });
        await Service.CadastrarProdutoAsync(new Produto { Nome = "Feijão Carioca", Categoria = "Alimentos", LocalArmazenamento = "Cozinha" });
        await Service.CadastrarProdutoAsync(new Produto { Nome = "Detergente", Categoria = "Limpeza", LocalArmazenamento = "Lavanderia" });
        await Service.CadastrarProdutoAsync(new Produto { Nome = "Sabonete", Categoria = "Higiene Pessoal", LocalArmazenamento = "Banheiro" });

        // Act & Assert
        // Filtro por busca de nome
        var buscaArroz = await Service.ObterProdutosAsync(busca: "arroz");
        Assert.Single(buscaArroz);
        Assert.Equal("Arroz Branco", buscaArroz.First().Nome);

        // Filtro por categoria
        var alimentos = await Service.ObterProdutosAsync(categoria: "Alimentos");
        Assert.Equal(2, alimentos.Count);

        // Filtro por local
        var lavanderia = await Service.ObterProdutosAsync(local: "Lavanderia");
        Assert.Single(lavanderia);
        Assert.Equal("Detergente", lavanderia.First().Nome);
    }

    [Fact]
    public async Task ResumoInventario_DeveCalcularMetricasCorretamente()
    {
        // Arrange
        // Produto 1: Estoque normal
        await Service.CadastrarProdutoAsync(new Produto { Nome = "P1", QuantidadeMinima = 2m }, quantidadeInicial: 5m);
        // Produto 2: Estoque baixo (3 <= 5)
        await Service.CadastrarProdutoAsync(new Produto { Nome = "P2", QuantidadeMinima = 5m }, quantidadeInicial: 3m);
        // Produto 3: Estoque zerado com mínimo (0 <= 1)
        await Service.CadastrarProdutoAsync(new Produto { Nome = "P3", QuantidadeMinima = 1m }, quantidadeInicial: 0m);

        // Act
        var resumo = await Service.ObterResumoAsync();

        // Assert
        Assert.Equal(3, resumo.TotalProdutos);
        Assert.Equal(2, resumo.TotalItensReposicao); // P2 e P3
        Assert.Equal(1, resumo.TotalItensZerados); // P3
        Assert.True(resumo.TotalMovimentacoes >= 2);
    }
}
