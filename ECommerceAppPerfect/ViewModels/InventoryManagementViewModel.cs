using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ECommerceAppPerfect.Commands;
using ECommerceAppPerfect.Models;
using ECommerceAppPerfect.Services;
using ECommerceAppPerfect.Stores;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA INVENTORYMANAGEMENTVIEWMODEL - Management stocuri
    //
    // CE ESTE ACEST VIEWMODEL?
    // Gestioneaza stocul produselor din magazin
    // Permite actualizarea cantitatilor si monitorizarea stocului redus
    //
    // RELATIA ONE-TO-ONE PRODUCT -> INVENTORY:
    // Aceasta este una din relatiile cheie din cerinte!
    //
    // FIECARE PRODUS are EXACT UN INVENTORY
    // FIECARE INVENTORY apartine EXACT UNUI PRODUS
    //
    // IN BAZA DE DATE:
    // Products (1) -------- (1) Inventory
    //
    // Constrangerea UNIQUE pe ProductID in tabelul Inventory garanteaza
    // ca un produs nu poate avea mai multe inventories
    //
    // CUM SE DEMONSTREAZA RELATIA ONE-TO-ONE?
    // 1. La adaugare produs: se creeaza automat Inventory-ul asociat
    // 2. La stergere produs: se sterge si Inventory-ul (cascade)
    // 3. La accesare: product.Inventory sau inventory.Product
    //
    // FUNCTIONALITATI:
    // - Lista tuturor inventoriilor cu produsele asociate
    // - Filtrare: doar Low Stock, doar Out of Stock
    // - Update stoc: modifica StockQuantity
    // - Update minimum: modifica MinimumStock
    //
    // UI TIPIC:
    // +--------------------------------------------------------+
    // | Product Name     | Stock | Minimum | Status   | Actions|
    // +--------------------------------------------------------+
    // | iPhone 15        |    5  |   10    | Low Stock|  [+][-]|
    // | MacBook Pro      |   25  |    5    | In Stock |  [+][-]|
    // | AirPods          |    0  |    5    | Out      |  [+][-]|
    // +--------------------------------------------------------+
    // | [Show All] [Low Stock Only] [Out of Stock Only]        |
    // +--------------------------------------------------------+
    public class InventoryManagementViewModel : ViewModelBase
    {
        // SERVICII

        // _productService - Pentru operatii cu produse si inventar
        //
        // Folosim ProductService pentru ca Inventory e strans legat de Product
        // In productie, am putea avea un IInventoryService separat
        private readonly IProductService _productService;

        // STORES

        // _currentUserStore - Informatii despre utilizatorul curent
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare
        private readonly NavigationStore _navigationStore;

        // COLECTII

        // _allInventoryItems - Lista completa (nefiltrata)
        //
        // Tine toate Inventory-urile incarcate
        // InventoryItems (expusa) poate fi filtrata
        private List<Inventory> _allInventoryItems;

        // _inventoryItems - Lista filtrata expusa catre UI
        //
        // ObservableCollection pentru actualizare automata UI
        private ObservableCollection<Inventory> _inventoryItems;

        // _lowStockItems - Lista cu produse low stock
        //
        // Subset al listei principale pentru afisare separata
        // Utila pentru alertele din dashboard sau sidebar
        private ObservableCollection<Inventory> _lowStockItems;

        // CAMPURI PROPRIETATI

        // Inventory-ul selectat in lista
        private Inventory _selectedInventory;

        // Modul de filtrare curent
        private string _filterMode;

        // Flag incarcare
        private bool _isLoading;

        // Mesaj eroare
        private string _errorMessage;

        // Cantitatea de adaugat/scazut (pentru update rapid)
        private int _quantityToAdjust;

        // CONSTRUCTOR
        public InventoryManagementViewModel(
            IProductService productService,
            CurrentUserStore currentUserStore,
            NavigationStore navigationStore)
        {
            // Salvare dependinte
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare colectii
            _inventoryItems = new ObservableCollection<Inventory>();
            _lowStockItems = new ObservableCollection<Inventory>();
            _allInventoryItems = new List<Inventory>();

            // Valoare default pentru ajustare
            _quantityToAdjust = 1;

            // Initializare comenzi
            UpdateStockCommand = new RelayCommand(ExecuteUpdateStock, CanExecuteUpdateStock);
            IncreaseStockCommand = new RelayCommand(ExecuteIncreaseStock, CanExecuteModifyStock);
            DecreaseStockCommand = new RelayCommand(ExecuteDecreaseStock, CanExecuteDecreaseStock);
            UpdateMinimumStockCommand = new RelayCommand(ExecuteUpdateMinimumStock, CanExecuteUpdateMinimumStock);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
            ShowAllCommand = new RelayCommand(ExecuteShowAll);
            ShowLowStockCommand = new RelayCommand(ExecuteShowLowStock);
            ShowOutOfStockCommand = new RelayCommand(ExecuteShowOutOfStock);
            NavigateToProductCommand = new RelayCommand(ExecuteNavigateToProduct, CanExecuteNavigateToProduct);

            // Incarcare date
            LoadData();
        }

        // PROPRIETATI - Colectii

        // InventoryItems - Lista de inventar afisata
        //
        // Contine Inventory-uri, fiecare cu Product-ul asociat
        // Demonstreaza relatia One-to-One:
        // item.Product.ProductName -> numele produsului
        // item.StockQuantity -> cantitatea in stoc
        //
        // BINDING:
        // <DataGrid ItemsSource="{Binding InventoryItems}">
        //     <DataGridTextColumn Header="Product" Binding="{Binding Product.ProductName}" />
        //     <DataGridTextColumn Header="Stock" Binding="{Binding StockQuantity}" />
        //     <DataGridTextColumn Header="Minimum" Binding="{Binding MinimumStock}" />
        //     <DataGridTextColumn Header="Status" Binding="{Binding StockStatus}" />
        // </DataGrid>
        public ObservableCollection<Inventory> InventoryItems
        {
            get => _inventoryItems;
            set => SetProperty(ref _inventoryItems, value);
        }

        // LowStockItems - Lista produselor cu stoc redus
        //
        // Subset filtrat pentru afisare in sidebar sau alerte
        // Contine doar items unde IsLowStock == true
        //
        // BINDING:
        // <ItemsControl ItemsSource="{Binding LowStockItems}">
        //     <ItemsControl.ItemTemplate>
        //         <DataTemplate>
        //             <TextBlock>
        //                 <Run Text="{Binding Product.ProductName}" />
        //                 <Run Text=" - " />
        //                 <Run Text="{Binding StockStatus}" Foreground="Orange" />
        //             </TextBlock>
        //         </DataTemplate>
        //     </ItemsControl.ItemTemplate>
        // </ItemsControl>
        public ObservableCollection<Inventory> LowStockItems
        {
            get => _lowStockItems;
            set => SetProperty(ref _lowStockItems, value);
        }

        // PROPRIETATI - Selectie

        // SelectedInventory - Itemul selectat in lista
        //
        // Folosit pentru operatiile de update
        public Inventory SelectedInventory
        {
            get => _selectedInventory;
            set
            {
                if (SetProperty(ref _selectedInventory, value))
                {
                    // Invalidam comenzile pentru a reverifica CanExecute
                    CommandManager.InvalidateRequerySuggested();

                    // Notificam proprietatile derivate
                    OnPropertyChanged(nameof(SelectedProductName));
                    OnPropertyChanged(nameof(SelectedStockQuantity));
                    OnPropertyChanged(nameof(SelectedMinimumStock));
                    OnPropertyChanged(nameof(SelectedStockStatus));
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }

        // QuantityToAdjust - Cantitatea pentru ajustari rapide
        //
        // Default 1, dar utilizatorul poate schimba
        // Folosit de butoanele [+] si [-]
        public int QuantityToAdjust
        {
            get => _quantityToAdjust;
            set => SetProperty(ref _quantityToAdjust, Math.Max(1, value)); // Minim 1
        }

        // PROPRIETATI - Stare

        // FilterMode - Modul de filtrare curent
        //
        // Valori: "All", "LowStock", "OutOfStock"
        public string FilterMode
        {
            get => _filterMode;
            set => SetProperty(ref _filterMode, value);
        }

        // IsLoading - Se incarca?
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ErrorMessage - Mesaj eroare
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // HasError - Exista eroare?
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // PROPRIETATI CALCULATE - Informatii despre selectie

        // HasSelection - Este selectat ceva?
        public bool HasSelection => SelectedInventory != null;

        // SelectedProductName - Numele produsului selectat
        //
        // Demonstreaza navigarea relatiei One-to-One
        // SelectedInventory.Product.ProductName
        public string SelectedProductName => SelectedInventory?.Product?.ProductName ?? "No selection";

        // SelectedStockQuantity - Cantitatea in stoc pentru selectie
        public int SelectedStockQuantity => SelectedInventory?.StockQuantity ?? 0;

        // SelectedMinimumStock - Minimul pentru selectie
        public int SelectedMinimumStock => SelectedInventory?.MinimumStock ?? 0;

        // SelectedStockStatus - Statusul pentru selectie
        public string SelectedStockStatus => SelectedInventory?.StockStatus ?? "N/A";

        // PROPRIETATI CALCULATE - Statistici

        // TotalItems - Total itemuri in inventar
        public int TotalItems => _allInventoryItems?.Count ?? 0;

        // LowStockCount - Cate produse sunt low stock
        public int LowStockCount => _lowStockItems?.Count ?? 0;

        // OutOfStockCount - Cate produse sunt out of stock
        public int OutOfStockCount => _allInventoryItems?.Count(i => i.IsOutOfStock) ?? 0;

        // COMENZI

        // UpdateStockCommand - Actualizeaza stocul cu o valoare specifica
        //
        // Deschide un dialog pentru a introduce noua cantitate
        public ICommand UpdateStockCommand { get; }

        // IncreaseStockCommand - Creste stocul cu QuantityToAdjust
        //
        // Butonul [+] din UI
        public ICommand IncreaseStockCommand { get; }

        // DecreaseStockCommand - Scade stocul cu QuantityToAdjust
        //
        // Butonul [-] din UI
        // CanExecute verifica sa nu scada sub 0
        public ICommand DecreaseStockCommand { get; }

        // UpdateMinimumStockCommand - Actualizeaza minimul
        public ICommand UpdateMinimumStockCommand { get; }

        // RefreshCommand - Reincarca datele
        public ICommand RefreshCommand { get; }

        // ShowAllCommand - Afiseaza toate itemurile
        public ICommand ShowAllCommand { get; }

        // ShowLowStockCommand - Filtreaza doar low stock
        public ICommand ShowLowStockCommand { get; }

        // ShowOutOfStockCommand - Filtreaza doar out of stock
        public ICommand ShowOutOfStockCommand { get; }

        // NavigateToProductCommand - Navigheaza la produsul selectat
        public ICommand NavigateToProductCommand { get; }

        // METODE INCARCARE DATE

        // LoadData - Incarca toate datele de inventar
        private void LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Incarcam produsele cu Inventory (relatia One-to-One)
                var products = _productService.GetAllProducts();

                // Extragem Inventory-urile
                // DEMONSTRATIE RELATIE ONE-TO-ONE:
                // Fiecare product.Inventory e un singur obiect (nu colectie)
                _allInventoryItems = products
                    .Where(p => p.Inventory != null)
                    .Select(p => p.Inventory)
                    .ToList();

                // Aplicam filtrul curent
                ApplyFilter();

                // Actualizam lista de low stock
                UpdateLowStockItems();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load inventory data: " + ex.Message;
                System.Diagnostics.Debug.WriteLine($"Inventory load error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // UpdateLowStockItems - Actualizeaza lista de produse low stock
        private void UpdateLowStockItems()
        {
            LowStockItems.Clear();

            var lowStock = _allInventoryItems.Where(i => i.IsLowStock);
            foreach (var item in lowStock)
            {
                LowStockItems.Add(item);
            }

            // Notificam statisticile
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(OutOfStockCount));
            OnPropertyChanged(nameof(TotalItems));
        }

        // ApplyFilter - Aplica filtrul curent
        private void ApplyFilter()
        {
            InventoryItems.Clear();

            IEnumerable<Inventory> filtered = _allInventoryItems;

            // Aplicam filtrul in functie de mod
            switch (FilterMode)
            {
                case "LowStock":
                    filtered = filtered.Where(i => i.IsLowStock);
                    break;

                case "OutOfStock":
                    filtered = filtered.Where(i => i.IsOutOfStock);
                    break;

                // "All" sau default: toate itemurile
            }

            foreach (var item in filtered)
            {
                InventoryItems.Add(item);
            }
        }

        // IMPLEMENTARI COMENZI

        // ExecuteUpdateStock - Actualizeaza stocul cu o valoare noua
        private void ExecuteUpdateStock(object parameter)
        {
            if (SelectedInventory == null)
                return;

            // Aici am deschide un dialog pentru a cere noua cantitate
            // var dialog = new InputDialog("Enter new stock quantity:");
            // if (dialog.ShowDialog() == true && int.TryParse(dialog.Value, out int newStock))
            // {
            //     SelectedInventory.StockQuantity = newStock;
            //     SelectedInventory.LastUpdated = DateTime.Now;
            //     // Salvare in DB
            // }
        }

        // CanExecuteUpdateStock - Se poate actualiza?
        private bool CanExecuteUpdateStock(object parameter)
        {
            return SelectedInventory != null && !IsLoading;
        }

        // ExecuteIncreaseStock - Creste stocul
        private void ExecuteIncreaseStock(object parameter)
        {
            if (SelectedInventory == null)
                return;

            // Crestem stocul
            SelectedInventory.IncreaseStock(QuantityToAdjust);

            // Notificam UI-ul
            OnPropertyChanged(nameof(SelectedStockQuantity));
            OnPropertyChanged(nameof(SelectedStockStatus));

            // Actualizam lista low stock
            UpdateLowStockItems();

            // In productie, am salva in DB
            // _inventoryService.UpdateInventory(SelectedInventory);
        }

        // CanExecuteModifyStock - Se poate modifica stocul?
        private bool CanExecuteModifyStock(object parameter)
        {
            return SelectedInventory != null && !IsLoading;
        }

        // ExecuteDecreaseStock - Scade stocul
        private void ExecuteDecreaseStock(object parameter)
        {
            if (SelectedInventory == null)
                return;

            // Incercam sa scadem stocul
            bool success = SelectedInventory.ReduceStock(QuantityToAdjust);

            if (!success)
            {
                ErrorMessage = "Cannot reduce stock below 0";
                return;
            }

            // Notificam UI-ul
            OnPropertyChanged(nameof(SelectedStockQuantity));
            OnPropertyChanged(nameof(SelectedStockStatus));

            // Actualizam lista low stock
            UpdateLowStockItems();
        }

        // CanExecuteDecreaseStock - Se poate scadea?
        //
        // Verifica sa nu scada sub 0
        private bool CanExecuteDecreaseStock(object parameter)
        {
            return SelectedInventory != null &&
                   !IsLoading &&
                   SelectedInventory.StockQuantity >= QuantityToAdjust;
        }

        // ExecuteUpdateMinimumStock - Actualizeaza minimul
        private void ExecuteUpdateMinimumStock(object parameter)
        {
            // Similar cu UpdateStock, dar pentru MinimumStock
        }

        // CanExecuteUpdateMinimumStock - Se poate actualiza minimul?
        private bool CanExecuteUpdateMinimumStock(object parameter)
        {
            return SelectedInventory != null && !IsLoading;
        }

        // ExecuteRefresh - Reincarca datele
        private void ExecuteRefresh(object parameter)
        {
            LoadData();
        }

        // CanExecuteRefresh - Se poate face refresh?
        private bool CanExecuteRefresh(object parameter)
        {
            return !IsLoading;
        }

        // ExecuteShowAll - Afiseaza toate itemurile
        private void ExecuteShowAll(object parameter)
        {
            FilterMode = "All";
            ApplyFilter();
        }

        // ExecuteShowLowStock - Filtreaza low stock
        private void ExecuteShowLowStock(object parameter)
        {
            FilterMode = "LowStock";
            ApplyFilter();
        }

        // ExecuteShowOutOfStock - Filtreaza out of stock
        private void ExecuteShowOutOfStock(object parameter)
        {
            FilterMode = "OutOfStock";
            ApplyFilter();
        }

        // ExecuteNavigateToProduct - Navigheaza la produsul selectat
        private void ExecuteNavigateToProduct(object parameter)
        {
            if (SelectedInventory?.Product == null)
                return;

            // Navigam la editarea produsului
            // _navigationStore.CurrentViewModel = new ProductEditViewModel(SelectedInventory.Product, ...);
        }

        // CanExecuteNavigateToProduct - Se poate naviga?
        private bool CanExecuteNavigateToProduct(object parameter)
        {
            return SelectedInventory?.Product != null;
        }
    }
}
