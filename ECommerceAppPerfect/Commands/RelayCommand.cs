using System;
using System.Windows.Input;

namespace ECommerceAppPerfect.Commands
{
    // CLASA RELAYCOMMAND - Implementare ICommand pentru MVVM
    //
    // CE ESTE ICOMMAND?
    // ICommand este interfata din WPF pentru actiuni declansate de UI
    // Permite separarea logicii de UI (MVVM pattern)
    // Butoanele, meniurile, shortcut-urile pot fi legate de comenzi
    //
    // DE CE AVEM NEVOIE DE RELAYCOMMAND?
    // In WPF, butoanele au proprietatea Command
    // Poti lega Command de un ICommand din ViewModel
    // Problema: ICommand e o interfata, ai nevoie de implementare
    // RelayCommand e implementarea standard in MVVM
    //
    // CUM FUNCTIONEAZA?
    // 1. ViewModel-ul creeaza un RelayCommand cu actiunea dorita
    // 2. View-ul leaga Button.Command de acest RelayCommand
    // 3. Cand utilizatorul apasa butonul, se executa actiunea
    //
    // EXEMPLU IN VIEWMODEL:
    // public ICommand SaveCommand { get; }
    //
    // public MyViewModel()
    // {
    //     SaveCommand = new RelayCommand(
    //         execute: param => Save(),
    //         canExecute: param => CanSave()
    //     );
    // }
    //
    // private void Save()
    // {
    //     // Logica de salvare
    // }
    //
    // private bool CanSave()
    // {
    //     // Returneaza true daca se poate salva
    //     return !string.IsNullOrEmpty(Name);
    // }
    //
    // EXEMPLU IN XAML:
    // <Button Content="Save" Command="{Binding SaveCommand}" />
    //
    // AVANTAJE MVVM:
    // 1. Testabilitate: Poti testa ViewModel-ul fara UI
    // 2. Separare: Logica e in ViewModel, UI e in View
    // 3. Reusability: Aceeasi comanda poate fi legata la mai multe controale
    public class RelayCommand : ICommand
    {
        // CAMPURI PRIVATE

        // _execute - Actiunea de executat
        //
        // Este un ACTION<object> pentru ca poate primi un parametru
        // Parametrul vine din CommandParameter in XAML
        //
        // EXEMPLU:
        // <Button Command="{Binding DeleteCommand}" CommandParameter="{Binding SelectedItem}" />
        // La click, se apeleaza Delete(SelectedItem)
        private readonly Action<object> _execute;

        // _canExecute - Conditia de executie
        //
        // Este un PREDICATE<object> (functie care returneaza bool)
        // Returneaza true daca comanda poate fi executata
        // Returneaza false daca comanda e dezactivata
        //
        // EFECT UI:
        // Daca CanExecute returneaza false, butonul devine dezactivat (grayed out)
        // WPF face asta automat prin data binding
        //
        // POATE FI NULL - daca e null, comanda e mereu activa
        private readonly Predicate<object> _canExecute;

        // CONSTRUCTORUL - Creeaza o noua comanda
        //
        // PARAMETRI:
        // - execute: Actiunea de executat (OBLIGATORIU)
        // - canExecute: Conditia de executie (OPTIONAL, default: mereu true)
        //
        // FOLOSIRE:
        // // Comanda simpla (mereu activa):
        // new RelayCommand(param => DoSomething())
        //
        // // Comanda cu conditie:
        // new RelayCommand(
        //     param => Save(),
        //     param => CanSave()
        // )
        //
        // // Comanda cu parametru:
        // new RelayCommand(
        //     param => Delete((Product)param),
        //     param => param != null
        // )
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            // Validare: execute nu poate fi null
            // Fara actiune, comanda nu are sens
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));

            // canExecute poate fi null (comanda mereu activa)
            _canExecute = canExecute;
        }

        // EVENIMENTUL CanExecuteChanged - Notifica ca starea s-a schimbat
        //
        // CE ESTE ACEST EVENIMENT?
        // Cand conditia de executie se schimba, UI trebuie notificat
        // Altfel, butonul nu ar stii ca trebuie sa se activeze/dezactiveze
        //
        // CUM FUNCTIONEAZA?
        // In loc sa gestionam noi evenimentul, delegam la CommandManager
        // CommandManager.RequerySuggested este un eveniment global
        // Se declanseaza cand WPF crede ca trebuie reverificat CanExecute
        // (la focus change, dupa input, etc.)
        //
        // AVANTAJ:
        // Nu trebuie sa apelam manual OnCanExecuteChanged()
        // WPF verifica automat periodic
        //
        // DEZAVANTAJ:
        // Poate fi prea frecvent pentru aplicatii complexe
        // Solutie: InvalidateRequerySuggested() manual
        public event EventHandler CanExecuteChanged
        {
            // add: Cand cineva se aboneaza la evenimentul nostru,
            // il abonam de fapt la RequerySuggested
            add { CommandManager.RequerySuggested += value; }

            // remove: La dezabonare, dezabonam de la RequerySuggested
            remove { CommandManager.RequerySuggested -= value; }
        }

        // METODA CanExecute - Verifica daca comanda poate fi executata
        //
        // IMPLEMENTARE ICOMMAND
        // Aceasta metoda e apelata de WPF pentru a stii daca butonul e activ
        //
        // PARAMETRU:
        // - parameter: Valoarea din CommandParameter (poate fi null)
        //
        // RETURNEAZA:
        // - true: Comanda poate fi executata (buton activ)
        // - false: Comanda nu poate fi executata (buton dezactivat)
        //
        // LOGICA:
        // Daca _canExecute e null (nu s-a specificat conditie), returnam true
        // Altfel, apelam _canExecute cu parametrul primit
        public bool CanExecute(object parameter)
        {
            // Daca nu e specificata conditie, comanda e mereu activa
            return _canExecute == null || _canExecute(parameter);
        }

        // METODA Execute - Executa comanda
        //
        // IMPLEMENTARE ICOMMAND
        // Aceasta metoda e apelata de WPF cand utilizatorul activeaza comanda
        // (click pe buton, shortcut, etc.)
        //
        // PARAMETRU:
        // - parameter: Valoarea din CommandParameter
        //
        // EXEMPLU:
        // CommandParameter="{Binding SelectedProduct}"
        // La Execute, parameter = SelectedProduct
        public void Execute(object parameter)
        {
            // Executa actiunea cu parametrul dat
            _execute(parameter);
        }

        // METODA HELPER RaiseCanExecuteChanged - Forteaza reverificarea
        //
        // CAND O FOLOSIM?
        // Cand stim ca starea s-a schimbat si vrem sa fortam UI sa se actualizeze
        //
        // EXEMPLU:
        // // Dupa ce s-a salvat, comanda SaveCommand nu mai e activa
        // // (nu mai sunt modificari de salvat)
        // IsSaved = true;
        // SaveCommand.RaiseCanExecuteChanged();
        //
        // IMPLEMENTARE:
        // Apeleaza CommandManager.InvalidateRequerySuggested()
        // Aceasta declanseaza reverificarea TUTUROR comenzilor
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
