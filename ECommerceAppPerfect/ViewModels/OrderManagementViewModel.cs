using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ECommerceAppPerfect.Commands;
using ECommerceAppPerfect.Models;
using ECommerceAppPerfect.Stores;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA ORDERMANAGEMENTVIEWMODEL - Management comenzi pentru StoreOwner
    //
    // CE ESTE ACEST VIEWMODEL?
    // Permite StoreOwner-ului sa gestioneze comenzile magazinului:
    // - Vizualizare toate comenzile
    // - Filtrare dupa status
    // - Actualizare status comenzi
    // - Vizualizare detalii comanda
    //
    // WORKFLOW COMENZI:
    // Pending -> Processing -> Shipped -> Delivered
    //     \                         /
    //      -----> Cancelled <------
    //
    // 1. PENDING: Comanda tocmai plasata, asteapta procesare
    // 2. PROCESSING: Comanda este pregatita (impachetare, verificare stoc)
    // 3. SHIPPED: Comanda a fost expediata catre client
    // 4. DELIVERED: Comanda a ajuns la client
    // 5. CANCELLED: Comanda anulata (de client sau magazin)
    //
    // RELATII DEMONSTRATE:
    // - Order -> Customer (Many-to-One): Fiecare comanda apartine unui client
    // - Order -> OrderDetails (One-to-Many): O comanda are mai multe linii
    // - OrderDetail -> Product (Many-to-One): Fiecare linie refera un produs
    //
    // FUNCTIONALITATI UI:
    // 1. Lista comenzi cu coloane: OrderNumber, Date, Customer, Total, Status
    // 2. Filtre: All, Pending, Processing, Shipped, Delivered, Cancelled
    // 3. Detalii comanda selectata (lista produse din comanda)
    // 4. Butoane: Update Status, View Details, Cancel Order
    //
    // BINDING EXEMPLU:
    // <DataGrid ItemsSource="{Binding Orders}" SelectedItem="{Binding SelectedOrder}">
    //     <DataGridTextColumn Header="Order #" Binding="{Binding OrderNumber}" />
    //     <DataGridTextColumn Header="Date" Binding="{Binding OrderDate, StringFormat='{}{0:dd/MM/yyyy}'}" />
    //     <DataGridTextColumn Header="Customer" Binding="{Binding Customer.FullName}" />
    //     <DataGridTextColumn Header="Total" Binding="{Binding TotalFormatted}" />
    //     <DataGridTextColumn Header="Status" Binding="{Binding OrderStatus}" />
    // </DataGrid>
    public class OrderManagementViewModel : ViewModelBase
    {
        // STORES

        // _currentUserStore - Informatii despre utilizatorul curent
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare
        private readonly NavigationStore _navigationStore;

        // COLECTII

        // _allOrders - Lista completa de comenzi (nefiltrata)
        private List<Order> _allOrders;

        // _orders - Lista filtrata expusa catre UI
        private ObservableCollection<Order> _orders;

        // _orderDetails - Detaliile comenzii selectate
        //
        // Demonstreaza relatia One-to-Many: Order -> OrderDetails
        private ObservableCollection<OrderDetail> _orderDetails;

        // _statusOptions - Optiunile de status pentru dropdown
        private ObservableCollection<string> _statusOptions;

        // CAMPURI PROPRIETATI

        // Comanda selectata
        private Order _selectedOrder;

        // Statusul selectat pentru filtru
        private string _selectedStatusFilter;

        // Noul status pentru actualizare
        private string _newStatus;

        // Flag incarcare
        private bool _isLoading;

        // Mesaj eroare
        private string _errorMessage;

        // CONSTRUCTOR
        public OrderManagementViewModel(
            CurrentUserStore currentUserStore,
            NavigationStore navigationStore)
        {
            // Salvare dependinte
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare colectii
            _orders = new ObservableCollection<Order>();
            _orderDetails = new ObservableCollection<OrderDetail>();
            _allOrders = new List<Order>();

            // Optiuni status pentru filtre si actualizare
            _statusOptions = new ObservableCollection<string>
            {
                "All",
                "Pending",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled"
            };

            // Initializare comenzi
            UpdateStatusCommand = new RelayCommand(ExecuteUpdateStatus, CanExecuteUpdateStatus);
            CancelOrderCommand = new RelayCommand(ExecuteCancelOrder, CanExecuteCancelOrder);
            ViewDetailsCommand = new RelayCommand(ExecuteViewDetails, CanExecuteViewDetails);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
            ProcessOrderCommand = new RelayCommand(ExecuteProcessOrder, CanExecuteProcessOrder);
            ShipOrderCommand = new RelayCommand(ExecuteShipOrder, CanExecuteShipOrder);
            MarkDeliveredCommand = new RelayCommand(ExecuteMarkDelivered, CanExecuteMarkDelivered);

            // Filtru default: toate comenzile
            _selectedStatusFilter = "All";

            // Incarcare date
            LoadData();
        }

        // PROPRIETATI - Colectii

        // Orders - Lista de comenzi afisata
        //
        // Poate fi filtrata dupa status
        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        // OrderDetails - Detaliile comenzii selectate
        //
        // RELATIA ONE-TO-MANY:
        // O comanda are mai multe OrderDetails
        // Fiecare OrderDetail contine un produs cu cantitate si pret
        //
        // BINDING:
        // <ListBox ItemsSource="{Binding OrderDetails}">
        //     <ListBox.ItemTemplate>
        //         <DataTemplate>
        //             <StackPanel>
        //                 <TextBlock Text="{Binding Product.ProductName}" FontWeight="Bold" />
        //                 <TextBlock Text="{Binding LineDescription}" />
        //                 <TextBlock Text="{Binding SubtotalFormatted}" />
        //             </StackPanel>
        //         </DataTemplate>
        //     </ListBox.ItemTemplate>
        // </ListBox>
        public ObservableCollection<OrderDetail> OrderDetails
        {
            get => _orderDetails;
            set => SetProperty(ref _orderDetails, value);
        }

        // StatusOptions - Optiunile pentru dropdown de filtrare
        public ObservableCollection<string> StatusOptions
        {
            get => _statusOptions;
            set => SetProperty(ref _statusOptions, value);
        }

        // PROPRIETATI - Selectie

        // SelectedOrder - Comanda selectata in lista
        //
        // Cand se selecteaza o comanda:
        // 1. Se incarca OrderDetails
        // 2. Se actualizeaza proprietatile dependente
        // 3. Se reverifica CanExecute pentru comenzi
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    // Incarca detaliile comenzii selectate
                    LoadOrderDetails();

                    // Notifica proprietatile dependente
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(SelectedOrderNumber));
                    OnPropertyChanged(nameof(SelectedOrderCustomer));
                    OnPropertyChanged(nameof(SelectedOrderTotal));
                    OnPropertyChanged(nameof(SelectedOrderStatus));
                    OnPropertyChanged(nameof(CanChangeStatus));

                    // Reverifica comenzile
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // SelectedStatusFilter - Statusul selectat pentru filtrare
        //
        // "All" = toate comenzile
        // Altfel = doar comenzile cu acel status
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        // NewStatus - Noul status pentru actualizare
        //
        // Selectat din dropdown la update status
        public string NewStatus
        {
            get => _newStatus;
            set => SetProperty(ref _newStatus, value);
        }

        // PROPRIETATI - Stare

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

        // PROPRIETATI CALCULATE - Despre selectie

        // HasSelection - Este selectata o comanda?
        public bool HasSelection => SelectedOrder != null;

        // SelectedOrderNumber - Numarul comenzii selectate
        public string SelectedOrderNumber => SelectedOrder?.OrderNumber ?? "No selection";

        // SelectedOrderCustomer - Clientul comenzii selectate
        //
        // DEMONSTRARE RELATIE MANY-TO-ONE:
        // SelectedOrder.Customer.FullName
        public string SelectedOrderCustomer => SelectedOrder?.Customer?.FullName ?? "N/A";

        // SelectedOrderTotal - Totalul comenzii selectate
        public string SelectedOrderTotal => SelectedOrder?.TotalFormatted ?? "N/A";

        // SelectedOrderStatus - Statusul comenzii selectate
        public string SelectedOrderStatus => SelectedOrder?.OrderStatus ?? "N/A";

        // CanChangeStatus - Se poate schimba statusul?
        //
        // Nu se poate schimba daca e deja Delivered sau Cancelled
        public bool CanChangeStatus
        {
            get
            {
                if (SelectedOrder == null)
                    return false;

                return SelectedOrder.OrderStatus != "Delivered" &&
                       SelectedOrder.OrderStatus != "Cancelled";
            }
        }

        // PROPRIETATI CALCULATE - Statistici

        // TotalOrdersCount - Total comenzi (din filtru curent)
        public int TotalOrdersCount => Orders?.Count ?? 0;

        // PendingOrdersCount - Comenzi pending
        public int PendingOrdersCount => _allOrders?.Count(o => o.OrderStatus == "Pending") ?? 0;

        // TodayOrdersCount - Comenzi de azi
        public int TodayOrdersCount => _allOrders?.Count(o => o.OrderDate.Date == DateTime.Today) ?? 0;

        // TotalRevenue - Venitul total
        public decimal TotalRevenue => _allOrders?.Sum(o => o.TotalAmount) ?? 0;

        // TotalRevenueFormatted - Venitul formatat
        public string TotalRevenueFormatted => $"{TotalRevenue:N2} RON";

        // COMENZI

        // UpdateStatusCommand - Actualizeaza statusul comenzii
        public ICommand UpdateStatusCommand { get; }

        // CancelOrderCommand - Anuleaza comanda
        public ICommand CancelOrderCommand { get; }

        // ViewDetailsCommand - Vezi detalii comanda
        public ICommand ViewDetailsCommand { get; }

        // RefreshCommand - Reincarca datele
        public ICommand RefreshCommand { get; }

        // ProcessOrderCommand - Trece comanda in Processing
        //
        // Shortcut pentru Pending -> Processing
        public ICommand ProcessOrderCommand { get; }

        // ShipOrderCommand - Trece comanda in Shipped
        //
        // Shortcut pentru Processing -> Shipped
        public ICommand ShipOrderCommand { get; }

        // MarkDeliveredCommand - Marcheaza ca livrat
        //
        // Shortcut pentru Shipped -> Delivered
        public ICommand MarkDeliveredCommand { get; }

        // METODE INCARCARE DATE

        // LoadData - Incarca comenzile
        private void LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Aici am apela serviciul de comenzi
                // var orders = _orderService.GetAllOrders();
                // Pentru demonstratie, folosim lista goala
                _allOrders = new List<Order>();

                // Aplicam filtrul
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load orders: " + ex.Message;
            }
            finally
            {
                IsLoading = false;

                // Notificam statisticile
                OnPropertyChanged(nameof(TotalOrdersCount));
                OnPropertyChanged(nameof(PendingOrdersCount));
                OnPropertyChanged(nameof(TodayOrdersCount));
                OnPropertyChanged(nameof(TotalRevenue));
                OnPropertyChanged(nameof(TotalRevenueFormatted));
            }
        }

        // LoadOrderDetails - Incarca detaliile pentru comanda selectata
        //
        // DEMONSTRARE RELATIE ONE-TO-MANY:
        // SelectedOrder.OrderDetails e colectia de linii
        private void LoadOrderDetails()
        {
            OrderDetails.Clear();

            if (SelectedOrder?.OrderDetails == null)
                return;

            // RELATIE ONE-TO-MANY:
            // O comanda are multe OrderDetails
            // Iteram prin ele si le adaugam in colectia pentru UI
            foreach (var detail in SelectedOrder.OrderDetails)
            {
                OrderDetails.Add(detail);
            }
        }

        // ApplyFilter - Aplica filtrul de status
        private void ApplyFilter()
        {
            Orders.Clear();

            IEnumerable<Order> filtered = _allOrders;

            // Filtram dupa status (daca nu e "All")
            if (SelectedStatusFilter != "All" && !string.IsNullOrEmpty(SelectedStatusFilter))
            {
                filtered = filtered.Where(o => o.OrderStatus == SelectedStatusFilter);
            }

            // Sortam descrescator dupa data (cele mai noi primele)
            filtered = filtered.OrderByDescending(o => o.OrderDate);

            foreach (var order in filtered)
            {
                Orders.Add(order);
            }

            OnPropertyChanged(nameof(TotalOrdersCount));
        }

        // IMPLEMENTARI COMENZI

        // ExecuteUpdateStatus - Actualizeaza statusul
        private void ExecuteUpdateStatus(object parameter)
        {
            if (SelectedOrder == null || string.IsNullOrEmpty(NewStatus))
                return;

            if (NewStatus == "All") // Nu e valid pentru actualizare
                return;

            try
            {
                // Actualizam statusul
                SelectedOrder.OrderStatus = NewStatus;

                // In productie, am salva in DB
                // _orderService.UpdateOrder(SelectedOrder);

                // Notificam UI-ul
                OnPropertyChanged(nameof(SelectedOrderStatus));
                OnPropertyChanged(nameof(CanChangeStatus));

                // Reaplicam filtrul (comanda poate disparea din filtru)
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to update status: " + ex.Message;
            }
        }

        // CanExecuteUpdateStatus - Se poate actualiza?
        private bool CanExecuteUpdateStatus(object parameter)
        {
            return SelectedOrder != null &&
                   !IsLoading &&
                   CanChangeStatus &&
                   !string.IsNullOrEmpty(NewStatus) &&
                   NewStatus != "All";
        }

        // ExecuteCancelOrder - Anuleaza comanda
        private void ExecuteCancelOrder(object parameter)
        {
            if (SelectedOrder == null)
                return;

            if (!SelectedOrder.CanBeCancelled)
            {
                ErrorMessage = "This order cannot be cancelled (already shipped or delivered)";
                return;
            }

            try
            {
                // Actualizam statusul
                SelectedOrder.OrderStatus = "Cancelled";

                // In productie: restock produsele, notifica clientul, etc.

                // Notificam UI-ul
                OnPropertyChanged(nameof(SelectedOrderStatus));
                OnPropertyChanged(nameof(CanChangeStatus));
                OnPropertyChanged(nameof(PendingOrdersCount));

                ApplyFilter();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to cancel order: " + ex.Message;
            }
        }

        // CanExecuteCancelOrder - Se poate anula?
        private bool CanExecuteCancelOrder(object parameter)
        {
            return SelectedOrder != null &&
                   !IsLoading &&
                   SelectedOrder.CanBeCancelled;
        }

        // ExecuteViewDetails - Afiseaza detalii complete
        private void ExecuteViewDetails(object parameter)
        {
            // Navigheaza la un ecran de detalii sau deschide un dialog
            // _navigationStore.CurrentViewModel = new OrderDetailsViewModel(SelectedOrder, ...);
        }

        // CanExecuteViewDetails - Se poate vedea detalii?
        private bool CanExecuteViewDetails(object parameter)
        {
            return SelectedOrder != null;
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

        // ExecuteProcessOrder - Trece comanda in Processing
        private void ExecuteProcessOrder(object parameter)
        {
            if (SelectedOrder == null)
                return;

            SelectedOrder.OrderStatus = "Processing";
            OnPropertyChanged(nameof(SelectedOrderStatus));
            ApplyFilter();
        }

        // CanExecuteProcessOrder - Se poate procesa?
        private bool CanExecuteProcessOrder(object parameter)
        {
            return SelectedOrder != null &&
                   SelectedOrder.OrderStatus == "Pending";
        }

        // ExecuteShipOrder - Trece comanda in Shipped
        private void ExecuteShipOrder(object parameter)
        {
            if (SelectedOrder == null)
                return;

            SelectedOrder.OrderStatus = "Shipped";
            OnPropertyChanged(nameof(SelectedOrderStatus));
            ApplyFilter();
        }

        // CanExecuteShipOrder - Se poate expedia?
        private bool CanExecuteShipOrder(object parameter)
        {
            return SelectedOrder != null &&
                   SelectedOrder.OrderStatus == "Processing";
        }

        // ExecuteMarkDelivered - Marcheaza ca livrat
        private void ExecuteMarkDelivered(object parameter)
        {
            if (SelectedOrder == null)
                return;

            SelectedOrder.OrderStatus = "Delivered";
            OnPropertyChanged(nameof(SelectedOrderStatus));
            OnPropertyChanged(nameof(CanChangeStatus));
            ApplyFilter();
        }

        // CanExecuteMarkDelivered - Se poate marca ca livrat?
        private bool CanExecuteMarkDelivered(object parameter)
        {
            return SelectedOrder != null &&
                   SelectedOrder.OrderStatus == "Shipped";
        }
    }
}
