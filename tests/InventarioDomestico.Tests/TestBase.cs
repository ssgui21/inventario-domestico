using InventarioDomestico.Core.Data;
using InventarioDomestico.Core.Services;

namespace InventarioDomestico.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly string DbPath;
    protected readonly IInventarioDatabase Database;
    protected readonly IInventarioService Service;

    static TestBase()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    protected TestBase()
    {
        DbPath = Path.Combine(Path.GetTempPath(), $"inventario_test_{Guid.NewGuid():N}.db");
        Database = new InventarioDatabase(DbPath);
        Service = new InventarioService(Database);
    }

    public void Dispose()
    {
        try
        {
            Database.CloseAsync().GetAwaiter().GetResult();
        }
        catch { }

        try
        {
            if (File.Exists(DbPath))
            {
                File.Delete(DbPath);
            }
        }
        catch { }
    }
}
