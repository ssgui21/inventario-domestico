using SQLite;

namespace InventarioDomestico.Core.Models;

[Table("LocaisArmazenamento")]
public class LocalItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique, NotNull]
    public string Nome { get; set; } = string.Empty;
}
