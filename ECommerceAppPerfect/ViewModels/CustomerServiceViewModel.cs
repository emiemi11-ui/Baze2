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
    // CLASA CUSTOMERSERVICEVIEWMODEL - Management ticket-uri de suport
    //
    // CE ESTE ACEST VIEWMODEL?
    // Gestioneaza sistemul de suport pentru clienti prin ticket-uri
    // Folosit de:
    // - AGENTI (CustomerService): Vad toate ticket-urile, le rezolva
    // - CLIENTI: Vad doar ticket-urile proprii, pot crea noi ticket-uri
    //
    // WORKFLOW TICKET:
    // 1. CLIENT creeaza ticket (Open) - descrie problema
    // 2. AGENT preia ticket (InProgress) - se asigneaza
    // 3. AGENT si CLIENT comunica prin MESAJE
    // 4. AGENT rezolva problema (Resolved)
    // 5. Ticket se inchide (Closed)
    //
    // RELATII DEMONSTRATE:
    // - Customer -> SupportTickets (One-to-Many): Un client poate deschide multe ticket-uri
    // - Agent -> SupportTickets (One-to-Many): Un agent poate avea multe ticket-uri asignate
    // - SupportTicket -> TicketMessages (One-to-Many): Un ticket are multe mesaje
    // - TicketMessage -> User (Many-to-One): Fiecare mesaj are un autor
    //
    // UI PENTRU AGENT:
    // +----------------------------------------------------------------+
    // | INBOX                                   | TICKET DETAILS       |
    // |----------------------------------------|----------------------|
    // | [!] TKT-00001 - Payment Issue  HIGH    | Subject: Payment... |
    // | [ ] TKT-00002 - Delivery Delay MEDIUM  | Customer: John Doe  |
    // | [ ] TKT-00003 - Product Return LOW     | Status: InProgress  |
    // |                                         | Priority: High      |
    // |                                         |                     |
    // |                                         | MESSAGES:           |
    // |                                         | [Customer] I have.. |
    // |                                         | [Agent] Thank you.. |
    // |                                         |                     |
    // |                                         | [Reply textbox]     |
    // |                                         | [Send] [Resolve]    |
    // +----------------------------------------------------------------+
    public class CustomerServiceViewModel : ViewModelBase
    {
        // STORES

        // _currentUserStore - Informatii despre utilizatorul curent
        //
        // Folosit pentru:
        // - A determina daca e agent sau client
        // - A filtra ticket-urile corespunzator
        // - A identifica autorul mesajelor
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare
        private readonly NavigationStore _navigationStore;

        // COLECTII

        // _allTickets - Lista completa de ticket-uri
        private List<SupportTicket> _allTickets;

        // _tickets - Lista filtrata expusa catre UI
        private ObservableCollection<SupportTicket> _tickets;

        // _messages - Mesajele ticket-ului selectat
        //
        // RELATIE ONE-TO-MANY: SupportTicket -> TicketMessages
        private ObservableCollection<TicketMessage> _messages;

        // _agents - Lista de agenti (pentru asignare)
        private ObservableCollection<User> _agents;

        // CAMPURI PROPRIETATI

        // Ticket-ul selectat
        private SupportTicket _selectedTicket;

        // Mesajul de trimis
        private string _newMessageText;

        // Filtrul de status
        private string _selectedStatusFilter;

        // Filtrul de prioritate
        private string _selectedPriorityFilter;

        // Agentul selectat pentru asignare
        private User _selectedAgentForAssignment;

        // Flag incarcare
        private bool _isLoading;

        // Mesaj eroare
        private string _errorMessage;

        // Pentru creare ticket nou - subiect
        private string _newTicketSubject;

        // Pentru creare ticket nou - descriere
        private string _newTicketDescription;

        // Pentru creare ticket nou - prioritate
        private string _newTicketPriority;

        // CONSTRUCTOR
        public CustomerServiceViewModel(
            CurrentUserStore currentUserStore,
            NavigationStore navigationStore)
        {
            // Salvare dependinte
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare colectii
            _tickets = new ObservableCollection<SupportTicket>();
            _messages = new ObservableCollection<TicketMessage>();
            _agents = new ObservableCollection<User>();
            _allTickets = new List<SupportTicket>();

            // Valori default
            _selectedStatusFilter = "All";
            _selectedPriorityFilter = "All";
            _newTicketPriority = "Medium";

            // Initializare comenzi
            SendMessageCommand = new RelayCommand(ExecuteSendMessage, CanExecuteSendMessage);
            AssignTicketCommand = new RelayCommand(ExecuteAssignTicket, CanExecuteAssignTicket);
            TakeTicketCommand = new RelayCommand(ExecuteTakeTicket, CanExecuteTakeTicket);
            ResolveTicketCommand = new RelayCommand(ExecuteResolveTicket, CanExecuteResolveTicket);
            CloseTicketCommand = new RelayCommand(ExecuteCloseTicket, CanExecuteCloseTicket);
            ReopenTicketCommand = new RelayCommand(ExecuteReopenTicket, CanExecuteReopenTicket);
            CreateTicketCommand = new RelayCommand(ExecuteCreateTicket, CanExecuteCreateTicket);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);

            // Incarcare date
            LoadData();
        }

        // PROPRIETATI - Informatii utilizator

        // IsAgent - Este utilizatorul curent un agent?
        //
        // Determina ce functionalitati sunt disponibile
        public bool IsAgent => _currentUserStore.IsCustomerService || _currentUserStore.IsStoreOwner;

        // IsCustomer - Este utilizatorul curent un client?
        public bool IsCustomer => _currentUserStore.IsCustomer;

        // CurrentUserName - Numele utilizatorului curent
        public string CurrentUserName => _currentUserStore.CurrentUserFullName;

        // PROPRIETATI - Colectii

        // Tickets - Lista de ticket-uri afisata
        //
        // Pentru AGENTI: toate ticket-urile (sau filtrate)
        // Pentru CLIENTI: doar ticket-urile proprii
        public ObservableCollection<SupportTicket> Tickets
        {
            get => _tickets;
            set => SetProperty(ref _tickets, value);
        }

        // Messages - Mesajele ticket-ului selectat
        //
        // RELATIE ONE-TO-MANY: SupportTicket -> TicketMessages
        // BINDING:
        // <ItemsControl ItemsSource="{Binding Messages}">
        //     <ItemsControl.ItemTemplate>
        //         <DataTemplate>
        //             <Border Background="{Binding BubbleColor}"
        //                     HorizontalAlignment="{Binding BubbleAlignment}">
        //                 <StackPanel>
        //                     <TextBlock Text="{Binding SenderName}" FontWeight="Bold" />
        //                     <TextBlock Text="{Binding MessageText}" TextWrapping="Wrap" />
        //                     <TextBlock Text="{Binding TimeAgo}" FontSize="10" />
        //                 </StackPanel>
        //             </Border>
        //         </DataTemplate>
        //     </ItemsControl.ItemTemplate>
        // </ItemsControl>
        public ObservableCollection<TicketMessage> Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        // Agents - Lista de agenti pentru dropdown de asignare
        public ObservableCollection<User> Agents
        {
            get => _agents;
            set => SetProperty(ref _agents, value);
        }

        // PROPRIETATI - Selectie

        // SelectedTicket - Ticket-ul selectat
        //
        // La selectare, se incarca mesajele
        public SupportTicket SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                if (SetProperty(ref _selectedTicket, value))
                {
                    // Incarca mesajele ticket-ului
                    LoadMessages();

                    // Notifica proprietatile dependente
                    NotifySelectedTicketProperties();

                    // Reverifica comenzile
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // SelectedAgentForAssignment - Agentul selectat pentru asignare
        public User SelectedAgentForAssignment
        {
            get => _selectedAgentForAssignment;
            set => SetProperty(ref _selectedAgentForAssignment, value);
        }

        // PROPRIETATI - Filtre

        // SelectedStatusFilter - Filtrul de status
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        // SelectedPriorityFilter - Filtrul de prioritate
        public string SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        // PROPRIETATI - Mesaj nou

        // NewMessageText - Textul mesajului de trimis
        public string NewMessageText
        {
            get => _newMessageText;
            set => SetProperty(ref _newMessageText, value);
        }

        // PROPRIETATI - Ticket nou (pentru clienti)

        // NewTicketSubject - Subiectul ticket-ului nou
        public string NewTicketSubject
        {
            get => _newTicketSubject;
            set => SetProperty(ref _newTicketSubject, value);
        }

        // NewTicketDescription - Descrierea ticket-ului nou
        public string NewTicketDescription
        {
            get => _newTicketDescription;
            set => SetProperty(ref _newTicketDescription, value);
        }

        // NewTicketPriority - Prioritatea ticket-ului nou
        public string NewTicketPriority
        {
            get => _newTicketPriority;
            set => SetProperty(ref _newTicketPriority, value);
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

        // HasSelection - Este selectat un ticket?
        public bool HasSelection => SelectedTicket != null;

        // SelectedTicketNumber - Numarul ticket-ului selectat
        public string SelectedTicketNumber => SelectedTicket?.TicketNumber ?? "No selection";

        // SelectedTicketSubject - Subiectul ticket-ului selectat
        public string SelectedTicketSubject => SelectedTicket?.Subject ?? "N/A";

        // SelectedTicketDescription - Descrierea ticket-ului selectat
        public string SelectedTicketDescription => SelectedTicket?.Description ?? "N/A";

        // SelectedTicketStatus - Statusul ticket-ului selectat
        public string SelectedTicketStatus => SelectedTicket?.Status ?? "N/A";

        // SelectedTicketPriority - Prioritatea ticket-ului selectat
        public string SelectedTicketPriority => SelectedTicket?.Priority ?? "N/A";

        // SelectedTicketCustomerName - Numele clientului
        //
        // DEMONSTRARE RELATIE MANY-TO-ONE:
        // SelectedTicket.Customer.FullName
        public string SelectedTicketCustomerName => SelectedTicket?.CustomerName ?? "N/A";

        // SelectedTicketAssignedTo - Agentul asignat
        //
        // DEMONSTRARE RELATIE MANY-TO-ONE:
        // SelectedTicket.AssignedTo.FullName
        public string SelectedTicketAssignedTo => SelectedTicket?.AssignedToName ?? "Unassigned";

        // SelectedTicketMessageCount - Cate mesaje are
        public int SelectedTicketMessageCount => SelectedTicket?.MessageCount ?? 0;

        // SelectedTicketIsOpen - Este deschis?
        public bool SelectedTicketIsOpen => SelectedTicket?.IsOpen ?? false;

        // SelectedTicketIsAssigned - Este asignat?
        public bool SelectedTicketIsAssigned => SelectedTicket?.IsAssigned ?? false;

        // PROPRIETATI CALCULATE - Statistici

        // TotalTicketsCount - Total ticket-uri
        public int TotalTicketsCount => _allTickets?.Count ?? 0;

        // OpenTicketsCount - Ticket-uri deschise
        public int OpenTicketsCount => _allTickets?.Count(t => t.IsOpen) ?? 0;

        // HighPriorityCount - Ticket-uri cu prioritate mare
        public int HighPriorityCount => _allTickets?.Count(t => t.Priority == "High" && t.IsOpen) ?? 0;

        // UnassignedCount - Ticket-uri neasignate
        public int UnassignedCount => _allTickets?.Count(t => !t.IsAssigned && t.IsOpen) ?? 0;

        // COMENZI

        // SendMessageCommand - Trimite un mesaj in ticket
        public ICommand SendMessageCommand { get; }

        // AssignTicketCommand - Asigneaza ticket-ul unui agent
        public ICommand AssignTicketCommand { get; }

        // TakeTicketCommand - Preia ticket-ul (se asigneaza sie)
        public ICommand TakeTicketCommand { get; }

        // ResolveTicketCommand - Marcheaza ticket-ul ca rezolvat
        public ICommand ResolveTicketCommand { get; }

        // CloseTicketCommand - Inchide ticket-ul
        public ICommand CloseTicketCommand { get; }

        // ReopenTicketCommand - Redeschide ticket-ul
        public ICommand ReopenTicketCommand { get; }

        // CreateTicketCommand - Creeaza un ticket nou (pentru clienti)
        public ICommand CreateTicketCommand { get; }

        // RefreshCommand - Reincarca datele
        public ICommand RefreshCommand { get; }

        // METODE INCARCARE DATE

        // LoadData - Incarca ticket-urile
        private void LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Incarcam ticket-urile in functie de rol
                if (IsAgent)
                {
                    // Agentii vad toate ticket-urile (sau doar cele asignate lor)
                    // _allTickets = _ticketService.GetAllTickets();
                    _allTickets = new List<SupportTicket>();
                }
                else
                {
                    // Clientii vad doar ticket-urile proprii
                    // _allTickets = _ticketService.GetTicketsByCustomer(_currentUserStore.CurrentUserId);
                    _allTickets = new List<SupportTicket>();
                }

                // Incarcam lista de agenti (pentru dropdown asignare)
                if (IsAgent)
                {
                    LoadAgents();
                }

                // Aplicam filtrele
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load tickets: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
                NotifyStatistics();
            }
        }

        // LoadAgents - Incarca lista de agenti
        private void LoadAgents()
        {
            Agents.Clear();

            // In productie: _userService.GetCustomerServiceAgents()
            // Pentru demonstratie, lista goala
        }

        // LoadMessages - Incarca mesajele pentru ticket-ul selectat
        //
        // DEMONSTRARE RELATIE ONE-TO-MANY:
        // SelectedTicket.TicketMessages
        private void LoadMessages()
        {
            Messages.Clear();

            if (SelectedTicket?.TicketMessages == null)
                return;

            // Sortam cronologic
            var sortedMessages = SelectedTicket.TicketMessages.OrderBy(m => m.MessageDate);

            foreach (var message in sortedMessages)
            {
                Messages.Add(message);
            }
        }

        // ApplyFilters - Aplica filtrele
        private void ApplyFilters()
        {
            Tickets.Clear();

            IEnumerable<SupportTicket> filtered = _allTickets;

            // Filtru status
            if (SelectedStatusFilter != "All" && !string.IsNullOrEmpty(SelectedStatusFilter))
            {
                filtered = filtered.Where(t => t.Status == SelectedStatusFilter);
            }

            // Filtru prioritate
            if (SelectedPriorityFilter != "All" && !string.IsNullOrEmpty(SelectedPriorityFilter))
            {
                filtered = filtered.Where(t => t.Priority == SelectedPriorityFilter);
            }

            // Sortam: High priority primele, apoi dupa data
            filtered = filtered
                .OrderByDescending(t => t.Priority == "High")
                .ThenByDescending(t => t.LastMessageDate ?? t.CreatedDate);

            foreach (var ticket in filtered)
            {
                Tickets.Add(ticket);
            }
        }

        // NotifySelectedTicketProperties - Notifica toate proprietatile despre selectie
        private void NotifySelectedTicketProperties()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectedTicketNumber));
            OnPropertyChanged(nameof(SelectedTicketSubject));
            OnPropertyChanged(nameof(SelectedTicketDescription));
            OnPropertyChanged(nameof(SelectedTicketStatus));
            OnPropertyChanged(nameof(SelectedTicketPriority));
            OnPropertyChanged(nameof(SelectedTicketCustomerName));
            OnPropertyChanged(nameof(SelectedTicketAssignedTo));
            OnPropertyChanged(nameof(SelectedTicketMessageCount));
            OnPropertyChanged(nameof(SelectedTicketIsOpen));
            OnPropertyChanged(nameof(SelectedTicketIsAssigned));
        }

        // NotifyStatistics - Notifica statisticile
        private void NotifyStatistics()
        {
            OnPropertyChanged(nameof(TotalTicketsCount));
            OnPropertyChanged(nameof(OpenTicketsCount));
            OnPropertyChanged(nameof(HighPriorityCount));
            OnPropertyChanged(nameof(UnassignedCount));
        }

        // IMPLEMENTARI COMENZI

        // ExecuteSendMessage - Trimite un mesaj
        private void ExecuteSendMessage(object parameter)
        {
            if (SelectedTicket == null || string.IsNullOrWhiteSpace(NewMessageText))
                return;

            try
            {
                // Cream mesajul nou
                var message = new TicketMessage
                {
                    TicketID = SelectedTicket.TicketID,
                    UserID = _currentUserStore.CurrentUserId,
                    MessageText = NewMessageText,
                    MessageDate = DateTime.Now,
                    IsFromCustomer = IsCustomer
                };

                // Adaugam in colectia locala
                SelectedTicket.TicketMessages.Add(message);
                Messages.Add(message);

                // In productie: _ticketService.AddMessage(message);

                // Curatam textbox-ul
                NewMessageText = string.Empty;

                OnPropertyChanged(nameof(SelectedTicketMessageCount));
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to send message: " + ex.Message;
            }
        }

        // CanExecuteSendMessage - Se poate trimite?
        private bool CanExecuteSendMessage(object parameter)
        {
            return SelectedTicket != null &&
                   SelectedTicket.IsOpen &&
                   !string.IsNullOrWhiteSpace(NewMessageText);
        }

        // ExecuteAssignTicket - Asigneaza ticket-ul
        private void ExecuteAssignTicket(object parameter)
        {
            if (SelectedTicket == null || SelectedAgentForAssignment == null)
                return;

            SelectedTicket.AssignedToID = SelectedAgentForAssignment.UserID;
            SelectedTicket.AssignedTo = SelectedAgentForAssignment;

            if (SelectedTicket.Status == "Open")
            {
                SelectedTicket.Status = "InProgress";
            }

            NotifySelectedTicketProperties();
        }

        // CanExecuteAssignTicket - Se poate asigna?
        private bool CanExecuteAssignTicket(object parameter)
        {
            return IsAgent && SelectedTicket != null && SelectedAgentForAssignment != null;
        }

        // ExecuteTakeTicket - Preia ticket-ul
        private void ExecuteTakeTicket(object parameter)
        {
            if (SelectedTicket == null)
                return;

            SelectedTicket.AssignedToID = _currentUserStore.CurrentUserId;
            SelectedTicket.AssignedTo = _currentUserStore.CurrentUser;
            SelectedTicket.Status = "InProgress";

            NotifySelectedTicketProperties();
        }

        // CanExecuteTakeTicket - Se poate prelua?
        private bool CanExecuteTakeTicket(object parameter)
        {
            return IsAgent && SelectedTicket != null && !SelectedTicket.IsAssigned;
        }

        // ExecuteResolveTicket - Rezolva ticket-ul
        private void ExecuteResolveTicket(object parameter)
        {
            if (SelectedTicket == null)
                return;

            SelectedTicket.Status = "Resolved";
            SelectedTicket.ResolvedDate = DateTime.Now;

            NotifySelectedTicketProperties();
            NotifyStatistics();
        }

        // CanExecuteResolveTicket - Se poate rezolva?
        private bool CanExecuteResolveTicket(object parameter)
        {
            return IsAgent && SelectedTicket != null && SelectedTicket.IsOpen;
        }

        // ExecuteCloseTicket - Inchide ticket-ul
        private void ExecuteCloseTicket(object parameter)
        {
            if (SelectedTicket == null)
                return;

            SelectedTicket.Status = "Closed";

            NotifySelectedTicketProperties();
            NotifyStatistics();
        }

        // CanExecuteCloseTicket - Se poate inchide?
        private bool CanExecuteCloseTicket(object parameter)
        {
            return SelectedTicket != null && SelectedTicket.Status == "Resolved";
        }

        // ExecuteReopenTicket - Redeschide ticket-ul
        private void ExecuteReopenTicket(object parameter)
        {
            if (SelectedTicket == null)
                return;

            SelectedTicket.Status = "Open";
            SelectedTicket.ResolvedDate = null;

            NotifySelectedTicketProperties();
            NotifyStatistics();
        }

        // CanExecuteReopenTicket - Se poate redeschide?
        private bool CanExecuteReopenTicket(object parameter)
        {
            return SelectedTicket != null &&
                   (SelectedTicket.Status == "Resolved" || SelectedTicket.Status == "Closed");
        }

        // ExecuteCreateTicket - Creeaza ticket nou
        private void ExecuteCreateTicket(object parameter)
        {
            if (string.IsNullOrWhiteSpace(NewTicketSubject) ||
                string.IsNullOrWhiteSpace(NewTicketDescription))
                return;

            try
            {
                var ticket = new SupportTicket
                {
                    CustomerID = _currentUserStore.CurrentUserId,
                    Subject = NewTicketSubject,
                    Description = NewTicketDescription,
                    Status = "Open",
                    Priority = NewTicketPriority,
                    CreatedDate = DateTime.Now
                };

                // In productie: _ticketService.CreateTicket(ticket);

                // Adaugam local
                _allTickets.Add(ticket);
                ApplyFilters();

                // Curatam formul
                NewTicketSubject = string.Empty;
                NewTicketDescription = string.Empty;
                NewTicketPriority = "Medium";

                NotifyStatistics();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to create ticket: " + ex.Message;
            }
        }

        // CanExecuteCreateTicket - Se poate crea?
        private bool CanExecuteCreateTicket(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewTicketSubject) &&
                   !string.IsNullOrWhiteSpace(NewTicketDescription);
        }

        // ExecuteRefresh - Reincarca
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
