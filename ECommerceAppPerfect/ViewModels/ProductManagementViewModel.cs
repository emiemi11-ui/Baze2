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
    // CLASA PRODUCTMANAGEMENTVIEWMODEL - CRUD complet pentru produse
    //
    // CE ESTE ACEST VIEWMODEL?
    // Gestioneaza operatiile de management al produselor:
    // - CREATE: Adaugare produse noi
    // - READ: Listare si cautare produse
    // - UPDATE: Editare produse existente
    // - DELETE: Stergere (soft delete) produse
    //
    // CINE IL FOLOSESTE?
    // StoreOwner-ul pentru a gestiona catalogul de produse
    //
    // FUNCTIONALITATI:
    // 1. LISTA PRODUSE - ObservableCollection cu toate produsele
    // 2. CAUTARE - Filtrare dupa nume, descriere, categorie
    // 3. FILTRARE CATEGORIE - Dropdown pentru filtrare pe categorii
    // 4. SELECTIE - SelectedProduct pentru editare/stergere
    // 5. CRUD - Comenzi pentru Add, Edit, Delete
    //
    // OBSERVABLECOLLECTION - CE ESTE?
    // Este o colectie speciala din WPF care NOTIFICA UI-ul automat
    // cand se adauga sau se sterg elemente
    //
    // DIFERENTA FATA DE LIST<T>:
    // - List<T>: UI-ul nu stie cand se schimba lista
    // - ObservableCollection<T>: UI-ul se actualizeaza automat
    //
    // EXEMPLU:
    // Products.Add(newProduct); // DataGrid se actualizeaza automat!
    // Products.Remove(product); // DataGrid se actualizeaza automat!
    //
    // BINDING IN UI:
    // <DataGrid ItemsSource="{Binding Products}"
    //           SelectedItem="{Binding SelectedProduct}">
    //     <DataGrid.Columns>
    //         <DataGridTextColumn Header="Name" Binding="{Binding ProductName}" />
    //         <DataGridTextColumn Header="Price" Binding="{Binding PriceFormatted}" />
    //         <DataGridTextColumn Header="Stock" Binding="{Binding StockStatus}" />
    //     </DataGrid.Columns>
    // </DataGrid>
    //
    // <Button Command="{Binding AddProductCommand}" Content="Add" />
    // <Button Command="{Binding EditProductCommand}" Content="Edit" />
    // <Button Command="{Binding DeleteProductCommand}" Content="Delete" />
    public class ProductManagementViewModel : ViewModelBase
    {
        // SERVICII

        // _productService - Serviciul pentru operatii cu produse
        //
        // Toate operatiile CRUD merg prin acest serviciu
        // ViewModel-ul NU acceseaza baza de date direct
        private readonly IProductService _productService;

        // STORES

        // _currentUserStore - Pentru a stii cine e logat
        //
        // Folosit pentru:
        // - A seta StoreOwnerID la adaugare produs
        // - A filtra produsele (vezi doar produsele tale)
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare
        private readonly NavigationStore _navigationStore;

        // COLECTII DE DATE

        // _allProducts - Lista completa de produse (nefiltrata)
        //
        // Aceasta lista tine toate produsele incarcate
        // Products (expusa) poate fi filtrata
        private List<Product> _allProducts;

        // _products - Lista filtrata de produse (expusa catre UI)
        //
        // ObservableCollection notifica automat UI-ul la schimbari
        private ObservableCollection<Product> _products;

        // _categories - Lista categoriilor pentru filtru
        private ObservableCollection<Category> _categories;

        // CAMPURI PENTRU PROPRIETATI

        // Produsul selectat in DataGrid
        private Product _selectedProduct;

        // Textul de cautare
        private string _searchText;

        // Categoria selectata pentru filtru
        private Category _selectedCategory;

        // Flag pentru incarcare
        private bool _isLoading;

        // Mesaj de eroare
        private string _errorMessage;

        // CONSTRUCTOR

        // Initializeaza ViewModel-ul cu dependintele necesare
        //
        // PARAMETRI:
        // - productService: Serviciul pentru operatii CRUD
        // - currentUserStore: Store-ul pentru utilizatorul curent
        // - navigationStore: Store-ul pentru navigare
        public ProductManagementViewModel(
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
            _allProducts = new List<Product>();

            // Initializare comenzi
            AddProductCommand = new RelayCommand(ExecuteAddProduct);
            EditProductCommand = new RelayCommand(ExecuteEditProduct, CanExecuteEditProduct);
            DeleteProductCommand = new RelayCommand(ExecuteDeleteProduct, CanExecuteDeleteProduct);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);

            // Incarcare date initiale
            LoadData();
        }

        // PROPRIETATI - Colectii

        // Products - Lista de produse afisata in UI
        //
        // OBSERVABLECOLLECTION permite:
        // - Binding la DataGrid/ListView
        // - Actualizare automata UI la Add/Remove
        //
        // FILTRARE:
        // Aceasta lista poate fi filtrata dupa:
        // - SearchText (cautare in nume/descriere)
        // - SelectedCategory (filtrare pe categorie)
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        // Categories - Lista categoriilor pentru dropdown
        //
        // Prima optiune e "All Categories" (null) pentru a vedea toate
        //
        // BINDING:
        // <ComboBox ItemsSource="{Binding Categories}"
        //           SelectedItem="{Binding SelectedCategory}"
        //           DisplayMemberPath="CategoryName" />
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        // PROPRIETATI - Selectie

        // SelectedProduct - Produsul selectat in lista
        //
        // BINDING BIDIRECTIONAL:
        // <DataGrid SelectedItem="{Binding SelectedProduct}" />
        //
        // UTILIZARE:
        // - Edit: Deschidem editarea pentru SelectedProduct
        // - Delete: Stergem SelectedProduct
        //
        // COMENZILE Edit si Delete depind de aceasta proprietate
        // Cand e null, butoanele sunt dezactivate (CanExecute = false)
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    // Cand selectia se schimba, comenzile trebuie reverificiate
                    // CanExecute se reevalueaza pentru Edit si Delete
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // PROPRIETATI - Filtrare

        // SearchText - Textul de cautare
        //
        // BINDING:
        // <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" />
        //
        // La fiecare modificare, se aplica filtrul automat
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Aplica filtrul cand se schimba textul
                    ApplyFilters();
                }
            }
        }

        // SelectedCategory - Categoria selectata pentru filtru
        //
        // null = toate categoriile
        // o categorie = doar produsele din acea categorie
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        // PROPRIETATI - Stare

        // IsLoading - Se incarca datele?
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ErrorMessage - Mesaj de eroare
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // HasError - Exista eroare?
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // PROPRIETATI CALCULATE

        // ProductCount - Numarul de produse afisate
        //
        // Util pentru: "Showing 45 products"
        public int ProductCount => Products?.Count ?? 0;

        // HasProducts - Exista produse?
        //
        // Folosit pentru a afisa un mesaj "No products found" cand lista e goala
        public bool HasProducts => ProductCount > 0;

        // IsFiltered - Sunt active filtre?
        //
        // True daca SearchText sau SelectedCategory sunt setate
        public bool IsFiltered => !string.IsNullOrEmpty(SearchText) || SelectedCategory != null;

        // COMENZI

        // AddProductCommand - Adauga un produs nou
        //
        // ACTIUNE:
        // Deschide un dialog sau navigheaza la un formular de adaugare
        public ICommand AddProductCommand { get; }

        // EditProductCommand - Editeaza produsul selectat
        //
        // CANEXECUTE: Doar daca SelectedProduct != null
        public ICommand EditProductCommand { get; }

        // DeleteProductCommand - Sterge produsul selectat
        //
        // ACTIUNE: Soft delete (seteaza IsActive = false)
        // CANEXECUTE: Doar daca SelectedProduct != null
        public ICommand DeleteProductCommand { get; }

        // RefreshCommand - Reincarca datele
        public ICommand RefreshCommand { get; }

        // ClearFiltersCommand - Sterge toate filtrele
        public ICommand ClearFiltersCommand { get; }

        // METODE DE INCARCARE DATE

        // LoadData - Incarca toate datele necesare
        //
        // Apelata la:
        // - Initializare (constructor)
        // - Refresh
        private void LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Incarca categoriile pentru dropdown
                LoadCategories();

                // Incarca produsele
                LoadProducts();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load data. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
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

            // Adaugam categoriile incarcate
            if (categories != null)
            {
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            }
        }

        // LoadProducts - Incarca lista de produse
        private void LoadProducts()
        {
            // Incarcam toate produsele
            var products = _productService.GetAllProducts();

            // Salvam in lista completa
            _allProducts = products ?? new List<Product>();

            // Aplicam filtrele pentru a popula Products
            ApplyFilters();
        }

        // ApplyFilters - Aplica filtrele de cautare si categorie
        //
        // Aceasta metoda:
        // 1. Ia toate produsele din _allProducts
        // 2. Filtreaza dupa SearchText (daca e setat)
        // 3. Filtreaza dupa SelectedCategory (daca e setat)
        // 4. Pune rezultatul in Products
        private void ApplyFilters()
        {
            // Pornim de la toate produsele
            IEnumerable<Product> filtered = _allProducts;

            // FILTRARE DUPA TEXT
            // Cautam in ProductName si Description
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(searchLower)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower))
                );
            }

            // FILTRARE DUPA CATEGORIE
            if (SelectedCategory != null)
            {
                filtered = filtered.Where(p => p.CategoryID == SelectedCategory.CategoryID);
            }

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

        // IMPLEMENTARI COMENZI

        // ExecuteAddProduct - Logica de adaugare produs
        //
        // OPTIUNI DE IMPLEMENTARE:
        // 1. Deschide un dialog modal
        // 2. Navigheaza la un ecran de adaugare
        // 3. Creeaza un produs in lista si il selecteaza pentru editare inline
        private void ExecuteAddProduct(object parameter)
        {
            // Cream un produs nou
            var newProduct = new Product
            {
                ProductName = "New Product",
                Price = 0,
                IsActive = true,
                CreatedDate = DateTime.Now,
                StoreOwnerID = _currentUserStore.CurrentUserId
            };

            // OPTIUNEA 1: Deschide dialog
            // var dialog = new ProductDialog(newProduct, _productService, isNew: true);
            // if (dialog.ShowDialog() == true)
            // {
            //     LoadProducts();
            // }

            // OPTIUNEA 2: Navigheaza la ecran de adaugare
            // _navigationStore.CurrentViewModel = new ProductEditViewModel(newProduct, ...);

            // OPTIUNEA 3: Adauga si selecteaza pentru editare
            // Products.Add(newProduct);
            // SelectedProduct = newProduct;
        }

        // ExecuteEditProduct - Logica de editare produs
        //
        // Deschide editarea pentru SelectedProduct
        private void ExecuteEditProduct(object parameter)
        {
            if (SelectedProduct == null)
                return;

            // OPTIUNEA 1: Dialog
            // var dialog = new ProductDialog(SelectedProduct, _productService, isNew: false);
            // if (dialog.ShowDialog() == true)
            // {
            //     LoadProducts();
            // }

            // OPTIUNEA 2: Navigare
            // _navigationStore.CurrentViewModel = new ProductEditViewModel(SelectedProduct, ...);
        }

        // CanExecuteEditProduct - Se poate edita?
        //
        // Doar daca avem un produs selectat
        private bool CanExecuteEditProduct(object parameter)
        {
            return SelectedProduct != null;
        }

        // ExecuteDeleteProduct - Logica de stergere produs
        //
        // Sterge (soft delete) produsul selectat
        private void ExecuteDeleteProduct(object parameter)
        {
            if (SelectedProduct == null)
                return;

            // Confirmare
            // In productie, am afisa un dialog de confirmare
            // if (MessageBox.Show("Are you sure?", "Confirm Delete", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            //     return;

            try
            {
                // Soft delete prin serviciu
                bool success = _productService.DeleteProduct(SelectedProduct.ProductID);

                if (success)
                {
                    // Stergem din lista locala
                    Products.Remove(SelectedProduct);
                    _allProducts.Remove(SelectedProduct);
                    SelectedProduct = null;

                    // Actualizam statisticile
                    OnPropertyChanged(nameof(ProductCount));
                    OnPropertyChanged(nameof(HasProducts));
                }
                else
                {
                    ErrorMessage = "Failed to delete product.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting product: " + ex.Message;
            }
        }

        // CanExecuteDeleteProduct - Se poate sterge?
        private bool CanExecuteDeleteProduct(object parameter)
        {
            return SelectedProduct != null;
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

        // ExecuteClearFilters - Sterge toate filtrele
        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            // ApplyFilters se apeleaza automat din settere
        }
    }
}
