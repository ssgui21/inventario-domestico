using InventarioDomestico.Core.Models;
using Xunit;

namespace InventarioDomestico.Tests;

public class EstoqueMinimoTests : TestBase
{
    [Fact]
    public async Task EstoqueMinimo_QuandoQuantidadeMenorOuIgualAoMinimo_DeveIndicarReposicao()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Sabão em Pó",
            Categoria = "Limpeza",
            QuantidadeMinima = 2m
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 2m);

        // Assert: 2 <= 2 -> Precisa de reposição
        Assert.True(salvo.PrecisaReposicao);
        Assert.Contains("ESTOQUE BAIXO", salvo.StatusEstoqueTexto);
        Assert.Contains("⚠️", salvo.IndicadorAviso);

        // Se der saída de mais 1, fica com 1 <= 2 -> Continua precisando
        await Service.RegistrarSaidaAsync(salvo.Id, 1m);
        var atualizado = await Service.ObterProdutoPorIdAsync(salvo.Id);
        Assert.True(atualizado!.PrecisaReposicao);
    }

    [Fact]
    public async Task EstoqueMinimo_QuandoQuantidadeMaiorQueOMinimo_NaoDeveIndicarReposicao()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Café Torrado e Moído",
            Categoria = "Alimentos",
            QuantidadeMinima = 1m
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 3m);

        // Assert: 3 > 1 -> Não precisa de reposição
        Assert.False(salvo.PrecisaReposicao);
        Assert.Equal("Estoque suficiente", salvo.StatusEstoqueTexto);
        Assert.Contains("✓", salvo.IndicadorAviso);
    }

    [Fact]
    public async Task EstoqueMinimo_QuandoNaoPossuiMinimoConfigurado_NaoDeveIndicarReposicaoMesmoComEstoqueBaixo()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Vinho Tinto",
            Categoria = "Bebidas",
            QuantidadeMinima = null // Sem mínimo
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 0m);

        // Assert: Requisito 4.5: Produtos sem quantidade mínima não devem ser classificados como estoque baixo
        Assert.False(salvo.PrecisaReposicao);
        Assert.Contains("sem mínimo", salvo.StatusEstoqueTexto);
    }

    [Fact]
    public async Task EstoqueMinimo_QuandoEstoqueZeradoComMinimo_DeveIndicarStatusZerado()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Leite Integral",
            Categoria = "Alimentos",
            QuantidadeMinima = 3m
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 2m);

        await Service.RegistrarSaidaAsync(salvo.Id, 2m); // Zera o estoque
        var atualizado = await Service.ObterProdutoPorIdAsync(salvo.Id);

        // Assert
        Assert.True(atualizado!.PrecisaReposicao);
        Assert.Equal(0m, atualizado.QuantidadeAtual);
        Assert.Contains("ESTOQUE ZERADO", atualizado.StatusEstoqueTexto);
    }
}
