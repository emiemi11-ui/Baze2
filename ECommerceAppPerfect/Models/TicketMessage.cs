using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA TICKETMESSAGE - Entitatea pentru mesajele din ticket-uri
    //
    // CE ESTE UN TICKET MESSAGE?
    // Un mesaj din conversatia unui ticket de suport
    // Permite comunicarea bidirectionala intre client si agent
    //
    // FLOW CONVERSATIE:
    // 1. Client deschide ticket cu Description (primul "mesaj")
    // 2. Agent raspunde cu un TicketMessage
    // 3. Client raspunde inapoi
    // 4. ... conversatia continua pana la rezolvare
    //
    // RELATII:
    // - SupportTicket (Many-to-One): Mesajul apartine unui ticket
    // - User (Many-to-One): Cine a trimis mesajul (client sau agent)
    //
    // IsFromCustomer:
    // - True: mesajul e de la client
    // - False: mesajul e de la agent
    // Acest flag permite stilizare diferita in UI (chat bubbles)
    [Table("TicketMessages")]
    public partial class TicketMessage
    {
        // PROPRIETATI - Coloanele din tabel

        // MessageID - Cheia primara
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageID { get; set; }

        // TicketID - Foreign Key catre SupportTickets
        //
        // La ce ticket apartine mesajul
        // ON DELETE CASCADE: daca stergem ticket-ul, se sterg si mesajele
        [Required]
        [ForeignKey("SupportTicket")]
        public int TicketID { get; set; }

        // UserID - Foreign Key catre Users
        //
        // Cine a trimis mesajul (client sau agent)
        [Required]
        [ForeignKey("User")]
        public int UserID { get; set; }

        // MessageText - Continutul mesajului
        //
        // Textul efectiv al mesajului
        // NVARCHAR(MAX) pentru mesaje lungi
        [Required]
        public string MessageText { get; set; }

        // MessageDate - Data si ora trimiterii
        //
        // Util pentru:
        // - Sortare mesaje cronologic
        // - Afisare timestamp in UI
        public DateTime MessageDate { get; set; }

        // IsFromCustomer - Este mesajul de la client?
        //
        // True: mesaj de la client (partea stanga in UI)
        // False: mesaj de la agent (partea dreapta in UI)
        //
        // DE CE ACEST FLAG?
        // Pentru ca UserID poate fi atat client cat si agent
        // Flag-ul simplifica query-urile si stilizarea UI
        public bool IsFromCustomer { get; set; }

        // PROPRIETATI DE NAVIGARE

        // SupportTicket - Ticket-ul parinte
        public virtual SupportTicket SupportTicket { get; set; }

        // User - Utilizatorul care a trimis mesajul
        public virtual User User { get; set; }

        // PROPRIETATI CALCULATE

        // SenderName - Numele celui care a trimis
        //
        // Afiseaza numele complet sau username-ul
        [NotMapped]
        public string SenderName
        {
            get
            {
                if (User == null)
                    return IsFromCustomer ? "Customer" : "Agent";

                return User.FullName;
            }
        }

        // SenderRole - Rolul celui care a trimis
        //
        // "Customer" sau "Support Agent"
        [NotMapped]
        public string SenderRole
        {
            get
            {
                return IsFromCustomer ? "Customer" : "Support Agent";
            }
        }

        // TimeAgo - Cat timp a trecut de la mesaj
        //
        // Format: "5 minutes ago", "2 hours ago", "Yesterday"
        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - MessageDate;

                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} min ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalDays < 2)
                    return "Yesterday";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";

                return MessageDate.ToString("MMM dd, yyyy");
            }
        }

        // FormattedTime - Ora formatata (pentru afisare in chat)
        //
        // Format: "14:30" (ora si minute)
        [NotMapped]
        public string FormattedTime => MessageDate.ToString("HH:mm");

        // FormattedDate - Data formatata
        //
        // Format: "Jan 15, 2024"
        [NotMapped]
        public string FormattedDate => MessageDate.ToString("MMM dd, yyyy");

        // ShortText - Mesajul prescurtat (pentru preview)
        //
        // Primele 50 de caractere
        [NotMapped]
        public string ShortText
        {
            get
            {
                if (string.IsNullOrEmpty(MessageText))
                    return "";

                if (MessageText.Length <= 50)
                    return MessageText;

                return MessageText.Substring(0, 50) + "...";
            }
        }

        // BubbleAlignment - Alinierea bulei de chat
        //
        // Pentru UI: mesajele client-ului la stanga, ale agentului la dreapta
        [NotMapped]
        public string BubbleAlignment => IsFromCustomer ? "Left" : "Right";

        // BubbleColor - Culoarea bulei de chat
        //
        // Culori diferite pentru client si agent
        [NotMapped]
        public string BubbleColor => IsFromCustomer ? "#E3F2FD" : "#E8F5E9";
    }
}
