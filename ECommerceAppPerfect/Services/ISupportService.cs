using System;
using System.Collections.Generic;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // INTERFATA ISUPPORTSERVICE - Contract pentru serviciul de suport clienti
    //
    // CE ESTE SISTEMUL DE SUPORT?
    // Un sistem de ticketing pentru comunicarea intre clienti si agenti
    // Clientii deschid ticket-uri cand au probleme
    // Agentii CustomerService le rezolva
    //
    // ENTITATI IMPLICATE:
    // 1. SupportTicket - ticket-ul propriu-zis (problema raportata)
    // 2. TicketMessage - mesajele din conversatie
    // 3. User - atat clientul cat si agentul
    //
    // WORKFLOW-UL UNUI TICKET:
    // 1. CLIENTUL deschide un ticket (status: Open)
    //    - Seteaza Subject, Description, Priority
    //    - AssignedToID e NULL (nimeni nu l-a preluat inca)
    //
    // 2. AGENTUL preia ticket-ul (status: InProgress)
    //    - Se seteaza AssignedToID = ID-ul agentului
    //
    // 3. CONVERSATIE prin TicketMessages
    //    - Agentul si clientul comunica
    //    - Fiecare mesaj are IsFromCustomer (true/false)
    //
    // 4. REZOLVARE (status: Resolved)
    //    - Agentul marcheaza ticket-ul ca rezolvat
    //    - Se seteaza ResolvedDate
    //
    // 5. INCHIDERE (status: Closed)
    //    - Ticket-ul e inchis definitiv
    //
    // PRIORITATI:
    // - Low: intrebari generale, nu e urgent
    // - Medium: probleme obisnuite (default)
    // - High: probleme urgente (comenzi pierdute, rambursari)
    //
    // RELATII:
    // - SupportTicket -> Customer (Many-to-One): clientul care a deschis
    // - SupportTicket -> AssignedTo (Many-to-One): agentul asignat (nullable)
    // - SupportTicket -> TicketMessages (One-to-Many): mesajele
    // - TicketMessage -> User (Many-to-One): cine a trimis mesajul
    //
    // CONCEPTE DIN CURS DEMONSTRATE:
    // - One-to-Many (Ticket -> Messages)
    // - Many-to-One (Ticket -> Customer, Ticket -> AssignedTo)
    // - Eager Loading pentru relatii
    // - LINQ cu conditii multiple
    public interface ISupportService
    {
        // OPERATII PENTRU TICKET-URI

        // GetAllTickets - Returneaza toate ticket-urile
        //
        // INCLUDE (Eager Loading):
        // - Customer: cine a deschis ticket-ul
        // - AssignedTo: agentul asignat (poate fi null)
        // - TicketMessages: mesajele din conversatie
        //
        // FOLOSIRE: Dashboard agent pentru a vedea toate ticket-urile
        List<SupportTicket> GetAllTickets();

        // GetTicketById - Returneaza un ticket cu toate detaliile
        //
        // PARAMETRU: ticketId - ID-ul ticket-ului
        //
        // INCLUDE:
        // - Customer
        // - AssignedTo
        // - TicketMessages cu User (pentru a vedea cine a trimis)
        //
        // RETURNEAZA: Ticket complet sau null
        SupportTicket GetTicketById(int ticketId);

        // GetTicketsByCustomer - Ticket-urile unui client
        //
        // PARAMETRU: customerId - ID-ul clientului
        //
        // FOLOSIRE: "My Tickets" - istoricul ticket-urilor clientului
        //
        // RETURNEAZA: Lista ticket-urilor, cele mai recente primele
        List<SupportTicket> GetTicketsByCustomer(int customerId);

        // GetTicketsByAgent - Ticket-urile asignate unui agent
        //
        // PARAMETRU: agentId - ID-ul agentului CustomerService
        //
        // FOLOSIRE: "My Assigned Tickets" pentru agent
        //
        // RETURNEAZA: Ticket-urile asignate acelui agent
        List<SupportTicket> GetTicketsByAgent(int agentId);

        // GetTicketsByStatus - Filtrare dupa status
        //
        // PARAMETRU: status - "Open", "InProgress", "Resolved", "Closed"
        //
        // FOLOSIRE:
        // - "Open" - ticket-uri neasignate, de preluat
        // - "InProgress" - ticket-uri in lucru
        // - "Resolved" - ticket-uri rezolvate
        //
        // RETURNEAZA: Lista ticket-urilor cu statusul dat
        List<SupportTicket> GetTicketsByStatus(string status);

        // GetTicketsByPriority - Filtrare dupa prioritate
        //
        // PARAMETRU: priority - "Low", "Medium", "High"
        //
        // FOLOSIRE: Agentul vrea sa vada doar ticket-urile High priority
        List<SupportTicket> GetTicketsByPriority(string priority);

        // GetOpenTickets - Ticket-uri neasignate
        //
        // CONDITIE: Status == "Open"
        //
        // Acestea sunt ticket-urile care asteapta sa fie preluate
        List<SupportTicket> GetOpenTickets();

        // GetUnassignedTickets - Ticket-uri fara agent asignat
        //
        // CONDITIE: AssignedToID == null
        //
        // Poate include si ticket-uri "InProgress" fara agent (caz rar)
        List<SupportTicket> GetUnassignedTickets();

        // CreateTicket - Deschide un ticket nou
        //
        // PARAMETRI:
        // - customerId: clientul care deschide ticket-ul
        // - subject: subiectul (titlul)
        // - description: descrierea problemei
        // - priority: prioritatea (default: "Medium")
        //
        // OPERATII:
        // 1. Creeaza SupportTicket cu status "Open"
        // 2. AssignedToID = null (nimeni asignat inca)
        // 3. CreatedDate = acum
        //
        // RETURNEAZA: Ticket-ul creat sau null daca a esuat
        SupportTicket CreateTicket(int customerId, string subject,
                                   string description, string priority = "Medium");

        // AssignTicket - Asigneaza un ticket unui agent
        //
        // PARAMETRI:
        // - ticketId: ticket-ul de asignat
        // - agentId: agentul caruia i se asigneaza
        //
        // OPERATII:
        // 1. Verifica ca ticket-ul exista si e Open
        // 2. Verifica ca agentul exista si e CustomerService
        // 3. Seteaza AssignedToID = agentId
        // 4. Seteaza Status = "InProgress"
        //
        // RETURNEAZA: true daca asignarea a reusit
        bool AssignTicket(int ticketId, int agentId);

        // UpdateTicketStatus - Schimba statusul ticket-ului
        //
        // PARAMETRI:
        // - ticketId: ticket-ul
        // - newStatus: noul status ("Open", "InProgress", "Resolved", "Closed")
        //
        // REGULI:
        // - Daca newStatus = "Resolved", seteaza ResolvedDate
        // - Daca newStatus = "Open", reseteaza AssignedToID (optional)
        //
        // RETURNEAZA: true daca s-a actualizat
        bool UpdateTicketStatus(int ticketId, string newStatus);

        // ResolveTicket - Marcheaza ticket-ul ca rezolvat
        //
        // PARAMETRU: ticketId
        //
        // OPERATII:
        // 1. Seteaza Status = "Resolved"
        // 2. Seteaza ResolvedDate = DateTime.Now
        //
        // RETURNEAZA: true daca a reusit
        bool ResolveTicket(int ticketId);

        // CloseTicket - Inchide ticket-ul definitiv
        //
        // PARAMETRU: ticketId
        //
        // NOTA: De obicei se inchide dupa ce clientul confirma rezolvarea
        //
        // RETURNEAZA: true daca s-a inchis
        bool CloseTicket(int ticketId);

        // ReopenTicket - Redeschide un ticket rezolvat
        //
        // PARAMETRU: ticketId
        //
        // FOLOSIRE: Clientul revine cu aceeasi problema
        //
        // OPERATII:
        // 1. Verifica ca ticket-ul e Resolved sau Closed
        // 2. Seteaza Status = "Open" (sau "InProgress" daca are agent)
        // 3. Reseteaza ResolvedDate = null
        //
        // RETURNEAZA: true daca s-a redeschis
        bool ReopenTicket(int ticketId);

        // UpdateTicketPriority - Schimba prioritatea
        //
        // PARAMETRI:
        // - ticketId
        // - newPriority: "Low", "Medium", "High"
        //
        // FOLOSIRE: Escaladare - agentul creste prioritatea
        //
        // RETURNEAZA: true daca s-a actualizat
        bool UpdateTicketPriority(int ticketId, string newPriority);

        // OPERATII PENTRU MESAJE

        // GetTicketMessages - Mesajele unui ticket
        //
        // PARAMETRU: ticketId
        //
        // INCLUDE: User (pentru a afisa cine a trimis)
        //
        // RETURNEAZA: Lista mesajelor, ordonate cronologic
        List<TicketMessage> GetTicketMessages(int ticketId);

        // AddMessage - Adauga un mesaj la ticket
        //
        // PARAMETRI:
        // - ticketId: ticket-ul
        // - userId: cine trimite mesajul
        // - messageText: textul mesajului
        // - isFromCustomer: true daca e de la client, false daca e de la agent
        //
        // OPERATII:
        // 1. Creeaza TicketMessage
        // 2. Seteaza MessageDate = acum
        // 3. Daca ticket-ul era Resolved, il redeschide automat (optional)
        //
        // RETURNEAZA: Mesajul creat sau null
        TicketMessage AddMessage(int ticketId, int userId, string messageText, bool isFromCustomer);

        // STATISTICI PENTRU DASHBOARD

        // GetTicketCountByStatus - Numar ticket-uri per status
        //
        // RETURNEAZA: Dictionary { "Open": 5, "InProgress": 3, ... }
        //
        // FOLOSIRE: Grafic in dashboard
        Dictionary<string, int> GetTicketCountByStatus();

        // GetTicketCountByPriority - Numar ticket-uri per prioritate
        //
        // RETURNEAZA: Dictionary { "Low": 10, "Medium": 25, "High": 5 }
        Dictionary<string, int> GetTicketCountByPriority();

        // GetAverageResolutionTime - Timpul mediu de rezolvare
        //
        // CALCUL: AVG(ResolvedDate - CreatedDate) pentru ticket-urile rezolvate
        //
        // RETURNEAZA: TimeSpan cu timpul mediu, sau null daca nu sunt date
        TimeSpan? GetAverageResolutionTime();

        // GetAgentTicketCount - Cate ticket-uri are asignate un agent
        //
        // PARAMETRU: agentId
        //
        // FOLOSIRE: Load balancing - vezi cine are mai putine ticket-uri
        int GetAgentTicketCount(int agentId);

        // GetOpenTicketCount - Cate ticket-uri sunt deschise
        //
        // FOLOSIRE: Badge in dashboard
        int GetOpenTicketCount();

        // GetHighPriorityTicketCount - Cate ticket-uri High priority sunt deschise
        //
        // FOLOSIRE: Alerta urgenta in dashboard
        int GetHighPriorityTicketCount();
    }
}
