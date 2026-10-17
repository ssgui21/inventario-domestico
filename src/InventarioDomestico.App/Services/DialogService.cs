namespace InventarioDomestico.App.Services;

public class DialogService : IDialogService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Sim", string cancel = "Não")
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }

    public async Task<string?> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancelar", string? placeholder = null)
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder);
        }
        return null;
    }
}
