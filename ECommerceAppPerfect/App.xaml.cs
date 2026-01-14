using System.Windows;
using ECommerceAppPerfect.Services;
using ECommerceAppPerfect.Stores;
using ECommerceAppPerfect.ViewModels;

namespace ECommerceAppPerfect
{
    // CLASA APP - Entry Point-ul aplicatiei WPF
    //
    // CE ESTE ACEASTA CLASA?
    // Este clasa principala a aplicatiei WPF
    // Se creeaza automat din App.xaml
    // Aici initializam serviciile si stores-urile
    //
    // CUM FUNCTIONEAZA?
    // 1. WPF creeaza instanta App la pornire
    // 2. Evenimentul OnStartup se declanseaza
    // 3. Cream si configuram serviciile
    // 4. Cream MainWindow si o afisam
    //
    // DEPENDENCY INJECTION (SIMPLIFICAT):
    // In aplicatii mari, ai folosi un container DI (Unity, Autofac, etc.)
    // Pentru simplitate, cream manual serviciile aici
    // Si le pasam prin constructori (Poor Man's DI)
    public partial class App : Application
    {
        // STORES - State global al aplicatiei
        // Cream o singura instanta care e partajata in toata aplicatia

        // CurrentUserStore - tine minte utilizatorul logat
        private readonly CurrentUserStore _currentUserStore;

        // NavigationStore - tine minte ce view e afisat
        private readonly NavigationStore _navigationStore;

        // CONSTRUCTORUL APP
        //
        // Se apeleaza PRIMUL la pornirea aplicatiei
        // Initializam stores-urile aici
        public App()
        {
            // Cream stores-urile
            // Acestea vor fi partajate intre toate ViewModels
            _currentUserStore = new CurrentUserStore();
            _navigationStore = new NavigationStore();
        }

        // METODA OnStartup - Se apeleaza cand aplicatia porneste
        //
        // PARAMETRU: e - argumentele de pornire
        //
        // CE FACEM AICI?
        // 1. Setam ViewModel-ul initial (LoginViewModel)
        // 2. Cream MainWindow cu MainViewModel
        // 3. Afisam fereastra principala
        protected override void OnStartup(StartupEventArgs e)
        {
            // Apelam implementarea din clasa de baza
            base.OnStartup(e);

            // Setam view-ul initial - pagina de Login
            // Cand aplicatia porneste, utilizatorul trebuie sa se autentifice
            _navigationStore.CurrentViewModel = CreateLoginViewModel();

            // Cream MainWindow cu MainViewModel
            // MainViewModel primeste stores-urile pentru a le putea folosi
            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(_currentUserStore, _navigationStore, CreateViewModelFactory())
            };

            // Afisam fereastra principala
            MainWindow.Show();
        }

        // METODE FACTORY - Creeaza ViewModels cu dependintele lor
        //
        // DE CE FACTORY METHODS?
        // ViewModels au nevoie de servicii si stores
        // Factory methods centralizeaza crearea si configurarea
        // Simplifica dependency injection

        // CreateLoginViewModel - Creeaza un nou LoginViewModel
        //
        // LoginViewModel are nevoie de:
        // - IUserService pentru autentificare
        // - CurrentUserStore pentru a salva utilizatorul logat
        // - NavigationStore pentru a naviga dupa login
        private LoginViewModel CreateLoginViewModel()
        {
            return new LoginViewModel(
                new UserService(),
                _currentUserStore,
                _navigationStore,
                CreateViewModelFactory()
            );
        }

        // CreateViewModelFactory - Returneaza o functie care creeaza ViewModels
        //
        // DE CE O FUNCTIE SI NU UN OBIECT?
        // Pentru ca ViewModels se creeaza la cerere (lazy)
        // Functia permite crearea la momentul necesar
        //
        // FOLOSIRE:
        // var factory = CreateViewModelFactory();
        // var dashboardVM = factory("StoreOwnerDashboard");
        private System.Func<string, ViewModelBase> CreateViewModelFactory()
        {
            // Returnam o functie lambda care creeaza ViewModel-ul cerut
            return viewModelName =>
            {
                // Switch pe numele ViewModel-ului cerut
                return viewModelName switch
                {
                    // Login
                    "Login" => CreateLoginViewModel(),

                    // Store Owner views
                    "StoreOwnerDashboard" => new StoreOwnerDashboardViewModel(
                        _currentUserStore,
                        _navigationStore,
                        CreateViewModelFactory()
                    ),

                    "ProductManagement" => new ProductManagementViewModel(
                        new ProductService(),
                        _currentUserStore
                    ),

                    "InventoryManagement" => new InventoryManagementViewModel(
                        new InventoryService(),
                        _currentUserStore
                    ),

                    "OrderManagement" => new OrderManagementViewModel(
                        new OrderService(),
                        _currentUserStore
                    ),

                    "StoreSettings" => new StoreSettingsViewModel(
                        new StoreSettingsService()
                    ),

                    // Customer views
                    "CustomerShop" => new CustomerShopViewModel(
                        new ProductService(),
                        _currentUserStore,
                        _navigationStore,
                        CreateViewModelFactory()
                    ),

                    "ShoppingCart" => new ShoppingCartViewModel(
                        new OrderService(),
                        _currentUserStore,
                        _navigationStore,
                        CreateViewModelFactory()
                    ),

                    "OrderHistory" => new OrderHistoryViewModel(
                        new OrderService(),
                        _currentUserStore
                    ),

                    // Customer Service views
                    "CustomerService" => new CustomerServiceViewModel(
                        new SupportService(),
                        _currentUserStore
                    ),

                    // Default: Login
                    _ => CreateLoginViewModel()
                };
            };
        }

        // METODA OnExit - Se apeleaza cand aplicatia se inchide
        //
        // CE FACEM AICI?
        // Cleanup - eliberam resursele
        // Inchidem conexiunile la baza de date
        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup stores
            // (in cazul nostru nu e necesar, dar e good practice)

            base.OnExit(e);
        }
    }
}
