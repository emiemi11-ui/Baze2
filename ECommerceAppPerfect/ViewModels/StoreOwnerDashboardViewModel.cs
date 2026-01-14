using System;
using System.Windows.Input;
using ECommerceAppPerfect.Commands;
using ECommerceAppPerfect.Services;
using ECommerceAppPerfect.Stores;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA STOREOWNERDASHBOARDVIEWMODEL - Dashboard pentru proprietarul magazinului
    //
    // CE ESTE ACEST DASHBOARD?
    // Este ecranul principal pe care il vede StoreOwner-ul dupa login
    // Afiseaza o prezentare generala a starii magazinului:
    // - Statistici cheie (produse, comenzi, venituri)
    // - Alerte (stoc redus, comenzi noi)
    // - Actiuni rapide (adauga produs, vezi comenzi)
    //
    // SCOPUL DASHBOARD-ULUI:
    // 1. VIZIBILITATE - StoreOwner-ul vede instant ce se intampla in magazin
    // 2. ACCES RAPID - Butoane pentru actiunile frecvente
    // 3. ALERTE - Probleme care necesita atentie (low stock, comenzi pending)
    //
    // CE STATISTICI AFISAM?
    // - TotalProducts: Cate produse active avem
    // - TotalOrders: Cate comenzi au fost plasate (total sau azi)
    // - TotalRevenue: Venitul total sau din ultima perioada
    // - LowStockCount: Cate produse au stoc sub minim
    // - PendingOrders: Comenzi care asteapta procesare
    //
    // LAYOUT TIPIC:
    // +------------------+------------------+------------------+
    // |    Products      |     Orders       |    Revenue       |
    // |       150        |       45         |   $12,500        |
    // +------------------+------------------+------------------+
    // |                     ALERTS                             |
    // | ! 5 products low on stock                              |
    // | ! 3 orders pending                                     |
    // +-------------------------------------------------------+
    // |                  QUICK ACTIONS                         |
    // | [Add Product] [View Orders] [Manage Inventory]         |
    // +-------------------------------------------------------+
    //
    // PATTERN MVVM:
    // Dashboard-ul e un ViewModel care:
    // - Incarca date din servicii la initializare
    // - Expune proprietati pentru statistici (binding)
    // - Expune comenzi pentru actiuni rapide
    public class StoreOwnerDashboardViewModel : ViewModelBase
    {
        // SERVICII - Pentru incarcarea datelor

        // _productService - Serviciul pentru produse
        //
        // Folosit pentru a obtine:
        // - Numarul total de produse
        // - Produsele cu stoc redus
        private readonly IProductService _productService;

        // STORES - Starea globala

        // _currentUserStore - Informatii despre utilizatorul curent
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare la alte ecrane
        private readonly NavigationStore _navigationStore;

        // CAMPURI PENTRU STATISTICI
        // Acestea sunt actualizate la incarcarea dashboard-ului

        // Numarul total de produse active
        private int _totalProducts;

        // Numarul total de comenzi
        private int _totalOrders;

        // Numarul de comenzi in asteptare (Pending)
        private int _pendingOrdersCount;

        // Numarul de produse cu stoc redus
        private int _lowStockCount;

        // Venitul total
        private decimal _totalRevenue;

        // Venitul din ziua curenta
        private decimal _todayRevenue;

        // Flag pentru incarcare date
        private bool _isLoading;

        // Mesaj de eroare (daca incarcarea esueaza)
        private string _errorMessage;

        // Data ultimei actualizari
        private DateTime _lastUpdated;

        // CONSTRUCTOR - Initializeaza dashboard-ul
        //
        // La constructie:
        // 1. Salvam dependintele
        // 2. Cream comenzile
        // 3. Incarcam datele
        public StoreOwnerDashboardViewModel(
            IProductService productService,
            CurrentUserStore currentUserStore,
            NavigationStore navigationStore)
        {
            // Salvare dependinte
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare comenzi pentru actiuni rapide
            // Fiecare buton din UI va fi legat de una din aceste comenzi
            NavigateToProductsCommand = new RelayCommand(ExecuteNavigateToProducts);
            NavigateToOrdersCommand = new RelayCommand(ExecuteNavigateToOrders);
            NavigateToInventoryCommand = new RelayCommand(ExecuteNavigateToInventory);
            NavigateToSettingsCommand = new RelayCommand(ExecuteNavigateToSettings);
            AddProductCommand = new RelayCommand(ExecuteAddProduct);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);

            // Incarca datele la pornire
            LoadDashboardData();
        }

        // PROPRIETATI - Statistici expuse catre View

        // TotalProducts - Numarul total de produse active
        //
        // BINDING:
        // <TextBlock Text="{Binding TotalProducts}" Style="{StaticResource StatNumber}" />
        // <TextBlock Text="Products" Style="{StaticResource StatLabel}" />
        //
        // SEMNIFICATIE:
        // Arata dimensiunea catalogului de produse
        // Include doar produsele cu IsActive = true
        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value);
        }

        // TotalOrders - Numarul total de comenzi
        //
        // Poate fi:
        // - Total din toate timpurile
        // - Total din luna curenta
        // - Depinde de cum definim metrica
        public int TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        // PendingOrdersCount - Comenzi in asteptare
        //
        // Comenzi cu status "Pending" care trebuie procesate
        // Aceasta e o metrica importanta - comenzile pending trebuie procesate rapid
        //
        // AFISARE CU ALERTA:
        // Daca > 0, afisam cu culoare de avertizare
        // <TextBlock Text="{Binding PendingOrdersCount}"
        //            Foreground="{Binding PendingOrdersCount, Converter={StaticResource CountToAlertColor}}" />
        public int PendingOrdersCount
        {
            get => _pendingOrdersCount;
            set => SetProperty(ref _pendingOrdersCount, value);
        }

        // LowStockCount - Produse cu stoc redus
        //
        // Produse unde StockQuantity < MinimumStock
        // ALERTA IMPORTANTA - trebuie reaprovizionare
        //
        // IN UI:
        // Daca > 0, afisam un banner de alerta:
        // "Warning: 5 products are low on stock!"
        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        // TotalRevenue - Venitul total
        //
        // Suma tuturor comenzilor (sau din perioada selectata)
        // Folosit pentru a vedea performanta magazinului
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        // TotalRevenueFormatted - Venitul formatat pentru afisare
        //
        // Format: "12,500.00 RON"
        // Proprietate calculata - nu e stocata separat
        public string TotalRevenueFormatted => $"{TotalRevenue:N2} RON";

        // TodayRevenue - Venitul din ziua curenta
        //
        // Comenzile plasate azi
        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set => SetProperty(ref _todayRevenue, value);
        }

        // TodayRevenueFormatted - Venitul de azi formatat
        public string TodayRevenueFormatted => $"{TodayRevenue:N2} RON";

        // IsLoading - Se incarca datele?
        //
        // True in timpul incarcarii
        // Folosit pentru a afisa un spinner sau overlay
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ErrorMessage - Mesaj de eroare
        //
        // Afisat daca incarcarea datelor esueaza
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // HasError - Exista eroare?
        //
        // Pentru binding de vizibilitate
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // LastUpdated - Ultima actualizare
        //
        // Afisam cand au fost incarcate datele
        // "Last updated: 14:30:25"
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        // LastUpdatedFormatted - Data formatata
        public string LastUpdatedFormatted => $"Last updated: {LastUpdated:HH:mm:ss}";

        // PROPRIETATI DERIVATE - Alerte si statusuri

        // HasLowStockAlert - Trebuie afisata alerta de stoc?
        //
        // True daca avem produse cu stoc redus
        public bool HasLowStockAlert => LowStockCount > 0;

        // HasPendingOrdersAlert - Trebuie afisata alerta de comenzi?
        //
        // True daca avem comenzi in asteptare
        public bool HasPendingOrdersAlert => PendingOrdersCount > 0;

        // WelcomeMessage - Mesajul de bun venit
        //
        // "Welcome back, John!"
        public string WelcomeMessage => $"Welcome back, {_currentUserStore.CurrentUserFullName}!";

        // COMENZI - Actiuni rapide

        // NavigateToProductsCommand - Navigheaza la managementul produselor
        //
        // <Button Command="{Binding NavigateToProductsCommand}"
        //         Content="Manage Products" />
        public ICommand NavigateToProductsCommand { get; }

        // NavigateToOrdersCommand - Navigheaza la managementul comenzilor
        public ICommand NavigateToOrdersCommand { get; }

        // NavigateToInventoryCommand - Navigheaza la managementul stocurilor
        public ICommand NavigateToInventoryCommand { get; }

        // NavigateToSettingsCommand - Navigheaza la setari
        public ICommand NavigateToSettingsCommand { get; }

        // AddProductCommand - Deschide dialogul de adaugare produs
        //
        // Actiune rapida direct din dashboard
        public ICommand AddProductCommand { get; }

        // RefreshCommand - Reincarca datele dashboard-ului
        //
        // <Button Command="{Binding RefreshCommand}" Content="Refresh" />
        public ICommand RefreshCommand { get; }

        // METODE PRIVATE - Logica interna

        // LoadDashboardData - Incarca toate datele dashboard-ului
        //
        // Apelata la:
        // - Constructie (initializare)
        // - Refresh (butonul de actualizare)
        //
        // OPERATII:
        // 1. Seteaza IsLoading = true
        // 2. Apeleaza serviciile pentru date
        // 3. Actualizeaza proprietatile
        // 4. Seteaza IsLoading = false
        private void LoadDashboardData()
        {
            // Marcam ca incarcam
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // INCARCARE DATE PRODUSE
                // Obtinem toate produsele pentru statistici
                var products = _productService.GetAllProducts();

                // Calculam statisticile
                TotalProducts = products?.Count ?? 0;

                // Numaram produsele cu stoc redus
                // Un produs e low stock daca Inventory.StockQuantity < Inventory.MinimumStock
                int lowStock = 0;
                if (products != null)
                {
                    foreach (var product in products)
                    {
                        if (product.IsLowStock)
                        {
                            lowStock++;
                        }
                    }
                }
                LowStockCount = lowStock;

                // INCARCARE DATE COMENZI
                // Aici am avea nevoie de IOrderService
                // Pentru simplitate, folosim valori placeholder
                // In productie: var orders = _orderService.GetAllOrders();
                TotalOrders = 0;
                PendingOrdersCount = 0;
                TotalRevenue = 0;
                TodayRevenue = 0;

                // Actualizam timestamp-ul
                LastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                // Eroare la incarcare
                ErrorMessage = "Failed to load dashboard data. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
            }
            finally
            {
                // Gata cu incarcarea
                IsLoading = false;

                // Notificam proprietatile derivate
                OnPropertyChanged(nameof(HasLowStockAlert));
                OnPropertyChanged(nameof(HasPendingOrdersAlert));
                OnPropertyChanged(nameof(TotalRevenueFormatted));
                OnPropertyChanged(nameof(TodayRevenueFormatted));
                OnPropertyChanged(nameof(LastUpdatedFormatted));
            }
        }

        // IMPLEMENTARI COMENZI

        // ExecuteNavigateToProducts - Navigheaza la ProductManagementViewModel
        private void ExecuteNavigateToProducts(object parameter)
        {
            // Cream si navigam la ViewModel-ul de produse
            // _navigationStore.CurrentViewModel = new ProductManagementViewModel(...);
        }

        // ExecuteNavigateToOrders - Navigheaza la OrderManagementViewModel
        private void ExecuteNavigateToOrders(object parameter)
        {
            // _navigationStore.CurrentViewModel = new OrderManagementViewModel(...);
        }

        // ExecuteNavigateToInventory - Navigheaza la InventoryManagementViewModel
        private void ExecuteNavigateToInventory(object parameter)
        {
            // _navigationStore.CurrentViewModel = new InventoryManagementViewModel(...);
        }

        // ExecuteNavigateToSettings - Navigheaza la StoreSettingsViewModel
        private void ExecuteNavigateToSettings(object parameter)
        {
            // _navigationStore.CurrentViewModel = new StoreSettingsViewModel(...);
        }

        // ExecuteAddProduct - Deschide dialogul de adaugare produs
        //
        // Poate naviga la ProductManagementViewModel in modul "Add"
        // sau deschide un dialog/popup
        private void ExecuteAddProduct(object parameter)
        {
            // Optiunea 1: Navigam la Products cu flag de adaugare
            // _navigationStore.CurrentViewModel = new ProductManagementViewModel(..., addNew: true);

            // Optiunea 2: Deschidem un dialog
            // DialogService.ShowDialog(new AddProductDialog());
        }

        // ExecuteRefresh - Reincarca datele
        private void ExecuteRefresh(object parameter)
        {
            LoadDashboardData();
        }

        // CanExecuteRefresh - Se poate face refresh?
        //
        // Nu putem face refresh daca deja incarcam
        private bool CanExecuteRefresh(object parameter)
        {
            return !IsLoading;
        }
    }
}
