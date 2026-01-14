using System;
using ECommerceAppPerfect.ViewModels;

namespace ECommerceAppPerfect.Stores
{
    // CLASA NAVIGATIONSTORE - State Management pentru navigare
    //
    // CE ESTE NAVIGATIONSTORE?
    // Gestioneaza NAVIGAREA in aplicatie - ce View/ViewModel e afisat curent
    // In loc de a schimba pagini direct, schimbam CurrentViewModel
    // MainWindow afiseaza automat View-ul corespunzator
    //
    // CUM FUNCTIONEAZA NAVIGAREA IN WPF/MVVM?
    //
    // METODA CLASICA (code-behind):
    // Button click -> NavigationService.Navigate(new Page())
    // Problema: Logica in code-behind, greu de testat
    //
    // METODA MVVM (cu Store):
    // 1. MainWindow are un ContentControl cu Content="{Binding CurrentViewModel}"
    // 2. DataTemplates mapeaza ViewModels la Views
    // 3. Cand CurrentViewModel se schimba, UI se actualizeaza automat
    //
    // EXEMPLU:
    // NavigationStore.CurrentViewModel = new LoginViewModel();
    // -> MainWindow afiseaza automat LoginView
    //
    // NavigationStore.CurrentViewModel = new ProductManagementViewModel();
    // -> MainWindow afiseaza automat ProductManagementView
    //
    // AVANTAJE:
    // 1. Testabilitate: Poti testa navigarea fara UI
    // 2. Decuplare: ViewModels nu stiu de Views
    // 3. Flexibilitate: Usor de adaugat animatii, istoric, etc.
    //
    // PATTERN: OBSERVER
    // Cand CurrentViewModel se schimba, se declanseaza CurrentViewModelChanged
    // MainWindow asculta si actualizeaza ContentControl
    public class NavigationStore
    {
        // CAMPUL PRIVAT _currentViewModel
        //
        // ViewModel-ul afisat curent
        private ViewModelBase _currentViewModel;

        // PROPRIETATEA CurrentViewModel
        //
        // ViewModel-ul care trebuie afisat in MainWindow
        // Schimbarea acestuia declanseaza navigarea
        //
        // CUM FUNCTIONEAZA CU UI:
        // MainWindow.xaml:
        // <ContentControl Content="{Binding CurrentViewModel}">
        //     <ContentControl.Resources>
        //         <DataTemplate DataType="{x:Type vm:LoginViewModel}">
        //             <views:LoginView />
        //         </DataTemplate>
        //         <DataTemplate DataType="{x:Type vm:ProductManagementViewModel}">
        //             <views:ProductManagementView />
        //         </DataTemplate>
        //     </ContentControl.Resources>
        // </ContentControl>
        //
        // Cand CurrentViewModel = new LoginViewModel():
        // WPF cauta DataTemplate pentru LoginViewModel
        // Gaseste -> afiseaza LoginView
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                // Dispune vechiul ViewModel daca e IDisposable
                // Previne memory leaks (conexiuni DB, event handlers, etc.)
                DisposeCurrentViewModel();

                // Seteaza noul ViewModel
                _currentViewModel = value;

                // Notifica abonatii ca s-a schimbat
                OnCurrentViewModelChanged();
            }
        }

        // EVENIMENTUL CurrentViewModelChanged
        //
        // Se declanseaza cand CurrentViewModel se schimba
        //
        // CINE E ABONAT?
        // MainViewModel (sau MainWindow) asculta si actualizeaza UI
        //
        // EXEMPLU FOLOSIRE:
        // _navigationStore.CurrentViewModelChanged += () => OnPropertyChanged(nameof(CurrentViewModel));
        public event Action CurrentViewModelChanged;

        // METODA PRIVATA OnCurrentViewModelChanged
        //
        // Declanseaza evenimentul CurrentViewModelChanged
        // Metoda separata pentru claritate si posibilitatea de override
        private void OnCurrentViewModelChanged()
        {
            // ?. = null-conditional operator
            // Daca nu sunt abonati (CurrentViewModelChanged == null), nu face nimic
            // Daca sunt, apeleaza Invoke()
            CurrentViewModelChanged?.Invoke();
        }

        // METODA PRIVATA DisposeCurrentViewModel
        //
        // Curata ViewModel-ul vechi inainte de a-l inlocui
        //
        // DE CE E IMPORTANT?
        // ViewModels pot avea:
        // - Conexiuni la DB deschise
        // - Event handlers abonati
        // - Timere active
        // - Alte resurse
        //
        // Fara Dispose, acestea ar ramane in memorie (memory leak)
        private void DisposeCurrentViewModel()
        {
            // Verificam daca ViewModel-ul curent implementeaza IDisposable
            if (_currentViewModel is IDisposable disposable)
            {
                // Apelam Dispose pentru a elibera resursele
                disposable.Dispose();
            }
        }

        // PROPRIETATI HELPER

        // HasCurrentViewModel - Este setat un ViewModel?
        //
        // True daca avem un ViewModel afisat
        // False la pornire inainte de prima navigare
        public bool HasCurrentViewModel => _currentViewModel != null;

        // CurrentViewModelType - Tipul ViewModel-ului curent
        //
        // Returneaza tipul pentru comparatii si logging
        // null daca nu e setat
        //
        // EXEMPLU:
        // if (navigationStore.CurrentViewModelType == typeof(LoginViewModel))
        //     // Suntem pe pagina de login
        public Type CurrentViewModelType => _currentViewModel?.GetType();

        // METODE DE NAVIGARE HELPER
        //
        // Pentru simplificarea navigarii din ViewModels

        // NavigateTo<T> - Navigheaza la un ViewModel de tipul T
        //
        // Creaza instanta si o seteaza ca CurrentViewModel
        //
        // GENERIC T: Tipul ViewModel-ului
        // where new(): Trebuie sa aiba constructor fara parametri
        //
        // FOLOSIRE:
        // navigationStore.NavigateTo<LoginViewModel>();
        //
        // LIMITARE:
        // Functioneaza doar pentru ViewModels fara dependinte in constructor
        // Pentru ViewModels cu dependinte, foloseste CurrentViewModel = new VM(deps)
        public void NavigateTo<T>() where T : ViewModelBase, new()
        {
            CurrentViewModel = new T();
        }

        // NavigateTo - Navigheaza la un ViewModel dat
        //
        // Varianta care primeste instanta deja creata
        // Util cand ViewModel-ul are dependinte in constructor
        //
        // FOLOSIRE:
        // navigationStore.NavigateTo(new ProductManagementViewModel(productService));
        public void NavigateTo(ViewModelBase viewModel)
        {
            CurrentViewModel = viewModel;
        }

        // IsCurrentViewModel<T> - Verifică daca suntem pe un anumit tip de ViewModel
        //
        // GENERIC T: Tipul de verificat
        //
        // FOLOSIRE:
        // if (navigationStore.IsCurrentViewModel<LoginViewModel>())
        //     // Suntem pe login
        public bool IsCurrentViewModel<T>() where T : ViewModelBase
        {
            return _currentViewModel is T;
        }
    }
}
