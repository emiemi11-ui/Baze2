using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ECommerceAppPerfect.Commands;
using ECommerceAppPerfect.Models;
using ECommerceAppPerfect.Stores;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA SHOPPINGCARTVIEWMODEL - Gestionarea cosului de cumparaturi
    //
    // CE ESTE ACEST VIEWMODEL?
    // Gestioneaza cosul de cumparaturi al clientului:
    // - Afiseaza produsele adaugate in cos
    // - Permite modificarea cantitatilor
    // - Permite stergerea produselor din cos
    // - Calculeaza totalul
    // - Initiaza procesul de checkout
    //
    // MODEL DE DATE - CartItem:
    // Pentru ca nu avem o entitate CartItem in Model, vom folosi
    // o clasa interna sau OrderDetail adaptat pentru cos
    // CartItem contine: Product, Quantity, UnitPrice, Subtotal
    //
    // UI TIPIC:
    // +----------------------------------------------------------------+
    // | Product              | Price    | Qty  | Subtotal    | Actions|
    // +----------------------------------------------------------------+
    // | iPhone 15            | $999.99  | [2]  | $1,999.98   |  [X]   |
    // | MacBook Pro          | $1999.99 | [1]  | $1,999.99   |  [X]   |
    // +----------------------------------------------------------------+
    // |                                  Total: $3,999.97            |
    // |                                  [Continue Shopping] [Checkout]|
    // +----------------------------------------------------------------+
    //
    // NOTA DESPRE PERSISTENTA:
    // Cosul poate fi:
    // 1. In memorie (se pierde la logout) - implementare curenta
    // 2. In baza de date (persista intre sesiuni)
    // 3. In localStorage (pentru web)
    //
    // Pentru simplitate, folosim un approach in memorie
    // cu un "CartStore" care tine datele
    public class ShoppingCartViewModel : ViewModelBase
    {
        // CLASE INTERNE

        // CartItem - Reprezinta un produs in cos
        //
        // Similar cu OrderDetail, dar pentru cosul nepersistent
        // Contine toate informatiile necesare pentru afisare
        public class CartItem : ViewModelBase
        {
            private int _quantity;
            private decimal _unitPrice;

            // Product - Produsul din cos
            public Product Product { get; set; }

            // Quantity - Cantitatea selectata
            //
            // Se poate modifica din UI
            // La modificare, se recalculeaza Subtotal
            public int Quantity
            {
                get => _quantity;
                set
                {
                    if (SetProperty(ref _quantity, Math.Max(1, value)))
                    {
                        OnPropertyChanged(nameof(Subtotal));
                        OnPropertyChanged(nameof(SubtotalFormatted));
                    }
                }
            }

            // UnitPrice - Pretul per bucata
            //
            // Copiat de la Product.Price la adaugare in cos
            // Ramane fix chiar daca pretul produsului se schimba
            public decimal UnitPrice
            {
                get => _unitPrice;
                set => SetProperty(ref _unitPrice, value);
            }

            // Subtotal - Totalul pentru acest item
            //
            // Quantity * UnitPrice
            public decimal Subtotal => Quantity * UnitPrice;

            // SubtotalFormatted - Subtotalul formatat
            public string SubtotalFormatted => $"{Subtotal:N2} RON";

            // UnitPriceFormatted - Pretul unitar formatat
            public string UnitPriceFormatted => $"{UnitPrice:N2} RON";

            // ProductName - Numele produsului (shortcut)
            public string ProductName => Product?.ProductName ?? "Unknown Product";

            // ImageURL - URL-ul imaginii (shortcut)
            public string ImageURL => Product?.ImageURL;

            // IsInStock - Este produsul in stoc?
            //
            // Verifica daca mai e stoc pentru cantitatea ceruta
            public bool IsInStock => Product?.Inventory?.CanFulfill(Quantity) ?? false;

            // MaxQuantity - Cantitatea maxima disponibila
            public int MaxQuantity => Product?.Inventory?.StockQuantity ?? 0;
        }

        // STORES

        // _currentUserStore - Informatii despre client
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare
        private readonly NavigationStore _navigationStore;

        // COLECTII

        // _cartItems - Lista de produse in cos
        //
        // ObservableCollection pentru actualizare automata UI
        private ObservableCollection<CartItem> _cartItems;

        // CAMPURI PROPRIETATI

        // Itemul selectat (pentru operatii)
        private CartItem _selectedItem;

        // Flag incarcare
        private bool _isLoading;

        // Mesaj eroare
        private string _errorMessage;

        // Adresa de livrare
        private string _shippingAddress;

        // Metoda de plata
        private string _paymentMethod;

        // CONSTRUCTOR
        public ShoppingCartViewModel(
            CurrentUserStore currentUserStore,
            NavigationStore navigationStore)
        {
            // Salvare dependinte
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare colectii
            _cartItems = new ObservableCollection<CartItem>();

            // Adresa default din profil user
            _shippingAddress = _currentUserStore.CurrentUser?.Address ?? "";

            // Initializare comenzi
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem, CanExecuteRemoveItem);
            UpdateQuantityCommand = new RelayCommand(ExecuteUpdateQuantity);
            IncreaseQuantityCommand = new RelayCommand(ExecuteIncreaseQuantity, CanExecuteIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand(ExecuteDecreaseQuantity, CanExecuteDecreaseQuantity);
            ClearCartCommand = new RelayCommand(ExecuteClearCart, CanExecuteClearCart);
            CheckoutCommand = new RelayCommand(ExecuteCheckout, CanExecuteCheckout);
            ContinueShoppingCommand = new RelayCommand(ExecuteContinueShopping);

            // Abonam la schimbari in colectie pentru a recalcula totalul
            _cartItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(TotalFormatted));
                OnPropertyChanged(nameof(ItemCount));
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(IsEmpty));
            };
        }

        // PROPRIETATI - Colectii

        // CartItems - Lista de produse din cos
        //
        // BINDING:
        // <ItemsControl ItemsSource="{Binding CartItems}">
        //     <ItemsControl.ItemTemplate>
        //         <DataTemplate>
        //             <Grid>
        //                 <TextBlock Text="{Binding ProductName}" />
        //                 <TextBlock Text="{Binding UnitPriceFormatted}" />
        //                 <TextBox Text="{Binding Quantity}" />
        //                 <TextBlock Text="{Binding SubtotalFormatted}" />
        //                 <Button Content="X" Command="{Binding RemoveItemCommand}"
        //                         CommandParameter="{Binding}" />
        //             </Grid>
        //         </DataTemplate>
        //     </ItemsControl.ItemTemplate>
        // </ItemsControl>
        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        // PROPRIETATI - Selectie

        // SelectedItem - Itemul selectat
        public CartItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // PROPRIETATI - Totale

        // Total - Suma totala a cosului
        //
        // Suma Subtotal-urilor din toate itemurile
        public decimal Total => CartItems?.Sum(item => item.Subtotal) ?? 0;

        // TotalFormatted - Totalul formatat
        //
        // BINDING:
        // <TextBlock Text="{Binding TotalFormatted}" Style="{StaticResource TotalPrice}" />
        public string TotalFormatted => $"{Total:N2} RON";

        // ItemCount - Cate tipuri de produse in cos
        //
        // Numarul de linii (nu cantitatea totala)
        public int ItemCount => CartItems?.Count ?? 0;

        // TotalQuantity - Cate bucati in total
        //
        // Suma cantitatilor din toate itemurile
        public int TotalQuantity => CartItems?.Sum(item => item.Quantity) ?? 0;

        // PROPRIETATI - Stare

        // HasItems - Exista produse in cos?
        public bool HasItems => ItemCount > 0;

        // IsEmpty - Este cosul gol?
        public bool IsEmpty => !HasItems;

        // IsLoading - Se proceseaza ceva?
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

        // PROPRIETATI - Checkout

        // ShippingAddress - Adresa de livrare
        //
        // BINDING:
        // <TextBox Text="{Binding ShippingAddress}" Placeholder="Enter shipping address" />
        public string ShippingAddress
        {
            get => _shippingAddress;
            set => SetProperty(ref _shippingAddress, value);
        }

        // PaymentMethod - Metoda de plata selectata
        //
        // Valori: "Credit Card", "PayPal", "Cash on Delivery", etc.
        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        // CanCheckout - Se poate face checkout?
        //
        // Verifica conditiile necesare pentru plasarea comenzii
        public bool CanCheckout
        {
            get
            {
                // Trebuie sa avem produse in cos
                if (!HasItems)
                    return false;

                // Trebuie sa avem adresa de livrare
                if (string.IsNullOrWhiteSpace(ShippingAddress))
                    return false;

                // Toate produsele trebuie sa fie in stoc
                if (CartItems.Any(item => !item.IsInStock))
                    return false;

                return true;
            }
        }

        // COMENZI

        // RemoveItemCommand - Sterge un item din cos
        //
        // PARAMETRU: CartItem de sters
        public ICommand RemoveItemCommand { get; }

        // UpdateQuantityCommand - Actualizeaza cantitatea
        //
        // PARAMETRU: Noua cantitate
        public ICommand UpdateQuantityCommand { get; }

        // IncreaseQuantityCommand - Creste cantitatea cu 1
        public ICommand IncreaseQuantityCommand { get; }

        // DecreaseQuantityCommand - Scade cantitatea cu 1
        public ICommand DecreaseQuantityCommand { get; }

        // ClearCartCommand - Goleste cosul
        public ICommand ClearCartCommand { get; }

        // CheckoutCommand - Plaseaza comanda
        public ICommand CheckoutCommand { get; }

        // ContinueShoppingCommand - Inapoi la magazin
        public ICommand ContinueShoppingCommand { get; }

        // METODE PUBLICE - Pentru adaugare din alte ViewModels

        // AddItem - Adauga un produs in cos
        //
        // Apelat de CustomerShopViewModel cand utilizatorul apasa "Add to Cart"
        //
        // PARAMETRI:
        // - product: Produsul de adaugat
        // - quantity: Cantitatea (default 1)
        public void AddItem(Product product, int quantity = 1)
        {
            if (product == null)
                return;

            // Verificam daca produsul e deja in cos
            var existingItem = CartItems.FirstOrDefault(item => item.Product?.ProductID == product.ProductID);

            if (existingItem != null)
            {
                // Produsul e deja in cos - marim cantitatea
                existingItem.Quantity += quantity;
            }
            else
            {
                // Produs nou - il adaugam
                var newItem = new CartItem
                {
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };

                CartItems.Add(newItem);
            }

            // Notificam totalul
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(TotalFormatted));
        }

        // IMPLEMENTARI COMENZI

        // ExecuteRemoveItem - Sterge item din cos
        private void ExecuteRemoveItem(object parameter)
        {
            var item = parameter as CartItem ?? SelectedItem;

            if (item == null)
                return;

            CartItems.Remove(item);

            if (SelectedItem == item)
            {
                SelectedItem = null;
            }
        }

        // CanExecuteRemoveItem - Se poate sterge?
        private bool CanExecuteRemoveItem(object parameter)
        {
            return (parameter as CartItem ?? SelectedItem) != null;
        }

        // ExecuteUpdateQuantity - Actualizeaza cantitatea
        private void ExecuteUpdateQuantity(object parameter)
        {
            // Cantitatea se actualizeaza direct prin binding
            // Aceasta comanda poate fi folosita pentru validare
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(TotalFormatted));
            OnPropertyChanged(nameof(TotalQuantity));
        }

        // ExecuteIncreaseQuantity - Creste cantitatea
        private void ExecuteIncreaseQuantity(object parameter)
        {
            var item = parameter as CartItem ?? SelectedItem;

            if (item == null)
                return;

            // Verificam stocul
            if (item.Product?.Inventory?.CanFulfill(item.Quantity + 1) == true)
            {
                item.Quantity++;
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(TotalFormatted));
                OnPropertyChanged(nameof(TotalQuantity));
            }
            else
            {
                ErrorMessage = "Not enough stock available";
            }
        }

        // CanExecuteIncreaseQuantity - Se poate creste?
        private bool CanExecuteIncreaseQuantity(object parameter)
        {
            var item = parameter as CartItem ?? SelectedItem;

            if (item?.Product?.Inventory == null)
                return false;

            return item.Quantity < item.MaxQuantity;
        }

        // ExecuteDecreaseQuantity - Scade cantitatea
        private void ExecuteDecreaseQuantity(object parameter)
        {
            var item = parameter as CartItem ?? SelectedItem;

            if (item == null)
                return;

            if (item.Quantity > 1)
            {
                item.Quantity--;
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(TotalFormatted));
                OnPropertyChanged(nameof(TotalQuantity));
            }
            else
            {
                // Cantitate 1 -> stergem itemul
                CartItems.Remove(item);
            }
        }

        // CanExecuteDecreaseQuantity - Se poate scadea?
        private bool CanExecuteDecreaseQuantity(object parameter)
        {
            var item = parameter as CartItem ?? SelectedItem;
            return item != null && item.Quantity > 0;
        }

        // ExecuteClearCart - Goleste cosul
        private void ExecuteClearCart(object parameter)
        {
            // Confirmare
            // In productie, am afisa un dialog de confirmare

            CartItems.Clear();
            SelectedItem = null;
        }

        // CanExecuteClearCart - Se poate goli?
        private bool CanExecuteClearCart(object parameter)
        {
            return HasItems;
        }

        // ExecuteCheckout - Plaseaza comanda
        //
        // PROCESUL DE CHECKOUT:
        // 1. Valideaza datele (adresa, stoc)
        // 2. Creeaza Order din CartItems
        // 3. Scade stocul produselor
        // 4. Goleste cosul
        // 5. Navigheaza la confirmarea comenzii
        private void ExecuteCheckout(object parameter)
        {
            if (!CanCheckout)
            {
                ErrorMessage = "Please fill in all required fields";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // CREARE COMANDA
                var order = new Order
                {
                    CustomerID = _currentUserStore.CurrentUserId,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending",
                    ShippingAddress = ShippingAddress,
                    PaymentMethod = PaymentMethod,
                    TotalAmount = Total
                };

                // ADAUGARE LINII COMANDA
                foreach (var item in CartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        ProductID = item.Product.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                        // Subtotal e calculat automat in DB
                    };

                    order.OrderDetails.Add(orderDetail);

                    // SCADERE STOC
                    // item.Product.Inventory?.ReduceStock(item.Quantity);
                }

                // In productie: _orderService.CreateOrder(order);

                // GOLIRE COS
                CartItems.Clear();

                // NAVIGARE LA CONFIRMARE
                // _navigationStore.CurrentViewModel = new OrderConfirmationViewModel(order, ...);

                System.Diagnostics.Debug.WriteLine($"Order placed successfully! Total: {order.TotalFormatted}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to place order: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // CanExecuteCheckout - Se poate face checkout?
        private bool CanExecuteCheckout(object parameter)
        {
            return HasItems && !IsLoading && !string.IsNullOrWhiteSpace(ShippingAddress);
        }

        // ExecuteContinueShopping - Inapoi la magazin
        private void ExecuteContinueShopping(object parameter)
        {
            // _navigationStore.CurrentViewModel = new CustomerShopViewModel(...);
        }
    }
}
