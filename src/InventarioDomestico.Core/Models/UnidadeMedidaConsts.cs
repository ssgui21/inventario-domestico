namespace InventarioDomestico.Core.Models;

public static class UnidadeMedidaConsts
{
    public const string Unidade = "Unidade (un)";
    public const string Quilograma = "Quilograma (kg)";
    public const string Grama = "Grama (g)";
    public const string Litro = "Litro (L)";
    public const string Mililitro = "Mililitro (mL)";
    public const string Pacote = "Pacote (pct)";
    public const string Caixa = "Caixa (cx)";

    public static readonly IReadOnlyList<string> TodasUnidades = new[]
    {
        Unidade,
        Quilograma,
        Grama,
        Litro,
        Mililitro,
        Pacote,
        Caixa
    };

    /// <summary>
    /// Normaliza a unidade de medida para comparação case-insensitive e flexível.
    /// </summary>
    public static string Normalizar(string? unidade)
    {
        if (string.IsNullOrWhiteSpace(unidade))
            return string.Empty;

        var limpo = unidade.Trim();
        foreach (var padrao in TodasUnidades)
        {
            if (string.Equals(padrao, limpo, StringComparison.OrdinalIgnoreCase))
                return padrao;
            // Caso receba apenas a sigla ou o nome simples
            if (padrao.StartsWith(limpo, StringComparison.OrdinalIgnoreCase))
                return padrao;
        }

        return limpo;
    }

    /// <summary>
    /// Verifica se duas unidades são compatíveis para operações de estoque.
    /// </summary>
    public static bool SaoCompativeis(string? unidadeA, string? unidadeB)
    {
        var normA = Normalizar(unidadeA);
        var normB = Normalizar(unidadeB);
        return string.Equals(normA, normB, StringComparison.OrdinalIgnoreCase);
    }
}
