using System;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Stores
{
    // CLASA CURRENTUSERSTORE - State Management pentru utilizatorul curent
    //
    // CE ESTE UN STORE?
    // Un Store este o clasa care tine STAREA GLOBALA a aplicatiei
    // In loc sa pasam datele de la ViewModel la ViewModel,
    // le punem intr-un Store central pe care il pot accesa toti
    //
    // DE CE AVEM NEVOIE DE CURRENTUSERSTORE?
    // Dupa login, trebuie sa stim CINE e logat in toata aplicatia:
    // - MainViewModel trebuie sa stie pentru a afisa meniul corect
    // - ProductManagementViewModel trebuie sa stie pentru a filtra produse
    // - OrderViewModel trebuie sa stie pentru a crea comenzi
    // - etc.
    //
    // FARA STORE:
    // Ar trebui sa pasam User-ul prin constructori de la ViewModel la ViewModel
    // Complicat, error-prone, greu de mentinut
    //
    // CU STORE:
    // Orice ViewModel poate accesa CurrentUserStore.CurrentUser
    // Simplu, centralizat, testabil
    //
    // PATTERN: SINGLETON-LIKE
    // In aplicatie, avem O SINGURA instanta de CurrentUserStore
    // Creata in App.xaml.cs si injectata in ViewModels
    //
    // PATTERN: OBSERVER
    // Cand CurrentUser se schimba, se declanseaza CurrentUserChanged
    // ViewModels abonate la eveniment pot reactiona
    //
    // EXEMPLU FOLOSIRE:
    // // In ViewModel:
    // public MyViewModel(CurrentUserStore userStore)
    // {
    //     _userStore = userStore;
    //     _userStore.CurrentUserChanged += OnUserChanged;
    // }
    //
    // private void OnUserChanged()
    // {
    //     // Utilizatorul s-a schimbat, actualizeaza UI
    //     OnPropertyChanged(nameof(WelcomeMessage));
    // }
    public class CurrentUserStore
    {
        // CAMPUL PRIVAT _currentUser
        //
        // Utilizatorul autentificat curent
        // Null daca nimeni nu e logat
        private User _currentUser;

        // PROPRIETATEA CurrentUser
        //
        // Utilizatorul autentificat curent
        //
        // GET: Returneaza utilizatorul curent
        // SET: Seteaza utilizatorul si declanseaza eveniment
        //
        // DE CE PROPRIETATE SI NU CAMP PUBLIC?
        // Pentru ca vrem sa NOTIFICAM cand se schimba
        // Campurile nu pot avea logica custom la set
        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                // Seteaza noua valoare
                _currentUser = value;

                // Notifica abonatii ca s-a schimbat
                // ?. = null-conditional (daca nu sunt abonati, nu crapa)
                // Invoke() = apeleaza toti handlers abonati
                CurrentUserChanged?.Invoke();
            }
        }

        // EVENIMENTUL CurrentUserChanged
        //
        // Se declanseaza cand CurrentUser se schimba
        //
        // FOLOSIRE:
        // _userStore.CurrentUserChanged += MyHandler;
        //
        // CAND SE DECLANSEAZA?
        // 1. La login: CurrentUser = utilizatorul autentificat
        // 2. La logout: CurrentUser = null
        // 3. La update profil: CurrentUser = utilizatorul updatat
        public event Action CurrentUserChanged;

        // PROPRIETATI HELPER - Pentru acces rapid la informatii comune

        // IsLoggedIn - Este cineva logat?
        //
        // True daca CurrentUser != null
        // Folosit pentru a afisa/ascunde elemente UI
        public bool IsLoggedIn => CurrentUser != null;

        // IsStoreOwner - Este StoreOwner?
        //
        // True daca utilizatorul e proprietarul magazinului
        // Folosit pentru a afisa meniul de admin
        public bool IsStoreOwner => CurrentUser?.UserRole == "StoreOwner";

        // IsCustomer - Este Customer?
        //
        // True daca utilizatorul e client obisnuit
        // Folosit pentru a afisa magazinul, cosul, etc.
        public bool IsCustomer => CurrentUser?.UserRole == "Customer";

        // IsCustomerService - Este Agent de suport?
        //
        // True daca utilizatorul e agent CustomerService
        // Folosit pentru a afisa dashboard-ul de suport
        public bool IsCustomerService => CurrentUser?.UserRole == "CustomerService";

        // CurrentUserId - ID-ul utilizatorului curent
        //
        // 0 daca nu e logat nimeni
        // Folosit pentru query-uri (ex: comenzile utilizatorului curent)
        public int CurrentUserId => CurrentUser?.UserID ?? 0;

        // CurrentUsername - Username-ul utilizatorului curent
        //
        // "" daca nu e logat nimeni
        public string CurrentUsername => CurrentUser?.Username ?? "";

        // CurrentUserFullName - Numele complet
        //
        // "Guest" daca nu e logat nimeni
        public string CurrentUserFullName => CurrentUser?.FullName ?? "Guest";

        // METODE

        // Login - Autentifica un utilizator
        //
        // PARAMETRU: user - Utilizatorul autentificat
        //
        // Seteaza CurrentUser si declanseaza notificare
        // Apelat de LoginViewModel dupa autentificare reusita
        public void Login(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            CurrentUser = user;
        }

        // Logout - Delogheaza utilizatorul
        //
        // Seteaza CurrentUser = null si declanseaza notificare
        // Apelat de MainViewModel la logout
        public void Logout()
        {
            CurrentUser = null;
        }

        // HasPermission - Verifica daca utilizatorul are o anumita permisiune
        //
        // PARAMETRU: permission - Numele permisiunii
        //
        // EXEMPLU:
        // if (userStore.HasPermission("ManageProducts"))
        //     // Afiseaza butonul de management
        //
        // PENTRU SIMPLITATE:
        // Permisiunile sunt bazate pe rol
        // In sisteme complexe, ai avea tabel separat de permisiuni
        public bool HasPermission(string permission)
        {
            if (!IsLoggedIn)
                return false;

            // Permisiuni bazate pe rol
            return permission switch
            {
                // Permisiuni StoreOwner
                "ManageProducts" => IsStoreOwner,
                "ManageInventory" => IsStoreOwner,
                "ManageOrders" => IsStoreOwner || IsCustomerService,
                "ManageSettings" => IsStoreOwner,
                "ViewReports" => IsStoreOwner,

                // Permisiuni Customer
                "PlaceOrders" => IsCustomer,
                "WriteReviews" => IsCustomer,
                "CreateTickets" => IsCustomer,

                // Permisiuni CustomerService
                "ManageTickets" => IsCustomerService,
                "ViewCustomers" => IsCustomerService || IsStoreOwner,

                // Default: nu are permisiunea
                _ => false
            };
        }
    }
}
