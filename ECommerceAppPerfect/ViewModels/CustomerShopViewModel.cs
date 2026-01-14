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
    // CLASA CUSTOMERSHOPVIEWMODEL - Magazinul pentru clienti
    //
    // CE ESTE ACEST VIEWMODEL?
    // Este ecranul principal pe care il vede un Client dupa login
    // Permite navigarea si cumpararea produselor:
    // - Vizualizare catalog produse
    // - Cautare produse
    // - Filtrare pe categorii
    // - Adaugare in cos
    // - Vizualizare detalii produs
    //
    // FUNCTIONALITATI:
    // 1. GRID/LISTA PRODUSE - Afiseaza toate produsele active
    // 2. CAUTARE - Cauta dupa nume, descriere
    // 3. FILTRARE CATEGORIE - Dropdown sau sidebar cu categorii
    // 4. SORTARE - Pret, popularitate, data adaugarii
    // 5. ADAUGARE COS - Buton "Add to Cart" pe fiecare produs
    //
    // UI TIPIC (GRID):
    // +---------------+ +---------------+ +---------------+
    // |   [Image]     | |   [Image]     | |   [Image]     |
    // |   iPhone 15   | |   MacBook     | |   AirPods     |
    // |   $999.99     | |   $1999.99    | |   $199.99     |
    // |   *****(45)   | |   *****(30)   | |   ****(25)    |
    // | [Add to Cart] | | [Add to Cart] | | [Add to Cart] |
    // +---------------+ +---------------+ +---------------+
    //
    // BINDING EXEMPLU:
    // <ItemsControl ItemsSource="{Binding Products}">
    //     <ItemsControl.ItemTemplate>
    //         <DataTemplate>
    //             <Border>
    //                 <StackPanel>
    //                     <Image Source="{Binding ImageURL}" />
    //                     <TextBlock Text="{Binding ProductName}" />
    //                     <TextBlock Text="{Binding PriceFormatted}" />
    //                     <TextBlock Text="{Binding StockStatus}" />
    //                     <Button Content="Add to Cart"
    //                             Command="{Binding DataContext.AddToCartCommand,
    //                                       RelativeSource={RelativeSource AncestorType=ItemsControl}}"
    //                             CommandParameter="{Binding}" />
    //                 </StackPanel>
    //             </Border>
    //         </DataTemplate>
    //     </ItemsControl.ItemTemplate>
    // </ItemsControl>
    public class CustomerShopViewModel : ViewModelBase
    {
        // SERVICII

        // _productService - Pentru incarcarea produselor
        private readonly IProductService _productService;

        // STORES

        // _currentUserStore - Informatii despre clientul curent
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare (la cos, la detalii produs)
        private readonly NavigationStore _navigationStore;

        // COLECTII

        // _allProducts - Lista completa de produse (nefiltrata)
        private List<Product> _allProducts;

        // _products - Lista filtrata afisata in UI
        private ObservableCollection<Product> _products;

        // _categories - Lista categoriilor pentru filtru
        private ObservableCollection<Category> _categories;

        // _featuredProducts - Produse recomandate (pentru slider sau sectiune separata)
        private ObservableCollection<Product> _featuredProducts;

        // CAMPURI PROPRIETATI

        // Produsul selectat (pentru vizualizare detalii)
        private Product _selectedProduct;

        // Textul de cautare
        private string _searchText;

        // Categoria selectata pentru filtru
        private Category _selectedCategory;

        // Modul de sortare
        private string _sortMode;

        // Flag incarcare
        private bool _isLoading;

        // Mesaj eroare
        private string _errorMessage;

        // Cantitatea de adaugat in cos
        private int _quantityToAdd;

        // CONSTRUCTOR
        public CustomerShopViewModel(
            IProductService productService,
            CurrentUserStore currentUserStore,
            NavigationStore navigationStore)
        {
            // Salvare dependinte
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare colectii
            _products = new ObservableCollection<Product>();
            _categories = new ObservableCollection<Category>();
            _featuredProducts = new ObservableCollection<Product>();
            _allProducts = new List<Product>();

            // Valoare default cantitate
            _quantityToAdd = 1;

            // Sortare default: cele mai noi
            _sortMode = "Newest";

            // Initializare comenzi
            AddToCartCommand = new RelayCommand(ExecuteAddToCart, CanExecuteAddToCart);
            ViewProductDetailsCommand = new RelayCommand(ExecuteViewProductDetails);
            NavigateToCartCommand = new RelayCommand(ExecuteNavigateToCart);
            SearchCommand = new RelayCommand(ExecuteSearch);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);

            // Incarcare date
            LoadData();
        }

        // PROPRIETATI - Colectii

        // Products - Lista de produse afisata in magazin
        //
        // Contine doar produsele active si cu stoc disponibil
        // Poate fi filtrata dupa categorie si cautare
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        // Categories - Lista categoriilor pentru filtrare
        //
        // BINDING:
        // <ListBox ItemsSource="{Binding Categories}"
        //          SelectedItem="{Binding SelectedCategory}">
        //     <ListBox.ItemTemplate>
        //         <DataTemplate>
        //             <TextBlock Text="{Binding DisplayName}" />
        //         </DataTemplate>
        //     </ListBox.ItemTemplate>
        // </ListBox>
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        // FeaturedProducts - Produse recomandate
        //
        // Best sellers sau produse promovate
        // Afisate in slider sau sectiune separata pe pagina principala
        public ObservableCollection<Product> FeaturedProducts
        {
            get => _featuredProducts;
            set => SetProperty(ref _featuredProducts, value);
        }

        // PROPRIETATI - Selectie

        // SelectedProduct - Produsul selectat pentru vizualizare detalii
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    // Notificam proprietatile derivate
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(SelectedProductName));
                    OnPropertyChanged(nameof(SelectedProductPrice));
                    OnPropertyChanged(nameof(SelectedProductDescription));
                    OnPropertyChanged(nameof(SelectedProductStock));
                    OnPropertyChanged(nameof(SelectedProductRating));
                    OnPropertyChanged(nameof(CanAddSelectedToCart));
                }
            }
        }

        // SelectedCategory - Categoria selectata pentru filtru
        //
        // null = toate categoriile
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                    OnPropertyChanged(nameof(SelectedCategoryName));
                }
            }
        }

        // PROPRIETATI - Filtrare si Sortare

        // SearchText - Textul de cautare
        //
        // BINDING:
        // <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
        //          Placeholder="Search products..." />
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        // SortMode - Modul de sortare curent
        //
        // Valori: "Newest", "PriceLowToHigh", "PriceHighToLow", "Popular", "Rating"
        public string SortMode
        {
            get => _sortMode;
            set
            {
                if (SetProperty(ref _sortMode, value))
                {
                    ApplyFilters();
                }
            }
        }

        // QuantityToAdd - Cantitatea de adaugat in cos
        //
        // Default 1, utilizatorul poate schimba inainte de adaugare
        public int QuantityToAdd
        {
            get => _quantityToAdd;
            set => SetProperty(ref _quantityToAdd, Math.Max(1, value));
        }

        // PROPRIETATI - Stare

        // IsLoading - Se incarca datele?
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

        // HasSelection - Este selectat un produs?
        public bool HasSelection => SelectedProduct != null;

        // SelectedProductName - Numele produsului selectat
        public string SelectedProductName => SelectedProduct?.ProductName ?? "No selection";

        // SelectedProductPrice - Pretul produsului selectat
        public string SelectedProductPrice => SelectedProduct?.PriceFormatted ?? "N/A";

        // SelectedProductDescription - Descrierea produsului selectat
        public string SelectedProductDescription => SelectedProduct?.Description ?? "No description available";

        // SelectedProductStock - Stocul produsului selectat
        public string SelectedProductStock => SelectedProduct?.StockStatus ?? "N/A";

        // SelectedProductRating - Rating-ul produsului selectat
        public string SelectedProductRating
        {
            get
            {
                if (SelectedProduct?.AverageRating == null)
                    return "No reviews yet";

                return $"{SelectedProduct.AverageRating:F1}/5 ({SelectedProduct.ReviewCount} reviews)";
            }
        }

        // CanAddSelectedToCart - Se poate adauga produsul selectat in cos?
        //
        // Verifica daca produsul are stoc
        public bool CanAddSelectedToCart
        {
            get
            {
                if (SelectedProduct?.Inventory == null)
                    return false;

                return SelectedProduct.Inventory.CanFulfill(QuantityToAdd);
            }
        }

        // SelectedCategoryName - Numele categoriei selectate
        public string SelectedCategoryName => SelectedCategory?.CategoryName ?? "All Categories";

        // PROPRIETATI CALCULATE - Statistici

        // ProductCount - Numarul de produse afisate
        public int ProductCount => Products?.Count ?? 0;

        // HasProducts - Exista produse de afisat?
        public bool HasProducts => ProductCount > 0;

        // IsFiltered - Sunt active filtre?
        public bool IsFiltered => !string.IsNullOrEmpty(SearchText) || SelectedCategory != null;

        // WelcomeMessage - Mesaj de bun venit
        public string WelcomeMessage => $"Welcome, {_currentUserStore.CurrentUserFullName}!";

        // COMENZI

        // AddToCartCommand - Adauga produs in cos
        //
        // PARAMETRU: Produsul de adaugat (din CommandParameter)
        //
        // BINDING:
        // <Button Command="{Binding AddToCartCommand}"
        //         CommandParameter="{Binding}"
        //         Content="Add to Cart" />
        public ICommand AddToCartCommand { get; }

        // ViewProductDetailsCommand - Vezi detalii produs
        //
        // Navigheaza la un ecran de detalii sau deschide popup
        public ICommand ViewProductDetailsCommand { get; }

        // NavigateToCartCommand - Navigheaza la cos
        public ICommand NavigateToCartCommand { get; }

        // SearchCommand - Executa cautarea
        //
        // Poate fi legat de butonul de cautare sau Enter in textbox
        public ICommand SearchCommand { get; }

        // ClearFiltersCommand - Sterge toate filtrele
        public ICommand ClearFiltersCommand { get; }

        // RefreshCommand - Reincarca produsele
        public ICommand RefreshCommand { get; }

        // METODE INCARCARE DATE

        // LoadData - Incarca produsele si categoriile
        private void LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Incarca categoriile
                LoadCategories();

                // Incarca produsele
                LoadProducts();

                // Incarca produsele recomandate
                LoadFeaturedProducts();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load products: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // LoadCategories - Incarca lista de categorii
        private void LoadCategories()
        {
            var categories = _productService.GetAllCategories();

            Categories.Clear();

            if (categories != null)
            {
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            }
        }

        // LoadProducts - Incarca produsele active
        private void LoadProducts()
        {
            // Incarcam doar produsele active si cu stoc
            var products = _productService.GetActiveProducts();

            _allProducts = products ?? new List<Product>();

            ApplyFilters();
        }

        // LoadFeaturedProducts - Incarca produsele recomandate
        private void LoadFeaturedProducts()
        {
            // Best sellers sau produse promovate
            var featured = _productService.GetFeaturedProducts(6);

            FeaturedProducts.Clear();

            if (featured != null)
            {
                foreach (var product in featured)
                {
                    FeaturedProducts.Add(product);
                }
            }
        }

        // ApplyFilters - Aplica filtrele si sortarea
        private void ApplyFilters()
        {
            // Pornim de la toate produsele
            IEnumerable<Product> filtered = _allProducts;

            // FILTRARE DUPA TEXT
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(searchLower)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
                    (p.Category?.CategoryName != null && p.Category.CategoryName.ToLower().Contains(searchLower))
                );
            }

            // FILTRARE DUPA CATEGORIE
            if (SelectedCategory != null)
            {
                filtered = filtered.Where(p => p.CategoryID == SelectedCategory.CategoryID);
            }

            // SORTARE
            filtered = ApplySorting(filtered);

            // Actualizam colectia
            Products.Clear();
            foreach (var product in filtered)
            {
                Products.Add(product);
            }

            // Notificam proprietatile dependente
            OnPropertyChanged(nameof(ProductCount));
            OnPropertyChanged(nameof(HasProducts));
            OnPropertyChanged(nameof(IsFiltered));
        }

        // ApplySorting - Aplica sortarea specificata
        private IEnumerable<Product> ApplySorting(IEnumerable<Product> products)
        {
            return SortMode switch
            {
                "PriceLowToHigh" => products.OrderBy(p => p.Price),
                "PriceHighToLow" => products.OrderByDescending(p => p.Price),
                "Popular" => products.OrderByDescending(p => p.TotalSold),
                "Rating" => products.OrderByDescending(p => p.AverageRating ?? 0),
                "Newest" => products.OrderByDescending(p => p.CreatedDate),
                _ => products.OrderByDescending(p => p.CreatedDate) // Default: newest
            };
        }

        // IMPLEMENTARI COMENZI

        // ExecuteAddToCart - Adauga produs in cos
        //
        // PARAMETRU: Produsul de adaugat
        private void ExecuteAddToCart(object parameter)
        {
            // Produsul poate veni din parametru sau din SelectedProduct
            var product = parameter as Product ?? SelectedProduct;

            if (product == null)
                return;

            // Verificam stocul
            if (product.Inventory == null || !product.Inventory.CanFulfill(QuantityToAdd))
            {
                ErrorMessage = "Sorry, this product is out of stock";
                return;
            }

            try
            {
                // Adaugam in cos
                // In productie, am avea un CartService sau CartStore
                // _cartStore.AddItem(product, QuantityToAdd);

                // Afisam confirmare
                // In productie, am afisa un toast sau snackbar
                // ToastService.Show($"Added {product.ProductName} to cart");

                // Resetam cantitatea
                QuantityToAdd = 1;

                System.Diagnostics.Debug.WriteLine($"Added {product.ProductName} to cart");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to add to cart: " + ex.Message;
            }
        }

        // CanExecuteAddToCart - Se poate adauga in cos?
        private bool CanExecuteAddToCart(object parameter)
        {
            var product = parameter as Product;

            if (product?.Inventory == null)
                return false;

            return product.Inventory.CanFulfill(1); // Minim 1 bucata
        }

        // ExecuteViewProductDetails - Navigheaza la detalii produs
        private void ExecuteViewProductDetails(object parameter)
        {
            var product = parameter as Product ?? SelectedProduct;

            if (product == null)
                return;

            // Navigam la ecranul de detalii
            // _navigationStore.CurrentViewModel = new ProductDetailsViewModel(product, ...);
        }

        // ExecuteNavigateToCart - Navigheaza la cos
        private void ExecuteNavigateToCart(object parameter)
        {
            // _navigationStore.CurrentViewModel = new ShoppingCartViewModel(...);
        }

        // ExecuteSearch - Executa cautarea
        //
        // Poate fi apelat din buton sau la Enter
        private void ExecuteSearch(object parameter)
        {
            // Filtrul se aplica deja automat la schimbarea SearchText
            // Aceasta comanda poate fi folosita pentru cautari avansate
            ApplyFilters();
        }

        // ExecuteClearFilters - Sterge filtrele
        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            SortMode = "Newest";
            // ApplyFilters se apeleaza automat
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
    }
}
