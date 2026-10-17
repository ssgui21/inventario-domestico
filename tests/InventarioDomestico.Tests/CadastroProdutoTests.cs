using InventarioDomestico.Core.Exceptions;
using InventarioDomestico.Core.Models;
using Xunit;

namespace InventarioDomestico.Tests;

public class CadastroProdutoTests : TestBase
{
    [Fact]
    public async Task Cadastro_ComDadosValidos_DeveCadastrarComSucesso()
    {
        // Arrange
        var produto = new Produto
        {
            Nome = "Arroz Branco Tipo 1",
            Categoria = "Alimentos",
            UnidadeMedida = UnidadeMedidaConsts.Quilograma,
            LocalArmazenamento = "Despensa",
            QuantidadeMinima = 2m
        };

        // Act
        var salvo = await Service.CadastrarProdutoAsync(produto, quantidadeInicial: 5m);

        // Assert
        Assert.True(salvo.Id > 0);
        Assert.Equal("Arroz Branco Tipo 1", salvo.Nome);
        Assert.Equal(5m, salvo.QuantidadeAtual);
        Assert.Equal("Despensa", salvo.LocalArmazenamento);
        Assert.False(salvo.PrecisaReposicao); // 5 > 2
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Cadastro_ComNomeVazioOuNulo_DeveLancarProdutoInvalidoException(string? nomeInvalido)
    {
        // Arrange
        var produto = new Produto
        {
            Nome = nomeInvalido!,
            Categoria = "Alimentos"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ProdutoInvalidoException>(() => 
            Service.CadastrarProdutoAsync(produto));
    }

    [Fact]
    public async Task Cadastro_ComQuantidadeNegativa_DeveLancarQuantidadeInvalidaException()
    {
        // Arrange
        var produto = new Produto
        {
            Nome = "Feijão Preto",
            Categoria = "Alimentos"
        };

        // Act & Assert
        await Assert.ThrowsAsync<QuantidadeInvalidaException>(() => 
            Service.CadastrarProdutoAsync(produto, quantidadeInicial: -1.5m));
    }

    [Fact]
    public async Task Cadastro_ComQuantidadeMinimaNegativa_DeveLancarQuantidadeInvalidaException()
    {
        // Arrange
        var produto = new Produto
        {
            Nome = "Feijão Preto",
            Categoria = "Alimentos",
            QuantidadeMinima = -3m
        };

        // Act & Assert
        await Assert.ThrowsAsync<QuantidadeInvalidaException>(() => 
            Service.CadastrarProdutoAsync(produto));
    }

    [Fact]
    public async Task Cadastro_ComQuantidadeInicialPositiva_DeveCriarEntradaNoHistorico()
    {
        // Arrange
        var produto = new Produto
        {
            Nome = "Azeite de Oliva Extra Virgem",
            Categoria = "Alimentos",
            UnidadeMedida = UnidadeMedidaConsts.Mililitro
        };

        // Act
        var salvo = await Service.CadastrarProdutoAsync(produto, quantidadeInicial: 500m);
        var historico = await Service.ObterMovimentacoesPorProdutoAsync(salvo.Id);

        // Assert
        Assert.Single(historico);
        var mov = historico.First();
        Assert.Equal(TipoMovimentacao.Entrada, mov.Tipo);
        Assert.Equal(500m, mov.Quantidade);
        Assert.Equal(0m, mov.QuantidadeAnterior);
        Assert.Equal(500m, mov.QuantidadeResultante);
    }
}
