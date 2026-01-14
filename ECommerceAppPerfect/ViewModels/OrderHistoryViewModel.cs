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
    // CLASA ORDERHISTORYVIEWMODEL - Istoricul comenzilor clientului
    //
    // CE ESTE ACEST VIEWMODEL?
    // Afiseaza istoricul comenzilor pentru clientul curent autentificat
    // Permite vizualizarea detaliilor fiecarei comenzi
    //
    // DIFERENTA FATA DE ORDERMANAGEMENTVIEWMODEL:
    // - OrderManagementViewModel: Pentru StoreOwner, vede TOATE comenzile
    // - OrderHistoryViewModel: Pentru Customer, vede doar comenzile PROPRII
    //
    // FUNCTIONALITATI:
    // 1. Lista comenzilor clientului (sortate descrescator dupa data)
    // 2. Filtrare dupa status (optional)
    // 3. Vizualizare detalii comanda selectata
    // 4. Reordonare (Add to cart items from previous order)
    // 5. Anulare comenzi (doar daca e posibil)
    //
    // DEMONSTRARE RELATII:
    // - Customer -> Orders (One-to-Many): Un client are multe comenzi
    // - Order -> OrderDetails (One-to-Many): O comanda are multe linii
    // - OrderDetail -> Product (Many-to-One): Fiecare linie refera un produs
    //
    // QUERY FOLOSIT:
    // var orders = context.Orders
    //     .Where(o => o.CustomerID == currentUserId)
    //     .Include(o => o.OrderDetails)
    //         .ThenInclude(od => od.Product)
    //     .OrderByDescending(o => o.OrderDate)
    //     .ToList();
    //
    // UI TIPIC:
    // +----------------------------------------------------------------+
    // | COMENZILE MELE                                                |
    // +----------------------------------------------------------------+
    // | #00001 | 15/01/2024 | $999.99 | Delivered | [View] [Reorder] |
    // | #00002 | 10/01/2024 | $299.99 | Shipped   | [View]           |
    // | #00003 | 05/01/2024 | $149.99 | Pending   | [View] [Cancel]  |
    // +----------------------------------------------------------------+
    // |                    DETALII COMANDA #00001                    |
    // +----------------------------------------------------------------+
    // | Product          | Qty | Price    | Subtotal                 |
    // | iPhone 15        | 1   | $999.99  | $999.99                  |
    // +----------------------------------------------------------------+
    public class OrderHistoryViewModel : ViewModelBase
    {
        // STORES

        // _currentUserStore - Informatii despre clientul curent
        //
        // Folosim CurrentUserId pentru a filtra comenzile
        // Doar comenzile acestui client vor fi afisate
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare
        private readonly NavigationStore _navigationStore;

        // COLECTII

        // _allOrders - Lista completa de comenzi ale clientului
        private List<Order> _allOrders;

        // _orders - Lista filtrata expusa catre UI
        private ObservableCollection<Order> _orders;

        // _orderDetails - Detaliile comenzii selectate
        //
        // RELATIE ONE-TO-MANY demonstrata
        private ObservableCollection<OrderDetail> _orderDetails;

        // CAMPURI PROPRIETATI

        // Comanda selectata pentru vizualizare detalii
        private Order _selectedOrder;

        // Statusul pentru filtrare
        private string _selectedStatusFilter;

        // Flag incarcare
        private bool _isLoading;

        // Mesaj eroare
        private string _errorMessage;

        // CONSTRUCTOR
        public OrderHistoryViewModel(
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

            // Filtru default: toate comenzile
            _selectedStatusFilter = "All";

            // Initializare comenzi
            ViewOrderDetailsCommand = new RelayCommand(ExecuteViewOrderDetails, CanExecuteViewOrderDetails);
            CancelOrderCommand = new RelayCommand(ExecuteCancelOrder, CanExecuteCancelOrder);
            ReorderCommand = new RelayCommand(ExecuteReorder, CanExecuteReorder);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
            TrackOrderCommand = new RelayCommand(ExecuteTrackOrder, CanExecuteTrackOrder);

            // Incarcare date
            LoadData();
        }

        // PROPRIETATI - Colectii

        // Orders - Lista de comenzi ale clientului curent
        //
        // FILTRATA dupa CustomerID = CurrentUserId
        // Demonstreaza relatia Customer -> Orders (One-to-Many)
        //
        // BINDING:
        // <ListView ItemsSource="{Binding Orders}" SelectedItem="{Binding SelectedOrder}">
        //     <ListView.View>
        //         <GridView>
        //             <GridViewColumn Header="Order #" DisplayMemberBinding="{Binding OrderNumber}" />
        //             <GridViewColumn Header="Date" DisplayMemberBinding="{Binding OrderDate, StringFormat='{}{0:dd/MM/yyyy}'}" />
        //             <GridViewColumn Header="Total" DisplayMemberBinding="{Binding TotalFormatted}" />
        //             <GridViewColumn Header="Status" DisplayMemberBinding="{Binding OrderStatus}" />
        //         </GridView>
        //     </ListView.View>
        // </ListView>
        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        // OrderDetails - Detaliile comenzii selectate
        //
        // RELATIE ONE-TO-MANY: Order -> OrderDetails
        // Fiecare OrderDetail contine informatii despre un produs comandat
        public ObservableCollection<OrderDetail> OrderDetails
        {
            get => _orderDetails;
            set => SetProperty(ref _orderDetails, value);
        }

        // PROPRIETATI - Selectie

        // SelectedOrder - Comanda selectata
        //
        // La selectare, se incarca detaliile comenzii
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    // Incarca detaliile comenzii
                    LoadOrderDetails();

                    // Notifica proprietatile dependente
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(SelectedOrderNumber));
                    OnPropertyChanged(nameof(SelectedOrderDate));
                    OnPropertyChanged(nameof(SelectedOrderTotal));
                    OnPropertyChanged(nameof(SelectedOrderStatus));
                    OnPropertyChanged(nameof(SelectedOrderStatusColor));
                    OnPropertyChanged(nameof(SelectedOrderCanBeCancelled));
                    OnPropertyChanged(nameof(SelectedOrderShippingAddress));
                    OnPropertyChanged(nameof(SelectedOrderPaymentMethod));

                    // Reverifica comenzile
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // SelectedStatusFilter - Filtrul de status
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

        // SelectedOrderDate - Data comenzii selectate
        public string SelectedOrderDate => SelectedOrder?.OrderDate.ToString("dd MMMM yyyy, HH:mm") ?? "N/A";

        // SelectedOrderTotal - Totalul comenzii selectate
        public string SelectedOrderTotal => SelectedOrder?.TotalFormatted ?? "N/A";

        // SelectedOrderStatus - Statusul comenzii selectate
        public string SelectedOrderStatus => SelectedOrder?.OrderStatus ?? "N/A";

        // SelectedOrderStatusColor - Culoarea statusului
        public string SelectedOrderStatusColor => SelectedOrder?.StatusColor ?? "#757575";

        // SelectedOrderCanBeCancelled - Poate fi anulata?
        public bool SelectedOrderCanBeCancelled => SelectedOrder?.CanBeCancelled ?? false;

        // SelectedOrderShippingAddress - Adresa de livrare
        public string SelectedOrderShippingAddress => SelectedOrder?.ShippingAddress ?? "N/A";

        // SelectedOrderPaymentMethod - Metoda de plata
        public string SelectedOrderPaymentMethod => SelectedOrder?.PaymentMethod ?? "N/A";

        // SelectedOrderItemCount - Cate linii are comanda
        //
        // Demonstreaza accesarea colectiei One-to-Many
        public int SelectedOrderItemCount => SelectedOrder?.OrderDetails?.Count ?? 0;

        // PROPRIETATI CALCULATE - Statistici

        // TotalOrdersCount - Cate comenzi are clientul
        public int TotalOrdersCount => _allOrders?.Count ?? 0;

        // DisplayedOrdersCount - Cate comenzi sunt afisate (dupa filtru)
        public int DisplayedOrdersCount => Orders?.Count ?? 0;

        // TotalSpent - Cat a cheltuit clientul in total
        public decimal TotalSpent => _allOrders?.Sum(o => o.TotalAmount) ?? 0;

        // TotalSpentFormatted - Suma formatata
        public string TotalSpentFormatted => $"{TotalSpent:N2} RON";

        // HasOrders - Are clientul comenzi?
        public bool HasOrders => TotalOrdersCount > 0;

        // CustomerName - Numele clientului
        public string CustomerName => _currentUserStore.CurrentUserFullName;

        // COMENZI

        // ViewOrderDetailsCommand - Vezi detalii comanda
        public ICommand ViewOrderDetailsCommand { get; }

        // CancelOrderCommand - Anuleaza comanda
        public ICommand CancelOrderCommand { get; }

        // ReorderCommand - Recomanda aceleasi produse
        //
        // Adauga in cos toate produsele din comanda selectata
        public ICommand ReorderCommand { get; }

        // RefreshCommand - Reincarca datele
        public ICommand RefreshCommand { get; }

        // TrackOrderCommand - Urmareste comanda (tracking)
        public ICommand TrackOrderCommand { get; }

        // METODE INCARCARE DATE

        // LoadData - Incarca comenzile clientului curent
        private void LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // QUERY DEMONSTRATIV:
                // Incarcam comenzile pentru clientul curent
                //
                // var orders = context.Orders
                //     .Where(o => o.CustomerID == _currentUserStore.CurrentUserId)
                //     .Include(o => o.OrderDetails)
                //         .ThenInclude(od => od.Product)
                //     .OrderByDescending(o => o.OrderDate)
                //     .ToList();
                //
                // RELATIA DEMONSTRATA:
                // User (Customer) -> Orders (One-to-Many)
                // Filtram Orders dupa CustomerID

                // Pentru demonstratie, lista goala
                // In productie: _allOrders = _orderService.GetOrdersByCustomer(_currentUserStore.CurrentUserId);
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
                OnPropertyChanged(nameof(TotalSpent));
                OnPropertyChanged(nameof(TotalSpentFormatted));
                OnPropertyChanged(nameof(HasOrders));
            }
        }

        // LoadOrderDetails - Incarca detaliile pentru comanda selectata
        //
        // DEMONSTRARE RELATIE ONE-TO-MANY:
        // SelectedOrder.OrderDetails e colectia de linii
        // Fiecare OrderDetail.Product e produsul comandat
        private void LoadOrderDetails()
        {
            OrderDetails.Clear();

            if (SelectedOrder?.OrderDetails == null)
                return;

            // ITERARE PRIN RELATIA ONE-TO-MANY
            // Order (1) -> OrderDetails (Many)
            foreach (var detail in SelectedOrder.OrderDetails)
            {
                OrderDetails.Add(detail);
            }

            OnPropertyChanged(nameof(SelectedOrderItemCount));
        }

        // ApplyFilter - Aplica filtrul de status
        private void ApplyFilter()
        {
            Orders.Clear();

            IEnumerable<Order> filtered = _allOrders;

            // Filtram dupa status
            if (SelectedStatusFilter != "All" && !string.IsNullOrEmpty(SelectedStatusFilter))
            {
                filtered = filtered.Where(o => o.OrderStatus == SelectedStatusFilter);
            }

            // Sortam descrescator dupa data
            filtered = filtered.OrderByDescending(o => o.OrderDate);

            foreach (var order in filtered)
            {
                Orders.Add(order);
            }

            OnPropertyChanged(nameof(DisplayedOrdersCount));
        }

        // IMPLEMENTARI COMENZI

        // ExecuteViewOrderDetails - Afiseaza detalii complete
        private void ExecuteViewOrderDetails(object parameter)
        {
            var order = parameter as Order ?? SelectedOrder;

            if (order == null)
                return;

            // Selectam comanda pentru a incarca detaliile
            SelectedOrder = order;

            // Sau navigam la un ecran separat de detalii
            // _navigationStore.CurrentViewModel = new OrderDetailsViewModel(order, ...);
        }

        // CanExecuteViewOrderDetails - Se poate vedea detalii?
        private bool CanExecuteViewOrderDetails(object parameter)
        {
            return (parameter as Order ?? SelectedOrder) != null;
        }

        // ExecuteCancelOrder - Anuleaza comanda
        private void ExecuteCancelOrder(object parameter)
        {
            var order = parameter as Order ?? SelectedOrder;

            if (order == null)
                return;

            if (!order.CanBeCancelled)
            {
                ErrorMessage = "This order cannot be cancelled";
                return;
            }

            try
            {
                // Actualizam statusul
                order.OrderStatus = "Cancelled";

                // In productie: _orderService.UpdateOrder(order);

                // Notificam UI-ul
                OnPropertyChanged(nameof(SelectedOrderStatus));
                OnPropertyChanged(nameof(SelectedOrderCanBeCancelled));

                // Reaplicam filtrul
                ApplyFilter();

                // Feedback
                // ToastService.Show("Order cancelled successfully");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to cancel order: " + ex.Message;
            }
        }

        // CanExecuteCancelOrder - Se poate anula?
        private bool CanExecuteCancelOrder(object parameter)
        {
            var order = parameter as Order ?? SelectedOrder;
            return order?.CanBeCancelled ?? false;
        }

        // ExecuteReorder - Recomanda produsele
        //
        // Adauga in cos toate produsele din comanda selectata
        private void ExecuteReorder(object parameter)
        {
            var order = parameter as Order ?? SelectedOrder;

            if (order?.OrderDetails == null)
                return;

            try
            {
                // Adaugam fiecare produs din comanda in cos
                foreach (var detail in order.OrderDetails)
                {
                    // In productie:
                    // _cartStore.AddItem(detail.Product, detail.Quantity);
                    System.Diagnostics.Debug.WriteLine($"Added {detail.Product?.ProductName} x{detail.Quantity} to cart");
                }

                // Navigam la cos
                // _navigationStore.CurrentViewModel = new ShoppingCartViewModel(...);

                // Sau afisam mesaj
                // ToastService.Show("Products added to cart!");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to add products to cart: " + ex.Message;
            }
        }

        // CanExecuteReorder - Se poate recomanda?
        private bool CanExecuteReorder(object parameter)
        {
            var order = parameter as Order ?? SelectedOrder;
            return order?.OrderDetails?.Count > 0;
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

        // ExecuteTrackOrder - Urmareste comanda
        private void ExecuteTrackOrder(object parameter)
        {
            var order = parameter as Order ?? SelectedOrder;

            if (order == null)
                return;

            // In productie: deschide pagina de tracking
            // sau arata informatiile de tracking inline
            System.Diagnostics.Debug.WriteLine($"Tracking order {order.OrderNumber}...");
        }

        // CanExecuteTrackOrder - Se poate urmari?
        private bool CanExecuteTrackOrder(object parameter)
        {
            var order = parameter as Order ?? SelectedOrder;

            // Doar comenzile expediate sau livrate pot fi urmarite
            return order != null &&
                   (order.OrderStatus == "Shipped" || order.OrderStatus == "Delivered");
        }
    }
}
