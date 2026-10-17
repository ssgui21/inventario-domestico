namespace InventarioDomestico.App.Services;

public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Sim", string cancel = "Não");
    Task<string?> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancelar", string? placeholder = null);
}
