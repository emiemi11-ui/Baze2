using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // CLASA SUPPORTSERVICE - Implementarea serviciului de suport clienti
    //
    // ACEASTA CLASA DEMONSTREAZA:
    //
    // 1. RELATII ONE-TO-MANY:
    // SupportTicket -> TicketMessages (un ticket are multe mesaje)
    // In cod: ticket.TicketMessages.Add(message)
    // EF gestioneaza automat Foreign Key-ul
    //
    // 2. RELATII MANY-TO-ONE:
    // SupportTicket -> Customer (multe ticket-uri, un client)
    // SupportTicket -> AssignedTo (multe ticket-uri, un agent)
    // TicketMessage -> User (multe mesaje, un user)
    //
    // 3. NAVIGARE PROPRIETATI NULLABLE:
    // AssignedToID poate fi null (ticket neasignat)
    // Verificam cu HasValue sau != null inainte de acces
    //
    // 4. EAGER LOADING CU INCLUDE MULTIPLU:
    // .Include(t => t.Customer)
    // .Include(t => t.AssignedTo)
    // .Include(t => t.TicketMessages.Select(m => m.User))
    //
    // 5. EXPLICIT LOADING PENTRU COLECTII:
    // Entry(ticket).Collection(t => t.TicketMessages).Load()
    //
    // PATTERN: Context-per-Operation cu using()
    public class SupportService : ISupportService, IDisposable
    {
        private bool _disposed = false;

        // HELPER - Creeaza un nou DbContext
        private ECommerceEntities GetContext()
        {
            return new ECommerceEntities();
        }

        // OPERATII PENTRU TICKET-URI - READ

        // GetAllTickets - Toate ticket-urile cu EAGER LOADING
        //
        // DEMONSTREAZA: Include() pentru multiple relatii
        //
        // QUERY SQL GENERAT (aproximativ):
        // SELECT t.*, c.*, a.*, m.*
        // FROM SupportTickets t
        // LEFT JOIN Users c ON t.CustomerID = c.UserID
        // LEFT JOIN Users a ON t.AssignedToID = a.UserID
        // LEFT JOIN TicketMessages m ON t.TicketID = m.TicketID
        // ORDER BY t.CreatedDate DESC
        //
        // NOTA: Include() pe AssignedTo e LEFT JOIN pentru ca poate fi NULL
        public List<SupportTicket> GetAllTickets()
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Include(t => t.Customer)                     // Many-to-One: clientul
                    .Include(t => t.AssignedTo)                   // Many-to-One: agentul (nullable)
                    .Include(t => t.TicketMessages)               // One-to-Many: mesajele
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
            }
        }

        // GetTicketById - Un ticket cu toate detaliile
        //
        // DEMONSTREAZA: Include() pentru navigare mai adanca
        // TicketMessages.Select(m => m.User) - incarca si User-ul fiecarui mesaj
        public SupportTicket GetTicketById(int ticketId)
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketMessages.Select(m => m.User))  // Navigare adanca
                    .FirstOrDefault(t => t.TicketID == ticketId);
            }
        }

        // GetTicketsByCustomer - Ticket-urile unui client
        //
        // DEMONSTREAZA: Filtrare LINQ simpla
        // WHERE CustomerID = @customerId
        public List<SupportTicket> GetTicketsByCustomer(int customerId)
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketMessages)
                    .Where(t => t.CustomerID == customerId)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
            }
        }

        // GetTicketsByAgent - Ticket-urile asignate unui agent
        //
        // DEMONSTREAZA: Comparatie cu FK nullable
        // AssignedToID.HasValue && AssignedToID.Value == agentId
        // Sau mai simplu: AssignedToID == agentId (EF gestioneaza null-ul)
        public List<SupportTicket> GetTicketsByAgent(int agentId)
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketMessages)
                    .Where(t => t.AssignedToID == agentId)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
            }
        }

        // GetTicketsByStatus - Filtrare dupa status
        public List<SupportTicket> GetTicketsByStatus(string status)
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketMessages)
                    .Where(t => t.Status == status)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
            }
        }

        // GetTicketsByPriority - Filtrare dupa prioritate
        public List<SupportTicket> GetTicketsByPriority(string priority)
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.TicketMessages)
                    .Where(t => t.Priority == priority)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
            }
        }

        // GetOpenTickets - Ticket-urile care asteapta preluare
        public List<SupportTicket> GetOpenTickets()
        {
            return GetTicketsByStatus("Open");
        }

        // GetUnassignedTickets - Ticket-uri fara agent asignat
        //
        // DEMONSTREAZA: Comparatie cu NULL in LINQ
        // AssignedToID == null se traduce in: WHERE AssignedToID IS NULL
        public List<SupportTicket> GetUnassignedTickets()
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.TicketMessages)
                    .Where(t => t.AssignedToID == null)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
            }
        }

        // OPERATII PENTRU TICKET-URI - CREATE

        // CreateTicket - Creeaza un ticket nou
        //
        // DEMONSTREAZA: Adaugarea unei entitati cu relatie Many-to-One
        // CustomerID refera un User existent
        //
        // FLOW:
        // 1. Validare input
        // 2. Verificare client existent
        // 3. Creare SupportTicket
        // 4. SaveChanges() face INSERT
        public SupportTicket CreateTicket(int customerId, string subject,
                                          string description, string priority = "Medium")
        {
            // VALIDARE INPUT
            if (string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(description))
                return null;

            // VALIDARE PRIORITATE
            var validPriorities = new[] { "Low", "Medium", "High" };
            if (!validPriorities.Contains(priority))
                priority = "Medium";  // Default daca e invalida

            try
            {
                using (var context = GetContext())
                {
                    // VERIFICARE CLIENT
                    // Clientul trebuie sa existe si sa fie activ
                    var customer = context.Users.Find(customerId);
                    if (customer == null || !customer.IsActive)
                        return null;

                    // CREARE TICKET
                    var ticket = new SupportTicket
                    {
                        CustomerID = customerId,        // Many-to-One: legatura cu clientul
                        Subject = subject,
                        Description = description,
                        Status = "Open",                // Ticket-urile noi sunt Open
                        Priority = priority,
                        CreatedDate = DateTime.Now,
                        AssignedToID = null,            // Neasignat initial
                        ResolvedDate = null             // Nerezolvat initial
                    };

                    context.SupportTickets.Add(ticket);
                    context.SaveChanges();

                    // Incarcam Customer-ul pentru a-l returna complet
                    context.Entry(ticket)
                        .Reference(t => t.Customer)
                        .Load();

                    return ticket;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        // OPERATII PENTRU TICKET-URI - UPDATE

        // AssignTicket - Asigneaza un ticket unui agent
        //
        // DEMONSTREAZA: Update pe FK nullable
        // AssignedToID trece de la NULL la un ID valid
        public bool AssignTicket(int ticketId, int agentId)
        {
            try
            {
                using (var context = GetContext())
                {
                    // GASIRE TICKET
                    var ticket = context.SupportTickets.Find(ticketId);
                    if (ticket == null)
                        return false;

                    // VERIFICARE: Ticket-ul poate fi asignat?
                    // Doar ticket-urile Open sau InProgress pot fi reasignate
                    if (ticket.Status == "Closed")
                        return false;

                    // VERIFICARE AGENT
                    // Agentul trebuie sa existe, sa fie activ, si sa fie CustomerService
                    var agent = context.Users.Find(agentId);
                    if (agent == null || !agent.IsActive || agent.UserRole != "CustomerService")
                        return false;

                    // ASIGNARE
                    ticket.AssignedToID = agentId;

                    // Daca era Open, trecem la InProgress
                    if (ticket.Status == "Open")
                        ticket.Status = "InProgress";

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // UpdateTicketStatus - Schimba statusul ticket-ului
        //
        // DEMONSTREAZA: Update conditional
        // Daca noul status e "Resolved", setam si ResolvedDate
        public bool UpdateTicketStatus(int ticketId, string newStatus)
        {
            // VALIDARE STATUS
            var validStatuses = new[] { "Open", "InProgress", "Resolved", "Closed" };
            if (!validStatuses.Contains(newStatus))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var ticket = context.SupportTickets.Find(ticketId);
                    if (ticket == null)
                        return false;

                    // UPDATE STATUS
                    ticket.Status = newStatus;

                    // SETARE RESOLVEDDATE PENTRU RESOLVED
                    if (newStatus == "Resolved" && !ticket.ResolvedDate.HasValue)
                    {
                        ticket.ResolvedDate = DateTime.Now;
                    }

                    // RESETARE DACA SE REDESCHIDE
                    if (newStatus == "Open" || newStatus == "InProgress")
                    {
                        ticket.ResolvedDate = null;
                    }

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ResolveTicket - Shortcut pentru rezolvare
        public bool ResolveTicket(int ticketId)
        {
            return UpdateTicketStatus(ticketId, "Resolved");
        }

        // CloseTicket - Shortcut pentru inchidere
        public bool CloseTicket(int ticketId)
        {
            return UpdateTicketStatus(ticketId, "Closed");
        }

        // ReopenTicket - Redeschide un ticket
        //
        // DEMONSTREAZA: Logica de business in service
        // Nu orice ticket poate fi redeschis
        public bool ReopenTicket(int ticketId)
        {
            try
            {
                using (var context = GetContext())
                {
                    var ticket = context.SupportTickets.Find(ticketId);
                    if (ticket == null)
                        return false;

                    // VALIDARE: Doar Resolved sau Closed pot fi redeschise
                    if (ticket.Status != "Resolved" && ticket.Status != "Closed")
                        return false;

                    // REDESCHIDERE
                    // Daca are agent asignat, trece la InProgress
                    // Daca nu, trece la Open
                    ticket.Status = ticket.AssignedToID.HasValue ? "InProgress" : "Open";
                    ticket.ResolvedDate = null;

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // UpdateTicketPriority - Schimba prioritatea
        public bool UpdateTicketPriority(int ticketId, string newPriority)
        {
            var validPriorities = new[] { "Low", "Medium", "High" };
            if (!validPriorities.Contains(newPriority))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var ticket = context.SupportTickets.Find(ticketId);
                    if (ticket == null)
                        return false;

                    ticket.Priority = newPriority;
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // OPERATII PENTRU MESAJE

        // GetTicketMessages - Mesajele unui ticket
        //
        // DEMONSTREAZA: Query pe relatia One-to-Many
        // Filtram dupa TicketID si incarcam User-ul fiecarui mesaj
        public List<TicketMessage> GetTicketMessages(int ticketId)
        {
            using (var context = GetContext())
            {
                return context.TicketMessages
                    .Include(m => m.User)  // Cine a trimis mesajul
                    .Where(m => m.TicketID == ticketId)
                    .OrderBy(m => m.MessageDate)  // Cronologic
                    .ToList();
            }
        }

        // AddMessage - Adauga un mesaj la ticket
        //
        // DEMONSTREAZA: Adaugarea in colectia One-to-Many
        //
        // METODA 1 (folosita aici):
        // context.TicketMessages.Add(message) - adaugam direct in DbSet
        // Setam manual TicketID
        //
        // METODA 2 (alternativa):
        // ticket.TicketMessages.Add(message) - adaugam prin colectia de navigare
        // EF seteaza automat TicketID
        public TicketMessage AddMessage(int ticketId, int userId, string messageText, bool isFromCustomer)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return null;

            try
            {
                using (var context = GetContext())
                {
                    // VERIFICARE TICKET
                    var ticket = context.SupportTickets.Find(ticketId);
                    if (ticket == null)
                        return null;

                    // VERIFICARE USER
                    var user = context.Users.Find(userId);
                    if (user == null || !user.IsActive)
                        return null;

                    // CREARE MESAJ
                    var message = new TicketMessage
                    {
                        TicketID = ticketId,          // FK catre ticket
                        UserID = userId,              // FK catre user
                        MessageText = messageText,
                        MessageDate = DateTime.Now,
                        IsFromCustomer = isFromCustomer
                    };

                    context.TicketMessages.Add(message);

                    // OPTIONAL: Daca ticket-ul era Resolved si clientul trimite mesaj,
                    // il redeschide automat
                    if (isFromCustomer && ticket.Status == "Resolved")
                    {
                        ticket.Status = ticket.AssignedToID.HasValue ? "InProgress" : "Open";
                        ticket.ResolvedDate = null;
                    }

                    context.SaveChanges();

                    // Incarcam User-ul pentru a returna mesajul complet
                    context.Entry(message)
                        .Reference(m => m.User)
                        .Load();

                    return message;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        // STATISTICI PENTRU DASHBOARD

        // GetTicketCountByStatus - Distributia ticket-urilor pe status
        //
        // DEMONSTREAZA: GroupBy() cu ToDictionary()
        //
        // QUERY SQL GENERAT:
        // SELECT Status, COUNT(*) as Count
        // FROM SupportTickets
        // GROUP BY Status
        public Dictionary<string, int> GetTicketCountByStatus()
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        // GetTicketCountByPriority - Distributia pe prioritate
        public Dictionary<string, int> GetTicketCountByPriority()
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .GroupBy(t => t.Priority)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        // GetAverageResolutionTime - Timpul mediu de rezolvare
        //
        // DEMONSTREAZA: Calcul pe date
        //
        // NOTA: EF nu suporta direct operatii pe TimeSpan in LINQ
        // De aceea incarcam datele si calculam in memorie
        // Pentru volume mari de date, ar trebui un stored procedure
        public TimeSpan? GetAverageResolutionTime()
        {
            using (var context = GetContext())
            {
                // Incarcam ticket-urile rezolvate
                var resolvedTickets = context.SupportTickets
                    .Where(t => t.ResolvedDate.HasValue)
                    .Select(t => new { t.CreatedDate, t.ResolvedDate })
                    .ToList();

                if (!resolvedTickets.Any())
                    return null;

                // Calculam media in memorie
                var totalTicks = resolvedTickets
                    .Average(t => (t.ResolvedDate.Value - t.CreatedDate).Ticks);

                return TimeSpan.FromTicks((long)totalTicks);
            }
        }

        // GetAgentTicketCount - Cate ticket-uri are un agent
        public int GetAgentTicketCount(int agentId)
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Count(t => t.AssignedToID == agentId &&
                               (t.Status == "Open" || t.Status == "InProgress"));
            }
        }

        // GetOpenTicketCount - Cate ticket-uri Open exista
        public int GetOpenTicketCount()
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Count(t => t.Status == "Open");
            }
        }

        // GetHighPriorityTicketCount - Cate ticket-uri High priority sunt active
        public int GetHighPriorityTicketCount()
        {
            using (var context = GetContext())
            {
                return context.SupportTickets
                    .Count(t => t.Priority == "High" &&
                               (t.Status == "Open" || t.Status == "InProgress"));
            }
        }

        // DISPOSE PATTERN

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Elibereaza resurse managed
                }

                _disposed = true;
            }
        }
    }
}
