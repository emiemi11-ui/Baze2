using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ECommerceAppPerfect.Models
{
    // CLASA SUPPORTTICKET - Entitatea pentru ticket-uri de suport
    //
    // CE ESTE UN SUPPORT TICKET?
    // Un ticket de suport este o cerere de ajutor de la un client
    // Sistemul de ticketing permite:
    // - Clientilor sa raporteze probleme
    // - Agentilor sa gestioneze si rezolve problemele
    // - Tracking-ul conversatiilor
    //
    // WORKFLOW TICKET:
    // 1. CLIENT deschide ticket (status: Open)
    // 2. AGENT preia ticket-ul (status: InProgress, se asigneaza)
    // 3. AGENT si CLIENT comunica prin TicketMessages
    // 4. AGENT rezolva problema (status: Resolved)
    // 5. Ticket se inchide (status: Closed)
    //
    // PRIORITATI:
    // - Low: intrebari generale, nu e urgent
    // - Medium: probleme obisnuite (default)
    // - High: probleme urgente (comenzi pierdute, etc.)
    //
    // RELATII:
    // - Customer (Many-to-One): Clientul care a deschis ticket-ul
    // - AssignedTo (Many-to-One): Agentul asignat (poate fi NULL)
    // - TicketMessages (One-to-Many): Mesajele din conversatie
    [Table("SupportTickets")]
    public partial class SupportTicket
    {
        // CONSTRUCTORUL - Initializare colectie Messages
        public SupportTicket()
        {
            this.TicketMessages = new HashSet<TicketMessage>();
        }

        // PROPRIETATI - Coloanele din tabel

        // TicketID - Cheia primara
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TicketID { get; set; }

        // CustomerID - Foreign Key catre Users (clientul)
        //
        // Cine a deschis ticket-ul
        [Required]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        // Subject - Subiectul ticket-ului
        //
        // Scurta descriere a problemei
        // Apare in lista de ticket-uri
        // Exemplu: "Order Delivery Issue", "Product Return Request"
        [Required]
        [StringLength(200)]
        public string Subject { get; set; }

        // Description - Descrierea detaliata a problemei
        //
        // Primul mesaj al ticket-ului
        // Clientul explica problema in detaliu
        [Required]
        public string Description { get; set; }

        // Status - Starea curenta a ticket-ului
        //
        // VALORI POSIBILE:
        // - "Open" - deschis, asteapta preluare
        // - "InProgress" - in lucru de catre agent
        // - "Resolved" - rezolvat
        // - "Closed" - inchis definitiv
        //
        // DEFAULT "Open" - ticket-urile noi sunt deschise
        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        // Priority - Prioritatea ticket-ului
        //
        // VALORI POSIBILE:
        // - "Low" - prioritate scazuta
        // - "Medium" - prioritate normala (default)
        // - "High" - prioritate ridicata
        //
        // Ticket-urile High apar primele in lista agentilor
        [Required]
        [StringLength(20)]
        public string Priority { get; set; }

        // CreatedDate - Data deschiderii ticket-ului
        public DateTime CreatedDate { get; set; }

        // AssignedToID - Foreign Key catre Users (agentul)
        //
        // NULLABLE! - ticket-urile noi nu au agent asignat
        // Se seteaza cand un agent preia ticket-ul
        [ForeignKey("AssignedTo")]
        public int? AssignedToID { get; set; }

        // ResolvedDate - Data rezolvarii
        //
        // NULLABLE - se seteaza cand status devine Resolved
        // NULL pentru ticket-urile nerezolvate
        public DateTime? ResolvedDate { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Customer - Clientul care a deschis ticket-ul
        public virtual User Customer { get; set; }

        // AssignedTo - Agentul asignat (poate fi null)
        public virtual User AssignedTo { get; set; }

        // TicketMessages - Mesajele din ticket
        //
        // RELATIA ONE-TO-MANY
        // Un ticket poate avea multe mesaje (conversatie)
        public virtual ICollection<TicketMessage> TicketMessages { get; set; }

        // PROPRIETATI CALCULATE

        // TicketNumber - Numarul ticket-ului formatat
        //
        // Format: "TKT-00001"
        [NotMapped]
        public string TicketNumber => $"TKT-{TicketID:D5}";

        // IsOpen - Este ticket-ul inca deschis?
        //
        // True pentru Open sau InProgress
        [NotMapped]
        public bool IsOpen => Status == "Open" || Status == "InProgress";

        // IsAssigned - Este asignat unui agent?
        [NotMapped]
        public bool IsAssigned => AssignedToID.HasValue;

        // MessageCount - Cate mesaje are ticket-ul
        [NotMapped]
        public int MessageCount
        {
            get
            {
                if (TicketMessages == null)
                    return 0;

                return TicketMessages.Count;
            }
        }

        // LastMessageDate - Data ultimului mesaj
        //
        // Util pentru sortare (cele cu mesaje recente primele)
        [NotMapped]
        public DateTime? LastMessageDate
        {
            get
            {
                if (TicketMessages == null || !TicketMessages.Any())
                    return null;

                return TicketMessages.Max(m => m.MessageDate);
            }
        }

        // DaysSinceCreated - Cate zile de la deschidere
        //
        // Util pentru SLA tracking
        [NotMapped]
        public int DaysSinceCreated => (DateTime.Now - CreatedDate).Days;

        // ResolutionTime - Timpul de rezolvare (daca e rezolvat)
        //
        // Diferenta dintre CreatedDate si ResolvedDate
        [NotMapped]
        public TimeSpan? ResolutionTime
        {
            get
            {
                if (!ResolvedDate.HasValue)
                    return null;

                return ResolvedDate.Value - CreatedDate;
            }
        }

        // StatusColor - Culoarea pentru afisare status
        [NotMapped]
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "Open" => "#2196F3",      // Blue
                    "InProgress" => "#FF9800", // Orange
                    "Resolved" => "#4CAF50",   // Green
                    "Closed" => "#9E9E9E",     // Grey
                    _ => "#757575"
                };
            }
        }

        // PriorityColor - Culoarea pentru afisare prioritate
        [NotMapped]
        public string PriorityColor
        {
            get
            {
                return Priority switch
                {
                    "Low" => "#4CAF50",     // Green
                    "Medium" => "#FF9800",  // Orange
                    "High" => "#F44336",    // Red
                    _ => "#757575"
                };
            }
        }

        // CustomerName - Numele clientului
        [NotMapped]
        public string CustomerName
        {
            get
            {
                if (Customer == null)
                    return "Unknown";

                return Customer.FullName;
            }
        }

        // AssignedToName - Numele agentului asignat
        [NotMapped]
        public string AssignedToName
        {
            get
            {
                if (AssignedTo == null)
                    return "Unassigned";

                return AssignedTo.FullName;
            }
        }
    }
}
