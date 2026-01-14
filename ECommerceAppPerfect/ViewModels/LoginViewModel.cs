using System;
using System.Windows.Input;
using ECommerceAppPerfect.Commands;
using ECommerceAppPerfect.Services;
using ECommerceAppPerfect.Stores;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA LOGINVIEWMODEL - ViewModel pentru ecranul de autentificare
    //
    // CE FACE LOGINVIEWMODEL?
    // Gestioneaza logica ecranului de login:
    // - Preia datele de autentificare de la utilizator (username, password)
    // - Valideaza datele introduse
    // - Apeleaza serviciul de autentificare
    // - Afiseaza erori daca autentificarea esueaza
    // - Navigheaza la dashboard dupa succes
    //
    // FLOW COMPLET DE LOGIN:
    //
    // 1. UTILIZATORUL INTRODUCE DATELE:
    //    - Tasteaza in TextBox-ul de username
    //    - Binding-ul actualizeaza Username in ViewModel
    //    - Similar pentru Password
    //
    // 2. UTILIZATORUL APASA BUTONUL DE LOGIN:
    //    - Button.Command e legat de LoginCommand
    //    - WPF apeleaza LoginCommand.Execute()
    //
    // 3. VIEWMODEL-UL PROCESEAZA:
    //    - Valideaza ca nu sunt campuri goale
    //    - Apeleaza IUserService.Authenticate(username, password)
    //    - Daca esueaza, seteaza ErrorMessage
    //    - Daca reuseste, seteaza CurrentUserStore.CurrentUser
    //
    // 4. NAVIGARE DUPA SUCCES:
    //    - NavigationStore.CurrentViewModel = DashboardViewModel
    //    - MainWindow afiseaza automat DashboardView
    //
    // BINDING-URI IN LOGINVIEW.XAML:
    // <TextBox Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" />
    // <PasswordBox helpers:PasswordHelper.Password="{Binding Password}" />
    // <TextBlock Text="{Binding ErrorMessage}" Foreground="Red" />
    // <Button Command="{Binding LoginCommand}" Content="Login" />
    //
    // NOTA DESPRE PASSWORD:
    // PasswordBox nu suporta binding direct (pentru securitate)
    // Folosim un helper (PasswordHelper) pentru a lega Password
    // In proiecte reale, s-ar putea folosi SecureString
    //
    // PATTERN MVVM RESPECTAT:
    // - View-ul nu are logica de autentificare
    // - ViewModel-ul nu stie de UI (nu acceseaza controale direct)
    // - Serviciul face autentificarea efectiva
    public class LoginViewModel : ViewModelBase
    {
        // SERVICII - Dependinte injectate

        // _userService - Serviciul pentru operatii cu utilizatori
        //
        // Folosit pentru autentificare: Authenticate(username, password)
        // Este o INTERFATA, nu implementare concreta
        // Permite inlocuirea cu mock in teste
        private readonly IUserService _userService;

        // STORES - Starea globala

        // _currentUserStore - Store pentru utilizatorul autentificat
        //
        // Dupa login reusit, setam CurrentUser aici
        // Restul aplicatiei poate accesa utilizatorul din acest store
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Store pentru navigare
        //
        // Dupa login reusit, navigam la dashboard
        private readonly NavigationStore _navigationStore;

        // CAMPURI PRIVATE PENTRU PROPRIETATI
        // Acestea tin valorile reale, proprietatile le expun

        // Campul pentru Username
        private string _username;

        // Campul pentru Password
        private string _password;

        // Campul pentru mesajul de eroare
        private string _errorMessage;

        // Flag pentru a indica ca autentificarea e in progres
        private bool _isLoggingIn;

        // CONSTRUCTOR - Initializeaza cu dependintele necesare
        //
        // DEPENDENCY INJECTION:
        // Toate dependintele vin prin constructor
        // LoginViewModel nu creeaza singur serviciile
        //
        // IN PRODUCTIE:
        // var loginVM = new LoginViewModel(
        //     new UserService(),        // Serviciu real cu DB
        //     currentUserStore,
        //     navigationStore
        // );
        //
        // IN TESTE:
        // var loginVM = new LoginViewModel(
        //     mockUserService,          // Mock pentru testare
        //     currentUserStore,
        //     navigationStore
        // );
        public LoginViewModel(IUserService userService, CurrentUserStore currentUserStore, NavigationStore navigationStore)
        {
            // Salvare referinte
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare comenzi
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }

        // PROPRIETATI - Expuse catre View

        // Username - Numele de utilizator introdus
        //
        // BINDING BIDIRECTIONAL:
        // View-ul poate citi valoarea (pentru afisare)
        // View-ul poate scrie valoarea (la tastare)
        //
        // IN XAML:
        // <TextBox Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" />
        //
        // UpdateSourceTrigger=PropertyChanged:
        // Actualizeaza proprietatea la fiecare caracter tastat
        // (default ar fi la pierderea focusului)
        //
        // DE CE VREM ACTUALIZARE IMEDIATA?
        // Pentru ca LoginCommand.CanExecute depinde de Username
        // Vrem ca butonul sa se activeze imediat ce avem date valide
        public string Username
        {
            get => _username;
            set
            {
                // SetProperty face:
                // 1. Verifica daca valoarea e diferita
                // 2. Seteaza _username = value
                // 3. Apeleaza OnPropertyChanged("Username")
                if (SetProperty(ref _username, value))
                {
                    // Daca username-ul s-a schimbat, stergem mesajul de eroare
                    // (utilizatorul incearca din nou, nu mai e relevant)
                    ErrorMessage = string.Empty;
                }
            }
        }

        // Password - Parola introdusa
        //
        // NOTA DESPRE SECURITATE:
        // Ideal, am folosi SecureString pentru a evita tinerea
        // parolei in memorie ca string simplu
        // Pentru simplitate, folosim string
        //
        // BINDING:
        // PasswordBox nu suporta binding direct
        // Folosim PasswordHelper pentru a face legatura
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    // Similar, stergem eroarea la schimbare
                    ErrorMessage = string.Empty;
                }
            }
        }

        // ErrorMessage - Mesajul de eroare afisat utilizatorului
        //
        // Setat cand autentificarea esueaza
        // Afisat in UI (de obicei rosu, sub campuri)
        //
        // EXEMPLU:
        // "Invalid username or password"
        // "Please enter username and password"
        // "Account is inactive"
        //
        // BINDING IN XAML:
        // <TextBlock Text="{Binding ErrorMessage}"
        //            Foreground="Red"
        //            Visibility="{Binding ErrorMessage, Converter={StaticResource StringToVis}}" />
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // IsLoggingIn - Este autentificarea in progres?
        //
        // True cand se asteapta raspunsul de la serviciu
        // Folosit pentru:
        // - Afisare spinner/loading
        // - Dezactivare buton de login (sa nu se apese de mai multe ori)
        //
        // BINDING:
        // <ProgressBar Visibility="{Binding IsLoggingIn, Converter={StaticResource BoolToVis}}" />
        // <Button IsEnabled="{Binding IsLoggingIn, Converter={StaticResource InverseBool}}" />
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        // HasError - Exista un mesaj de eroare?
        //
        // Proprietate calculata pentru simplificarea binding-urilor
        // True daca ErrorMessage nu e gol
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // COMENZI

        // LoginCommand - Comanda pentru butonul de Login
        //
        // Legata in XAML: <Button Command="{Binding LoginCommand}" />
        //
        // EXECUTE: Apeleaza ExecuteLogin
        // CANEXECUTE: Verifica daca se poate face login (campuri completate)
        public ICommand LoginCommand { get; }

        // IMPLEMENTARI COMENZI

        // ExecuteLogin - Logica de autentificare
        //
        // ACEASTA METODA FACE TOATA TREABA:
        // 1. Valideaza datele
        // 2. Apeleaza serviciul
        // 3. Gestioneaza rezultatul
        //
        // PARAMETRU: parameter - nefolosit
        private void ExecuteLogin(object parameter)
        {
            // VALIDARE PRELIMINARA
            // Verificam ca avem date (desi CanExecute ar trebui sa previna apelul)
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password";
                return;
            }

            // MARCARE IN PROGRES
            // Actualizeaza UI-ul (spinner, dezactiveaza buton)
            IsLoggingIn = true;
            ErrorMessage = string.Empty;

            try
            {
                // APELARE SERVICIU DE AUTENTIFICARE
                //
                // _userService.Authenticate face:
                // 1. Cauta utilizatorul dupa username
                // 2. Hash-uieste parola introdusa
                // 3. Compara cu hash-ul din DB
                // 4. Returneaza User sau null
                var user = _userService.Authenticate(Username, Password);

                if (user == null)
                {
                    // AUTENTIFICARE ESUATA
                    // Username sau parola gresita
                    // Nu specificam care pentru securitate (nu dam indicii atacatorilor)
                    ErrorMessage = "Invalid username or password";
                    return;
                }

                if (!user.IsActive)
                {
                    // CONT DEZACTIVAT
                    // Utilizatorul exista dar contul e dezactivat
                    ErrorMessage = "This account has been deactivated";
                    return;
                }

                // AUTENTIFICARE REUSITA!
                // Salvam utilizatorul in store
                // Aceasta declanseaza CurrentUserChanged in toata aplicatia
                _currentUserStore.Login(user);

                // NAVIGARE LA DASHBOARD
                // In functie de rol, navigam la dashboard-ul corespunzator
                NavigateToDashboard(user.UserRole);
            }
            catch (Exception ex)
            {
                // EROARE NEASTEPTATA
                // Conexiune la DB esuata, timeout, etc.
                // Afisam un mesaj generic utilizatorului
                // Logam eroarea completa pentru debugging
                ErrorMessage = "An error occurred during login. Please try again.";

                // In productie, am loga eroarea:
                // _logger.LogError(ex, "Login failed for user {Username}", Username);
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            }
            finally
            {
                // CURATARE
                // Indiferent de rezultat, nu mai suntem in progres
                IsLoggingIn = false;

                // Stergem parola din memorie (pentru securitate)
                // In realitate, cu SecureString ar fi mai sigur
                Password = string.Empty;
            }
        }

        // CanExecuteLogin - Verifica daca se poate face login
        //
        // Aceasta metoda determina daca butonul de Login e activ
        //
        // RETURNEAZA false daca:
        // - Username e gol
        // - Password e gol
        // - Autentificarea e deja in progres
        //
        // EFECT UI:
        // Butonul de Login e dezactivat (grayed out) cand returneaza false
        private bool CanExecuteLogin(object parameter)
        {
            // Nu putem face login daca:
            // 1. Nu avem username sau parola
            // 2. Suntem deja in proces de autentificare
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !IsLoggingIn;
        }

        // NavigateToDashboard - Navigheaza la dashboard-ul corespunzator rolului
        //
        // PARAMETRU: role - rolul utilizatorului ("StoreOwner", "Customer", etc.)
        //
        // FIECARE ROL ARE DASHBOARD DIFERIT:
        // - StoreOwner: StoreOwnerDashboardViewModel (statistici, quick actions)
        // - Customer: CustomerShopViewModel (magazin, produse)
        // - CustomerService: CustomerServiceViewModel (ticket-uri)
        //
        // ACEASTA METODA E SIMPLIFICATA
        // Intr-o aplicatie reala, ai folosi un factory sau DI container
        // pentru a crea ViewModels cu toate dependintele
        private void NavigateToDashboard(string role)
        {
            // In functie de rol, cream ViewModel-ul potrivit
            // si il setam ca ecran curent
            //
            // NOTA: Aceasta e o implementare simplificata
            // In realitate, ViewModels ar avea dependinte injectate
            // si ar fi create printr-un factory sau DI container
            switch (role)
            {
                case "StoreOwner":
                    // Proprietarul magazinului vede dashboard-ul de admin
                    // Cu statistici, produse, comenzi, etc.
                    // _navigationStore.CurrentViewModel = new StoreOwnerDashboardViewModel(...);
                    break;

                case "Customer":
                    // Clientul vede magazinul
                    // Poate naviga produse, adauga in cos, etc.
                    // _navigationStore.CurrentViewModel = new CustomerShopViewModel(...);
                    break;

                case "CustomerService":
                    // Agentul de suport vede ticket-urile
                    // _navigationStore.CurrentViewModel = new CustomerServiceViewModel(...);
                    break;

                default:
                    // Rol necunoscut - ramanem pe login sau afisam eroare
                    ErrorMessage = "Unknown user role";
                    break;
            }
        }

        // CLEANUP

        // Dispose - Elibereaza resursele
        //
        // Nu avem resurse de eliberat in acest ViewModel
        // Dar pastram pattern-ul pentru consistenta
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stergem parola din memorie
                _password = null;
            }

            base.Dispose(disposing);
        }
    }
}
