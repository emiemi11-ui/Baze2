using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA VIEWMODELBASE - Clasa de baza pentru toate ViewModels
    //
    // CE ESTE UN VIEWMODEL?
    // ViewModel-ul este componenta centrala a pattern-ului MVVM
    // (Model-View-ViewModel) care face legatura intre datele aplicatiei
    // (Model) si interfata cu utilizatorul (View)
    //
    // PATTERN-UL MVVM - EXPLICATIE COMPLETA:
    //
    // 1. MODEL - Datele aplicatiei
    //    - Clasele din Models/ (User, Product, Order, etc.)
    //    - Reprezinta structura datelor din baza de date
    //    - Nu stie nimic despre UI
    //
    // 2. VIEW - Interfata utilizator
    //    - Fisierele XAML (LoginView.xaml, ProductsView.xaml, etc.)
    //    - Defineste CUM arata aplicatia
    //    - Nu contine logica de business
    //
    // 3. VIEWMODEL - Logica de prezentare
    //    - Face legatura intre Model si View
    //    - Contine proprietati pe care View-ul le afiseaza
    //    - Contine comenzi pe care View-ul le executa
    //    - Notifica View-ul cand datele se schimba
    //
    // DE CE AVEM NEVOIE DE VIEWMODELBASE?
    // Toate ViewModels au nevoie de:
    // - Notificarea UI-ului cand proprietatile se schimba (INotifyPropertyChanged)
    // - Metode helper pentru setarea proprietatilor
    // - Eventual, implementarea IDisposable pentru cleanup
    //
    // In loc sa implementam aceste lucruri in fiecare ViewModel,
    // le punem intr-o clasa de baza pe care o mostenesc toate
    //
    // INOTIFYPROPERTYCHANGED - CE FACE?
    // Interfata INotifyPropertyChanged permite unui obiect sa notifice
    // ca o proprietate s-a schimbat. WPF asculta aceste notificari
    // si actualizeaza automat UI-ul (Binding-urile)
    //
    // EXEMPLU FARA NOTIFICARE:
    // Username = "John"; // UI nu se actualizeaza!
    //
    // EXEMPLU CU NOTIFICARE:
    // Username = "John";
    // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Username"));
    // // UI se actualizeaza automat!
    //
    // CUM FUNCTIONEAZA DATA BINDING-UL IN WPF?
    //
    // 1. In XAML, legam un control de o proprietate:
    //    <TextBox Text="{Binding Username}" />
    //
    // 2. WPF seteaza DataContext al View-ului la ViewModel:
    //    view.DataContext = viewModel;
    //
    // 3. Cand proprietatea Username se schimba in ViewModel,
    //    se declanseaza PropertyChanged
    //
    // 4. WPF prinde acest eveniment si actualizeaza TextBox-ul
    //
    // FLOW COMPLET:
    // ViewModel.Username = "John"
    //    -> PropertyChanged("Username")
    //       -> WPF intercepteaza
    //          -> TextBox.Text = "John"
    //
    // AVANTAJELE MVVM:
    // 1. SEPARARE - UI-ul e separat de logica
    // 2. TESTABILITATE - Poti testa ViewModel-ul fara UI
    // 3. DESIGNER-FRIENDLY - Designerii lucreaza pe XAML, programatorii pe C#
    // 4. REUTILIZARE - Acelasi ViewModel poate avea multiple Views
    // 5. MENTENANCE - Modificari in logica nu afecteaza UI-ul si viceversa
    public class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        // EVENIMENTUL PropertyChanged - Nucleul INotifyPropertyChanged
        //
        // CE ESTE ACEST EVENIMENT?
        // Este mecanismul prin care ViewModel-ul comunica cu View-ul
        // Cand o proprietate se schimba, declansam acest eveniment
        // WPF e abonat automat si actualizeaza binding-urile
        //
        // CINE ASCULTA?
        // - WPF Binding Engine - pentru actualizarea UI-ului
        // - Alte ViewModels care depind de aceasta (prin referinta)
        // - Orice alt cod care vrea sa stie de schimbari
        //
        // CAND SE DECLANSEAZA?
        // De fiecare data cand apelam OnPropertyChanged("NumeProprietate")
        //
        // FORMAT EVENIMENT:
        // PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        // - sender: obiectul care a declansat (this)
        // - e.PropertyName: numele proprietatii schimbate
        public event PropertyChangedEventHandler PropertyChanged;

        // METODA OnPropertyChanged - Notifica ca o proprietate s-a schimbat
        //
        // PARAMETRI:
        // - propertyName: Numele proprietatii care s-a schimbat
        //   [CallerMemberName] permite apelarea fara parametru din proprietate
        //
        // CUM FUNCTIONEAZA [CallerMemberName]?
        // Acest atribut injecteaza automat numele metodei/proprietatii apelante
        //
        // EXEMPLU:
        // public string Username
        // {
        //     set
        //     {
        //         _username = value;
        //         OnPropertyChanged(); // echivalent cu OnPropertyChanged("Username")
        //     }
        // }
        //
        // Compilatorul transforma OnPropertyChanged() in OnPropertyChanged("Username")
        // pentru ca apelul vine din proprietatea Username
        //
        // DE CE E UTIL?
        // 1. Mai putin cod de scris
        // 2. Refactoring-safe (daca redenumesti proprietatea, se actualizeaza automat)
        // 3. Previne greseli de scriere ("Usernmae" in loc de "Username")
        //
        // PROTECTED VIRTUAL:
        // - protected: poate fi apelata din clase derivate
        // - virtual: poate fi suprascrisa in clase derivate (override)
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // INVOCARE EVENIMENT
            //
            // ?. este null-conditional operator
            // Daca PropertyChanged e null (nimeni nu e abonat), nu face nimic
            // Daca nu e null, apeleaza Invoke cu parametrii dati
            //
            // Echivalent cu:
            // if (PropertyChanged != null)
            //     PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //
            // Dar mai concis si thread-safe
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // METODA SetProperty<T> - Helper pentru setarea proprietatilor cu notificare
        //
        // CE FACE ACEASTA METODA?
        // Simplifica pattern-ul comun de:
        // 1. Verificare daca valoarea e diferita
        // 2. Setare noua valoare
        // 3. Notificare schimbare
        //
        // GENERIC T:
        // T poate fi orice tip (string, int, Product, etc.)
        // Metoda functioneaza cu orice tip de proprietate
        //
        // PARAMETRI:
        // - field: Referinta la campul privat (ref permite modificare)
        // - value: Noua valoare de setat
        // - propertyName: Numele proprietatii (optional, autodetectat)
        //
        // RETURNEAZA:
        // - true daca valoarea s-a schimbat
        // - false daca valoarea e aceeasi (nu s-a schimbat nimic)
        //
        // EXEMPLU FOLOSIRE:
        //
        // INAINTE (manual):
        // private string _username;
        // public string Username
        // {
        //     get => _username;
        //     set
        //     {
        //         if (_username != value)
        //         {
        //             _username = value;
        //             OnPropertyChanged(nameof(Username));
        //         }
        //     }
        // }
        //
        // DUPA (cu SetProperty):
        // private string _username;
        // public string Username
        // {
        //     get => _username;
        //     set => SetProperty(ref _username, value);
        // }
        //
        // AVANTAJE:
        // - Cod mai concis
        // - Mai putin loc pentru erori
        // - Pattern consistent in toata aplicatia
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            // VERIFICARE DACA VALOAREA E ACEEASI
            //
            // EqualityComparer<T>.Default compara corect orice tip:
            // - Pentru tipuri reference: compara referintele (sau foloseste Equals daca e implementat)
            // - Pentru tipuri value: compara valorile
            // - Pentru null: gestioneaza corect cazurile null
            //
            // DE CE NU FOLOSIM == DIRECT?
            // Operatorul == poate sa nu fie definit pentru tipul T
            // EqualityComparer functioneaza mereu corect
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                // Valoarea e aceeasi, nu facem nimic
                // Returnam false pentru a indica ca nu s-a schimbat
                return false;
            }

            // SETARE NOUA VALOARE
            // field e referinta (ref), deci modificam campul original
            field = value;

            // NOTIFICARE SCHIMBARE
            // Apelam OnPropertyChanged pentru a actualiza UI-ul
            OnPropertyChanged(propertyName);

            // RETURNAM TRUE
            // Indica ca valoarea s-a schimbat
            // Util pentru logica aditionala in setter
            return true;
        }

        // METODA SetProperty<T> CU CALLBACK - Varianta cu actiune suplimentara
        //
        // Uneori, cand o proprietate se schimba, vrei sa faci ceva in plus
        // Aceasta varianta permite specificarea unei actiuni de executat dupa schimbare
        //
        // PARAMETRI:
        // - field: Referinta la campul privat
        // - value: Noua valoare
        // - onChanged: Actiunea de executat dupa schimbare
        // - propertyName: Numele proprietatii
        //
        // EXEMPLU:
        // private Category _selectedCategory;
        // public Category SelectedCategory
        // {
        //     get => _selectedCategory;
        //     set => SetProperty(ref _selectedCategory, value, () => LoadProductsForCategory());
        // }
        //
        // Cand se selecteaza o noua categorie, se incarca automat produsele
        protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            // Apelam versiunea de baza
            if (!SetProperty(ref field, value, propertyName))
            {
                // Valoarea nu s-a schimbat, nu executam callback-ul
                return false;
            }

            // Valoarea s-a schimbat, executam callback-ul
            // ?. pentru cazul in care onChanged e null
            onChanged?.Invoke();

            return true;
        }

        // METODA OnPropertyChanged PENTRU PROPRIETATI DEPENDENTE
        //
        // Uneori, cand o proprietate se schimba, alte proprietati calculate
        // depind de ea si trebuie notificate si ele
        //
        // PARAMETRI:
        // - propertyNames: Lista de nume de proprietati de notificat
        //
        // EXEMPLU:
        // Cand _price sau _quantity se schimba, trebuie sa notificam si Total:
        // SetProperty(ref _price, value);
        // OnPropertiesChanged("Price", "Total", "TotalFormatted");
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            // Iteram prin toate numele si notificam fiecare
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        // IMPLEMENTAREA IDISPOSABLE - Pentru eliberarea resurselor
        //
        // CE ESTE IDISPOSABLE?
        // Este un pattern .NET pentru eliberarea resurselor (connections, files, etc.)
        // Cand un obiect implementeaza IDisposable, trebuie apelat Dispose()
        // cand nu mai e nevoie de el
        //
        // DE CE AVEM NEVOIE IN VIEWMODELS?
        // ViewModels pot avea:
        // - Event handlers abonati (trebuie dezabonati)
        // - Conexiuni la baze de date
        // - Timere
        // - Alte resurse
        //
        // CAND SE APELEAZA DISPOSE?
        // NavigationStore apeleaza Dispose cand se navigheaza la alt ViewModel
        // Sau la inchiderea aplicatiei

        // Flag pentru a preveni apelarea multipla a Dispose
        private bool _isDisposed;

        // Dispose() - Metoda publica pentru eliberarea resurselor
        //
        // Aceasta e metoda apelata de exterior
        // Implementeaza pattern-ul Dispose complet
        public void Dispose()
        {
            // Apelam Dispose(true) pentru a elibera resursele
            Dispose(true);

            // Spunem GC sa nu mai apeleze finalizatorul
            // pentru ca am eliberat deja resursele manual
            GC.SuppressFinalize(this);
        }

        // Dispose(bool) - Metoda protejata pentru cleanup
        //
        // PARAMETRU disposing:
        // - true: apelat din Dispose() - eliberam resurse managed si unmanaged
        // - false: apelat din finalizer - eliberam doar resurse unmanaged
        //
        // PATTERN STANDARD pentru IDisposable
        protected virtual void Dispose(bool disposing)
        {
            // Verificam daca am eliberat deja resursele
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // ELIBERARE RESURSE MANAGED
                // Aici clasele derivate pot face cleanup
                // Exemplu: dezabonare de la evenimente, inchidere conexiuni
            }

            // ELIBERARE RESURSE UNMANAGED
            // (de obicei nu avem in ViewModels)

            // Marcam ca disposed
            _isDisposed = true;
        }
    }
}
