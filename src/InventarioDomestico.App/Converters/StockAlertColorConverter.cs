using System.Globalization;

namespace InventarioDomestico.App.Converters;

public class StockAlertColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool precisaReposicao && precisaReposicao)
        {
            return Color.FromArgb("#FFD6D6"); // Fundo suave avermelhado para alerta
        }
        return Color.FromArgb("#F4F4F6"); // Fundo neutro
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StockBorderColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool precisaReposicao && precisaReposicao)
        {
            return Color.FromArgb("#D32F2F"); // Borda vermelha forte
        }
        return Color.FromArgb("#E0E0E0"); // Borda discreta
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}
