using System;
using System.Windows.Input;
using ECommerceAppPerfect.Commands;
using ECommerceAppPerfect.Stores;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA MAINVIEWMODEL - ViewModel-ul principal al aplicatiei
    //
    // CE ESTE MAINVIEWMODEL?
    // Este ViewModel-ul pentru fereastra principala (MainWindow)
    // Coordoneaza intreaga aplicatie:
    // - Tine referinta la utilizatorul curent (CurrentUserStore)
    // - Gestioneaza navigarea intre ecrane (NavigationStore)
    // - Expune comenzi pentru actiuni globale (Logout, etc.)
    //
    // ROLUL IN ARHITECTURA APLICATIEI:
    // MainViewModel este CENTRUL aplicatiei. MainWindow.xaml are
    // ca DataContext o instanta de MainViewModel. Toate celelalte
    // ViewModels sunt "copii" afisati prin NavigationStore
    //
    // CUM FUNCTIONEAZA AFISAREA ECRANELOR?
    //
    // 1. MainWindow are un ContentControl:
    //    <ContentControl Content="{Binding CurrentViewModel}" />
    //
    // 2. ContentControl foloseste DataTemplates pentru a afisa View-ul corect:
    //    <DataTemplate DataType="{x:Type vm:LoginViewModel}">
    //        <views:LoginView />
    //    </DataTemplate>
    //
    // 3. Cand NavigationStore.CurrentViewModel se schimba,
    //    MainViewModel notifica UI-ul prin PropertyChanged
    //
    // 4. UI-ul (ContentControl) afiseaza automat View-ul corespunzator
    //
    // FLOW EXEMPLU - LOGIN:
    // 1. App porneste -> MainViewModel creeaza LoginViewModel
    // 2. NavigationStore.CurrentViewModel = LoginViewModel
    // 3. MainWindow afiseaza LoginView (prin DataTemplate)
    // 4. User se logheaza cu succes
    // 5. LoginViewModel seteaza NavigationStore.CurrentViewModel = DashboardViewModel
    // 6. MainWindow afiseaza automat DashboardView
    //
    // PATTERN MVVM - COORDINARE:
    // MainViewModel nu contine logica de business
    // El doar coordoneaza intre diferitele parti ale aplicatiei
    // Este un "orchestrator" sau "conductor"
    public class MainViewModel : ViewModelBase
    {
        // STORES - Referinte la starea globala a aplicatiei

        // _currentUserStore - Tine utilizatorul autentificat curent
        //
        // Acest store e partajat intre toate ViewModels
        // Cand un user se logheaza, CurrentUserStore.CurrentUser se seteaza
        // MainViewModel asculta schimbarile pentru a actualiza UI-ul
        //
        // EXEMPLU FOLOSIRE:
        // - Afisare nume user in header: CurrentUserStore.CurrentUserFullName
        // - Verificare permisiuni: CurrentUserStore.IsStoreOwner
        // - Logout: CurrentUserStore.Logout()
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Gestioneaza navigarea intre ecrane
        //
        // Tine referinta la ViewModel-ul afisat curent
        // Cand se schimba, MainWindow afiseaza View-ul corespunzator
        //
        // EXEMPLU:
        // _navigationStore.CurrentViewModel = new ProductManagementViewModel();
        // -> MainWindow afiseaza ProductManagementView
        private readonly NavigationStore _navigationStore;

        // CONSTRUCTOR - Initializeaza MainViewModel cu dependintele necesare
        //
        // DEPENDENCY INJECTION:
        // MainViewModel primeste dependintele prin constructor
        // Nu le creeaza singur (ar fi tightly coupled)
        //
        // AVANTAJE DEPENDENCY INJECTION:
        // 1. TESTABILITATE: In teste, poti injecta mock-uri
        // 2. FLEXIBILITATE: Poti schimba implementarile usor
        // 3. SINGLE RESPONSIBILITY: MainViewModel nu stie cum se creeaza dependintele
        //
        // PARAMETRI:
        // - currentUserStore: Store-ul pentru utilizatorul curent
        // - navigationStore: Store-ul pentru navigare
        public MainViewModel(CurrentUserStore currentUserStore, NavigationStore navigationStore)
        {
            // SALVARE REFERINTE
            // Salvam referintele pentru a le folosi ulterior
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // ABONARE LA EVENIMENTE
            //
            // Cand NavigationStore.CurrentViewModel se schimba,
            // trebuie sa notificam UI-ul ca CurrentViewModel s-a schimbat
            //
            // Aceasta e ESENTA navigarii in MVVM:
            // NavigationStore notifica -> MainViewModel notifica -> UI se actualizeaza
            _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;

            // Abonare la schimbarile de user
            // Cand user-ul se logheaza/delogheaza, UI-ul trebuie actualizat
            // (de exemplu, pentru a afisa/ascunde meniul de admin)
            _currentUserStore.CurrentUserChanged += OnCurrentUserChanged;

            // INITIALIZARE COMENZI
            // Cream comenzile pentru actiunile din UI
            LogoutCommand = new RelayCommand(ExecuteLogout, CanExecuteLogout);
            NavigateCommand = new RelayCommand(ExecuteNavigate);
        }

        // PROPRIETATI - Expuse catre View

        // CurrentViewModel - ViewModel-ul afisat curent
        //
        // Aceasta proprietate e legata de ContentControl in MainWindow
        // Cand se schimba, ContentControl afiseaza View-ul corespunzator
        //
        // BINDING IN XAML:
        // <ContentControl Content="{Binding CurrentViewModel}" />
        //
        // NOTA: Getter-ul doar redirecteaza la NavigationStore
        // Toata logica de setare e in NavigationStore
        public ViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel;

        // CurrentUser - Utilizatorul autentificat curent
        //
        // Shortcut catre CurrentUserStore.CurrentUser
        // Util pentru binding-uri simple in UI
        //
        // EXEMPLU:
        // <TextBlock Text="{Binding CurrentUser.FullName}" />
        public Models.User CurrentUser => _currentUserStore.CurrentUser;

        // IsLoggedIn - Este cineva autentificat?
        //
        // True daca avem un utilizator logat
        // Folosit pentru a afisa/ascunde elemente din UI
        //
        // EXEMPLU:
        // <Button Content="Logout" Visibility="{Binding IsLoggedIn, Converter={StaticResource BoolToVis}}" />
        public bool IsLoggedIn => _currentUserStore.IsLoggedIn;

        // CurrentUserName - Numele utilizatorului curent pentru afisare
        //
        // Folosit in header pentru salut: "Welcome, John Doe!"
        public string CurrentUserName => _currentUserStore.CurrentUserFullName;

        // IsStoreOwner - Este utilizatorul StoreOwner?
        //
        // Folosit pentru a afisa meniul de administrare
        // <MenuItem Header="Products" Visibility="{Binding IsStoreOwner, Converter={StaticResource BoolToVis}}" />
        public bool IsStoreOwner => _currentUserStore.IsStoreOwner;

        // IsCustomer - Este utilizatorul Customer?
        //
        // Folosit pentru a afisa magazinul si cosul
        public bool IsCustomer => _currentUserStore.IsCustomer;

        // IsCustomerService - Este utilizatorul Agent?
        //
        // Folosit pentru a afisa dashboard-ul de suport
        public bool IsCustomerService => _currentUserStore.IsCustomerService;

        // COMENZI - Actiuni expuse catre View

        // LogoutCommand - Comanda pentru delogare
        //
        // Legata de butonul de Logout in UI
        // <Button Command="{Binding LogoutCommand}" Content="Logout" />
        //
        // ACTIUNI:
        // 1. Curata utilizatorul din CurrentUserStore
        // 2. Navigheaza la ecranul de login
        public ICommand LogoutCommand { get; }

        // NavigateCommand - Comanda pentru navigare intre ecrane
        //
        // PARAMETRU: Numele ecranului unde sa navigam
        //
        // EXEMPLU:
        // <Button Command="{Binding NavigateCommand}"
        //         CommandParameter="Products"
        //         Content="Manage Products" />
        //
        // La click, se navigheaza la ProductManagementViewModel
        public ICommand NavigateCommand { get; }

        // EVENT HANDLERS - Reactii la schimbari

        // OnCurrentViewModelChanged - Handler pentru schimbarea ViewModel-ului
        //
        // Aceasta metoda e apelata cand NavigationStore.CurrentViewModel se schimba
        // Notifica UI-ul ca proprietatea CurrentViewModel s-a schimbat
        //
        // FLOW:
        // 1. Cineva seteaza NavigationStore.CurrentViewModel = new SomeViewModel()
        // 2. NavigationStore declanseaza CurrentViewModelChanged
        // 3. Aceasta metoda e apelata
        // 4. OnPropertyChanged notifica UI-ul
        // 5. ContentControl isi actualizeaza continutul
        private void OnCurrentViewModelChanged()
        {
            // Notificam UI-ul ca CurrentViewModel s-a schimbat
            OnPropertyChanged(nameof(CurrentViewModel));
        }

        // OnCurrentUserChanged - Handler pentru schimbarea utilizatorului
        //
        // Aceasta metoda e apelata cand:
        // - Un utilizator se logheaza (CurrentUser devine non-null)
        // - Un utilizator se delogheaza (CurrentUser devine null)
        //
        // Notifica UI-ul sa actualizeze toate proprietatile legate de user
        private void OnCurrentUserChanged()
        {
            // Notificam toate proprietatile care depind de utilizatorul curent
            // UI-ul va actualiza toate binding-urile
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(IsStoreOwner));
            OnPropertyChanged(nameof(IsCustomer));
            OnPropertyChanged(nameof(IsCustomerService));
        }

        // IMPLEMENTARI COMENZI

        // ExecuteLogout - Executa actiunea de logout
        //
        // PARAMETRU: parameter - nefolosit in acest caz
        //
        // ACTIUNI:
        // 1. Apeleaza Logout pe CurrentUserStore
        // 2. Navigheaza la ecranul de login
        private void ExecuteLogout(object parameter)
        {
            // Curata utilizatorul curent
            // Aceasta declanseaza CurrentUserChanged care actualizeaza UI-ul
            _currentUserStore.Logout();

            // Navigheaza la login
            // Putem crea un nou LoginViewModel aici sau in metoda Navigate
            // In functie de cum e structurata aplicatia
        }

        // CanExecuteLogout - Verifica daca se poate face logout
        //
        // RETURNEAZA: true daca cineva e logat
        //
        // EFECT UI: Butonul de Logout e dezactivat daca nimeni nu e logat
        private bool CanExecuteLogout(object parameter)
        {
            return IsLoggedIn;
        }

        // ExecuteNavigate - Executa navigarea la un ecran
        //
        // PARAMETRU: parameter - numele ecranului (string)
        //
        // EXEMPLU:
        // parameter = "Products" -> navigheaza la ProductManagementViewModel
        // parameter = "Orders" -> navigheaza la OrderManagementViewModel
        //
        // NOTA: Aceasta e o implementare simplificata
        // Intr-o aplicatie reala, ai putea folosi un NavigationService
        // sau factory pattern pentru a crea ViewModels
        private void ExecuteNavigate(object parameter)
        {
            // Extragem numele ecranului din parametru
            string screenName = parameter as string;

            if (string.IsNullOrEmpty(screenName))
                return;

            // Navigam in functie de numele ecranului
            // Aceasta e o implementare simplificata
            // Intr-o aplicatie reala, ai avea un mapping mai sofisticat
            // sau ai folosi un container de dependency injection
            switch (screenName)
            {
                case "Dashboard":
                    // Navigheaza la dashboard-ul corespunzator rolului
                    break;

                case "Products":
                    // Navigheaza la managementul produselor
                    break;

                case "Orders":
                    // Navigheaza la managementul comenzilor
                    break;

                case "Inventory":
                    // Navigheaza la managementul stocurilor
                    break;

                case "Settings":
                    // Navigheaza la setari
                    break;

                case "Shop":
                    // Navigheaza la magazin (pentru clienti)
                    break;

                case "Cart":
                    // Navigheaza la cos
                    break;

                case "Support":
                    // Navigheaza la suport
                    break;

                default:
                    // Ecran necunoscut - nu facem nimic
                    break;
            }
        }

        // CLEANUP - Eliberarea resurselor
        //
        // Override pentru a ne dezabona de la evenimente
        // Previne memory leaks
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dezabonare de la evenimente
                // IMPORTANT: Daca nu te dezabonezi, MainViewModel nu va fi
                // garbage collected pentru ca stores inca au referinta la el
                _navigationStore.CurrentViewModelChanged -= OnCurrentViewModelChanged;
                _currentUserStore.CurrentUserChanged -= OnCurrentUserChanged;
            }

            base.Dispose(disposing);
        }
    }
}
