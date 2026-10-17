using InventarioDomestico.Core.Models;
using Xunit;

namespace InventarioDomestico.Tests;

public class HistoricoEConsistenciaTests : TestBase
{
    [Fact]
    public async Task Historico_MultiplasOperacoes_DevePreservarTodasCronologicamente()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Água Mineral 500ml",
            Categoria = "Bebidas",
            UnidadeMedida = UnidadeMedidaConsts.Unidade
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 12m, observacaoInicial: "Fardo inicial");

        // Act
        await Service.RegistrarSaidaAsync(salvo.Id, 2m, observacao: "Consumo 1");
        await Service.RegistrarSaidaAsync(salvo.Id, 3m, observacao: "Consumo 2");
        await Service.RegistrarEntradaAsync(salvo.Id, 6m, observacao: "Reposição");
        await Service.RegistrarAjusteAsync(salvo.Id, 15m, observacao: "Contagem semanal");

        var historico = await Service.ObterMovimentacoesPorProdutoAsync(salvo.Id);

        // Assert: 1 cadastro + 2 saídas + 1 entrada + 1 ajuste = 5 movimentações
        Assert.Equal(5, historico.Count);

        // Verifica que nenhuma movimentação foi perdida e que os saldos intermediários estão corretos
        var pAtualizado = await Service.ObterProdutoPorIdAsync(salvo.Id);
        Assert.Equal(15m, pAtualizado!.QuantidadeAtual);

        // Validar consistência matemática
        var consistente = await Service.ValidarConsistenciaEstoqueAsync(salvo.Id);
        Assert.True(consistente);
    }

    [Fact]
    public async Task Consistencia_SaldoAtualDeveBaterComSomaMovimentacoes()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Açúcar Refinado",
            Categoria = "Alimentos",
            UnidadeMedida = UnidadeMedidaConsts.Quilograma
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 0m);

        // 3 Entradas e 2 Saídas
        await Service.RegistrarEntradaAsync(salvo.Id, 5m);
        await Service.RegistrarSaidaAsync(salvo.Id, 1.5m);
        await Service.RegistrarEntradaAsync(salvo.Id, 2m);
        await Service.RegistrarSaidaAsync(salvo.Id, 0.5m);
        await Service.RegistrarEntradaAsync(salvo.Id, 1m);

        // Saldo esperado: 0 + 5 - 1.5 + 2 - 0.5 + 1 = 6.0 kg
        var pAtualizado = await Service.ObterProdutoPorIdAsync(salvo.Id);
        Assert.Equal(6.0m, pAtualizado!.QuantidadeAtual);

        var ehConsistente = await Service.ValidarConsistenciaEstoqueAsync(salvo.Id);
        Assert.True(ehConsistente);
    }

    [Fact]
    public async Task Edicao_AtualizarNomeECategoria_NaoDeveApagarMovimentacoesNemAlterarEstoque()
    {
        // Arrange
        var p = new Produto
        {
            Nome = "Sabonete Líquido",
            Categoria = "Higiene Pessoal",
            UnidadeMedida = UnidadeMedidaConsts.Mililitro
        };
        var salvo = await Service.CadastrarProdutoAsync(p, quantidadeInicial: 250m);
        await Service.RegistrarSaidaAsync(salvo.Id, 50m);

        // Act - Edição cadastral
        salvo.Nome = "Sabonete Líquido Erva Doce";
        salvo.Categoria = "Higiene Pessoal";
        salvo.LocalArmazenamento = "Banheiro Suíte";
        salvo.QuantidadeMinima = 100m;

        await Service.AtualizarProdutoAsync(salvo);

        // Assert
        var pEditado = await Service.ObterProdutoPorIdAsync(salvo.Id);
        Assert.NotNull(pEditado);
        Assert.Equal("Sabonete Líquido Erva Doce", pEditado.Nome);
        Assert.Equal("Banheiro Suíte", pEditado.LocalArmazenamento);
        Assert.Equal(200m, pEditado.QuantidadeAtual); // 250 - 50 mantido

        var historico = await Service.ObterMovimentacoesPorProdutoAsync(salvo.Id);
        Assert.Equal(2, historico.Count); // 2 movimentações originais intactas
    }
}
