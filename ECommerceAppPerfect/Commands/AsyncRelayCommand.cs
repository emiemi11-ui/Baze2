using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ECommerceAppPerfect.Commands
{
    // CLASA ASYNCRELAYCOMMAND - Implementare ICommand pentru operatii ASINCRONE
    //
    // DE CE AVEM NEVOIE DE VERSIUNE ASYNC?
    // Operatiile de baza de date, retea, fisiere sunt LENTE
    // Daca le rulam sincron, UI se BLOCHEAZA (freeze)
    // Utilizatorul nu poate face nimic pana se termina operatia
    //
    // SOLUTIA: ASYNC/AWAIT
    // Operatiile asincrone ruleaza "in background"
    // UI ramane responsiv - utilizatorul poate anula, naviga, etc.
    //
    // PROBLEMA:
    // ICommand.Execute() returneaza void, nu Task
    // Nu putem face direct: async void Execute() - e dangerous!
    //
    // "async void" E PERICULOS PENTRU CA:
    // 1. Exceptiile nu pot fi prinse (crash aplicatie)
    // 2. Nu stii cand se termina
    // 3. Nu poti astepta completarea
    //
    // SOLUTIA NOASTRA:
    // AsyncRelayCommand wrapeaza executia async in mod sigur:
    // 1. Prinde exceptiile
    // 2. Expune flag IsExecuting
    // 3. Previne executii simultane
    //
    // EXEMPLU FOLOSIRE:
    // public ICommand LoadProductsCommand { get; }
    //
    // public MyViewModel()
    // {
    //     LoadProductsCommand = new AsyncRelayCommand(
    //         execute: async param => await LoadProductsAsync(),
    //         canExecute: param => !IsLoading
    //     );
    // }
    //
    // private async Task LoadProductsAsync()
    // {
    //     IsLoading = true;
    //     Products = await _productService.GetAllProductsAsync();
    //     IsLoading = false;
    // }
    public class AsyncRelayCommand : ICommand
    {
        // CAMPURI PRIVATE

        // _execute - Functia asincrona de executat
        //
        // Func<object, Task> = functie care primeste object si returneaza Task
        // Task = promise-ul .NET pentru operatii asincrone
        //
        // DE CE FUNC SI NU ACTION?
        // Action nu poate returna nimic (void)
        // Func poate returna Task, ceea ce permite await
        private readonly Func<object, Task> _execute;

        // _canExecute - Conditia de executie (la fel ca RelayCommand)
        private readonly Predicate<object> _canExecute;

        // _isExecuting - Flag care indica daca comanda ruleaza
        //
        // Folosit pentru:
        // 1. A preveni executii simultane (daca IsExecuting, CanExecute = false)
        // 2. A afisa loading indicator in UI
        private bool _isExecuting;

        // PROPRIETATE PUBLICA IsExecuting
        //
        // Permite accesul la _isExecuting din exterior
        // Util pentru binding in UI (loading spinner)
        //
        // EXEMPLU XAML:
        // <ProgressBar Visibility="{Binding LoadCommand.IsExecuting, Converter={StaticResource BoolToVis}}" />
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                // Notifica ca CanExecute s-a schimbat
                // (daca executa, nu mai poate executa din nou)
                RaiseCanExecuteChanged();
            }
        }

        // CONSTRUCTORUL
        //
        // PARAMETRI:
        // - execute: Functia asincrona (Func<object, Task>)
        // - canExecute: Conditia de executie (optional)
        //
        // FOLOSIRE:
        // // Cu lambda async:
        // new AsyncRelayCommand(async param => await DoSomethingAsync())
        //
        // // Cu metoda:
        // new AsyncRelayCommand(ExecuteAsync, CanExecute)
        //
        // // Cu conditie:
        // new AsyncRelayCommand(
        //     async param => await SaveAsync(),
        //     param => !IsExecuting && HasChanges
        // )
        public AsyncRelayCommand(Func<object, Task> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // EVENIMENTUL CanExecuteChanged
        //
        // La fel ca RelayCommand - delegam la CommandManager
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // METODA CanExecute
        //
        // LOGICA SUPLIMENTARA:
        // Daca IsExecuting = true, returnam false
        // Nu permitem executii simultane ale aceleiasi comenzi
        public bool CanExecute(object parameter)
        {
            // Nu poate executa daca deja executa
            if (_isExecuting)
                return false;

            // Verificare conditie custom
            return _canExecute == null || _canExecute(parameter);
        }

        // METODA Execute - Lanseaza executia asincrona
        //
        // IMPLEMENTARE ICOMMAND
        // ICommand.Execute() returneaza void, nu Task
        // Dar noi vrem sa executam o operatie asincrona
        //
        // SOLUTIE:
        // Apelam metoda asincrona si... NU asteptam (fire and forget)
        // DAR gestionam exceptiile cu Task.ContinueWith sau try/catch
        public void Execute(object parameter)
        {
            // Apeleaza ExecuteAsync si NU asteapta
            // _ = ignora explicit Task-ul returnat (fire and forget)
            // Nu e ideal, dar e necesar pentru ICommand
            _ = ExecuteAsync(parameter);
        }

        // METODA PRIVATA ExecuteAsync - Executia efectiva
        //
        // Aceasta metoda:
        // 1. Seteaza IsExecuting = true
        // 2. Executa operatia asincrona
        // 3. Prinde orice exceptii
        // 4. Seteaza IsExecuting = false la final
        //
        // DE CE METODA SEPARATA?
        // Pentru ca Execute() returneaza void (ICommand)
        // Dar logica noastra e asincrona (returneaza Task)
        // Le separam pentru claritate
        private async Task ExecuteAsync(object parameter)
        {
            // Verificare dubla ca putem executa
            // (CanExecute s-ar fi putut schimba intre timp)
            if (!CanExecute(parameter))
                return;

            try
            {
                // INCEPEM EXECUTIA
                // Marcam ca executam (dezactiveaza butonul)
                IsExecuting = true;

                // EXECUTAM OPERATIA ASINCRONA
                // await face ca executia sa continue cand Task-ul se termina
                // UI ramane responsiv in acest timp
                await _execute(parameter);
            }
            catch (Exception ex)
            {
                // GESTIONAM EXCEPTIILE
                //
                // IN PRODUCTIE, ai vrea sa:
                // 1. Loghezi exceptia
                // 2. Afisezi un mesaj utilizatorului
                // 3. Raportezi la crash reporting service
                //
                // PENTRU SIMPLITATE:
                // Afisam exceptia in Debug output
                // In productie, inlocuieste cu logging proper
                System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                // OPTIONAL: Re-throw pentru handling la nivel superior
                // throw;
            }
            finally
            {
                // INTOTDEAUNA executam finally
                // Chiar daca a fost exceptie, trebuie sa marcam ca nu mai executam
                // Altfel, butonul ar ramane dezactivat forever
                IsExecuting = false;
            }
        }

        // METODA HELPER RaiseCanExecuteChanged
        //
        // Forteaza WPF sa reverifica CanExecute
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
