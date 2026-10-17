using SQLite;

namespace InventarioDomestico.Core.Models;

[Table("Categorias")]
public class CategoriaItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique, NotNull]
    public string Nome { get; set; } = string.Empty;
}
