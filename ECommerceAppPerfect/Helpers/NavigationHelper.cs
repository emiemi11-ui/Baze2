using System;
using System.Windows;

namespace ECommerceAppPerfect.Helpers
{
    // CLASA NAVIGATIONHELPER - Utilitare pentru navigare si UI
    //
    // CE CONTINE ACEASTA CLASA?
    // Metode helper pentru operatii comune de UI:
    // 1. Afisare mesaje (MessageBox wrapper)
    // 2. Confirmare actiuni
    // 3. Navigare intre ferestre
    // 4. UI utilities
    //
    // DE CE WRAPPER PESTE MESSAGEBOX?
    // 1. TESTABILITATE: Poti mock-ui MessageBox in teste
    // 2. CONSISTENTA: Acelasi stil de mesaje peste tot
    // 3. FLEXIBILITATE: Poti schimba implementarea (MessageBox -> Toast)
    //
    // NOTA MVVM:
    // In MVVM pur, nu ai vrea sa apelezi MessageBox din ViewModel
    // Ai folosi un IDialogService injectat
    // Pentru simplitate, folosim metode statice aici
    public static class NavigationHelper
    {
        // METODA ShowMessage - Afiseaza un mesaj informativ
        //
        // PARAMETRI:
        // - message: Textul mesajului
        // - title: Titlul ferestrei (optional)
        //
        // FOLOSIRE:
        // NavigationHelper.ShowMessage("Operation completed successfully!");
        public static void ShowMessage(string message, string title = "Information")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // METODA ShowError - Afiseaza un mesaj de eroare
        //
        // Iconita rosie pentru a indica problema
        //
        // FOLOSIRE:
        // NavigationHelper.ShowError("Failed to save product.");
        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        // METODA ShowWarning - Afiseaza un avertisment
        //
        // Iconita galbena pentru atentie
        //
        // FOLOSIRE:
        // NavigationHelper.ShowWarning("Stock is running low!");
        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        // METODA ShowSuccess - Afiseaza un mesaj de succes
        //
        // Iconita de informare (verde logic, dar WPF nu are iconita verde standard)
        //
        // FOLOSIRE:
        // NavigationHelper.ShowSuccess("Product saved successfully!");
        public static void ShowSuccess(string message, string title = "Success")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // METODA Confirm - Cere confirmare utilizatorului
        //
        // PARAMETRI:
        // - message: Intrebarea
        // - title: Titlul (optional)
        //
        // RETURNEAZA:
        // - true daca utilizatorul apasa Yes
        // - false daca apasa No
        //
        // FOLOSIRE:
        // if (NavigationHelper.Confirm("Are you sure you want to delete this product?"))
        // {
        //     DeleteProduct();
        // }
        public static bool Confirm(string message, string title = "Confirmation")
        {
            var result = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            return result == MessageBoxResult.Yes;
        }

        // METODA ConfirmDelete - Confirmare specifica pentru stergere
        //
        // Adauga mesaj standard pentru stergere
        //
        // PARAMETRU: itemName - Ce se sterge ("product", "order", etc.)
        //
        // FOLOSIRE:
        // if (NavigationHelper.ConfirmDelete("product"))
        // {
        //     // Sterge
        // }
        public static bool ConfirmDelete(string itemName)
        {
            return Confirm(
                $"Are you sure you want to delete this {itemName}?\n\nThis action cannot be undone.",
                "Confirm Delete"
            );
        }

        // METODA ConfirmLogout - Confirmare pentru logout
        public static bool ConfirmLogout()
        {
            return Confirm(
                "Are you sure you want to log out?",
                "Confirm Logout"
            );
        }

        // METODA ConfirmExit - Confirmare pentru inchidere aplicatie
        public static bool ConfirmExit()
        {
            return Confirm(
                "Are you sure you want to exit the application?",
                "Confirm Exit"
            );
        }

        // METODA PromptInput - Cere input text de la utilizator
        //
        // NOTA: WPF nu are InputBox built-in
        // Aceasta e o implementare simpla cu MessageBox
        // Pentru input real, ai folosi o fereastra custom
        //
        // RETURNEAZA: null daca s-a anulat, string-ul daca s-a confirmat
        //
        // PENTRU SIMPLITATE, nu implementam input dialog complet
        // In practica, ai crea o fereastra separata

        // METODA SetBusyCursor - Seteaza cursorul la "busy"
        //
        // FOLOSIRE:
        // try
        // {
        //     NavigationHelper.SetBusyCursor();
        //     await LongOperation();
        // }
        // finally
        // {
        //     NavigationHelper.ResetCursor();
        // }
        public static void SetBusyCursor()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            });
        }

        // METODA ResetCursor - Reseteaza cursorul la normal
        public static void ResetCursor()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Input.Mouse.OverrideCursor = null;
            });
        }

        // METODA InvokeOnUIThread - Executa pe UI thread
        //
        // DE CE E NEVOIE?
        // Operatiile UI trebuie executate pe UI thread
        // Daca esti pe alt thread (background), trebuie sa "sari" pe UI thread
        //
        // FOLOSIRE:
        // // Pe background thread:
        // NavigationHelper.InvokeOnUIThread(() =>
        // {
        //     StatusText = "Completed!";
        // });
        public static void InvokeOnUIThread(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                // Suntem deja pe UI thread
                action();
            }
            else
            {
                // Nu suntem pe UI thread, invocam
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        // METODA ASYNC InvokeOnUIThreadAsync - Versiune async
        //
        // La fel ca InvokeOnUIThread, dar async
        // Nu blocheaza thread-ul curent
        public static async System.Threading.Tasks.Task InvokeOnUIThreadAsync(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(action);
            }
        }

        // METODA GetMainWindow - Obtine fereastra principala
        //
        // Util pentru a centra dialoguri fata de fereastra principala
        public static Window GetMainWindow()
        {
            return Application.Current?.MainWindow;
        }

        // METODA CopyToClipboard - Copiaza text in clipboard
        //
        // PARAMETRU: text - Textul de copiat
        //
        // FOLOSIRE:
        // NavigationHelper.CopyToClipboard(OrderNumber);
        // NavigationHelper.ShowMessage("Order number copied to clipboard!");
        public static void CopyToClipboard(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                InvokeOnUIThread(() =>
                {
                    Clipboard.SetText(text);
                });
            }
        }

        // METODA OpenUrl - Deschide un URL in browser
        //
        // PARAMETRU: url - URL-ul de deschis
        //
        // FOLOSIRE:
        // NavigationHelper.OpenUrl("https://example.com/help");
        public static void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                // Process.Start deschide URL-ul in browser-ul default
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError($"Could not open URL: {ex.Message}");
            }
        }
    }
}
