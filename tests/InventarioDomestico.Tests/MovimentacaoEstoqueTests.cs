using InventarioDomestico.Core.Exceptions;
using InventarioDomestico.Core.Models;
using Xunit;

namespace InventarioDomestico.Tests;

public class MovimentacaoEstoqueTests : TestBase
{
    private async Task<Produto> CriarProdutoBaseAsync(decimal qtdInicial = 10m, string unidade = UnidadeMedidaConsts.Unidade)
    {
        var produto = new Produto
        {
            Nome = "Detergente Neutro",
            Categoria = "Limpeza",
            LocalArmazenamento = "Lavanderia",
            UnidadeMedida = unidade,
            QuantidadeMinima = 2m
        };
        return await Service.CadastrarProdutoAsync(produto, quantidadeInicial: qtdInicial);
    }

    [Fact]
    public async Task Entrada_ComQuantidadeValida_DeveAumentarEstoqueEGravarHistorico()
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 5m);

        // Act
        var mov = await Service.RegistrarEntradaAsync(p.Id, 3m, observacao: "Compra do mês");
        var produtoAtualizado = await Service.ObterProdutoPorIdAsync(p.Id);

        // Assert
        Assert.NotNull(produtoAtualizado);
        Assert.Equal(8m, produtoAtualizado.QuantidadeAtual);
        Assert.Equal(TipoMovimentacao.Entrada, mov.Tipo);
        Assert.Equal(3m, mov.Quantidade);
        Assert.Equal(5m, mov.QuantidadeAnterior);
        Assert.Equal(8m, mov.QuantidadeResultante);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public async Task Entrada_ComQuantidadeZeroOuNegativa_DeveLancarQuantidadeInvalidaException(decimal qtdInvalida)
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 5m);

        // Act & Assert
        await Assert.ThrowsAsync<QuantidadeInvalidaException>(() =>
            Service.RegistrarEntradaAsync(p.Id, qtdInvalida));
    }

    [Fact]
    public async Task Saida_ComQuantidadeValida_DeveDiminuirEstoqueEGravarHistorico()
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 10m);

        // Act
        var mov = await Service.RegistrarSaidaAsync(p.Id, 4m, observacao: "Uso na cozinha");
        var produtoAtualizado = await Service.ObterProdutoPorIdAsync(p.Id);

        // Assert
        Assert.NotNull(produtoAtualizado);
        Assert.Equal(6m, produtoAtualizado.QuantidadeAtual);
        Assert.Equal(TipoMovimentacao.Saida, mov.Tipo);
        Assert.Equal(4m, mov.Quantidade);
        Assert.Equal(10m, mov.QuantidadeAnterior);
        Assert.Equal(6m, mov.QuantidadeResultante);
    }

    [Fact]
    public async Task Saida_ComQuantidadeSuperiorAoDisponivel_DeveLancarEstoqueInsuficienteException()
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 2m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EstoqueInsuficienteException>(() =>
            Service.RegistrarSaidaAsync(p.Id, 2.5m));

        Assert.Equal(2m, ex.QuantidadeAtual);
        Assert.Equal(2.5m, ex.QuantidadeSolicitada);

        // Verifica que o estoque não foi alterado
        var pPos = await Service.ObterProdutoPorIdAsync(p.Id);
        Assert.Equal(2m, pPos!.QuantidadeAtual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Saida_ComQuantidadeZeroOuNegativa_DeveLancarQuantidadeInvalidaException(decimal qtdInvalida)
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 5m);

        // Act & Assert
        await Assert.ThrowsAsync<QuantidadeInvalidaException>(() =>
            Service.RegistrarSaidaAsync(p.Id, qtdInvalida));
    }

    [Fact]
    public async Task Movimentacao_ComUnidadeIncompativel_DeveLancarUnidadeIncompativelException()
    {
        // Arrange (produto cadastrado com Quilograma)
        var p = await CriarProdutoBaseAsync(qtdInicial: 5m, unidade: UnidadeMedidaConsts.Quilograma);

        // Act & Assert (tentar dar entrada com Litro)
        await Assert.ThrowsAsync<UnidadeIncompativelException>(() =>
            Service.RegistrarEntradaAsync(p.Id, 2m, unidadeMedida: UnidadeMedidaConsts.Litro));

        // Act & Assert (tentar dar saída com Unidade)
        await Assert.ThrowsAsync<UnidadeIncompativelException>(() =>
            Service.RegistrarSaidaAsync(p.Id, 1m, unidadeMedida: UnidadeMedidaConsts.Unidade));
    }

    [Fact]
    public async Task Movimentacao_ComUnidadesEquivalentes_DeveAceitar()
    {
        // Arrange (produto cadastrado com "Litro (L)")
        var p = await CriarProdutoBaseAsync(qtdInicial: 3m, unidade: UnidadeMedidaConsts.Litro);

        // Act - usar "Litro" ou "L" deve ser compatível
        var mov = await Service.RegistrarEntradaAsync(p.Id, 1.5m, unidadeMedida: "Litro");

        // Assert
        Assert.Equal(4.5m, mov.QuantidadeResultante);
    }

    [Fact]
    public async Task Ajuste_ParaCima_DeveAtualizarEstoqueERegistrarHistorico()
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 4m);

        // Act: ajuste para 7m (aumento de 3m)
        var mov = await Service.RegistrarAjusteAsync(p.Id, 7m, observacao: "Contagem física do armário");
        var pAtualizado = await Service.ObterProdutoPorIdAsync(p.Id);

        // Assert
        Assert.NotNull(pAtualizado);
        Assert.Equal(7m, pAtualizado.QuantidadeAtual);
        Assert.Equal(TipoMovimentacao.Ajuste, mov.Tipo);
        Assert.Equal(3m, mov.Quantidade); // delta
        Assert.Equal(4m, mov.QuantidadeAnterior);
        Assert.Equal(7m, mov.QuantidadeResultante);
    }

    [Fact]
    public async Task Ajuste_ParaBaixo_DeveAtualizarEstoqueERegistrarHistorico()
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 10m);

        // Act: ajuste para 6m (redução de 4m)
        var mov = await Service.RegistrarAjusteAsync(p.Id, 6m, observacao: "Itens danificados removidos");
        var pAtualizado = await Service.ObterProdutoPorIdAsync(p.Id);

        // Assert
        Assert.NotNull(pAtualizado);
        Assert.Equal(6m, pAtualizado.QuantidadeAtual);
        Assert.Equal(4m, mov.Quantidade); // delta absoluto
        Assert.Equal(10m, mov.QuantidadeAnterior);
        Assert.Equal(6m, mov.QuantidadeResultante);
    }

    [Fact]
    public async Task Ajuste_ParaValorNegativo_DeveLancarQuantidadeInvalidaException()
    {
        // Arrange
        var p = await CriarProdutoBaseAsync(qtdInicial: 5m);

        // Act & Assert
        await Assert.ThrowsAsync<QuantidadeInvalidaException>(() =>
            Service.RegistrarAjusteAsync(p.Id, -2m));
    }
}
