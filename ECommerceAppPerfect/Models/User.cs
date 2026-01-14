using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA USER - Entitatea pentru utilizatori
    //
    // CE ESTE O ENTITATE?
    // O entitate este o clasa C# care reprezinta un RAND dintr-un tabel
    // Proprietatile clasei corespund COLOANELOR din tabel
    // Entity Framework mapeaza automat intre clase si tabele
    //
    // IN DB FIRST:
    // Aceasta clasa este generata AUTOMAT de EF din structura tabelului Users
    // EF analizeaza schema bazei de date si creeaza clasa corespunzatoare
    //
    // PARTIAL CLASS - DE CE?
    // "partial" permite EXTINDEREA clasei in alt fisier
    // EF genereaza clasa de baza, tu adaugi business logic in alt fisier
    // Cand EF regenereaza clasa (dupa modificari in DB), nu piezi codul tau
    //
    // EXEMPLU:
    // User.cs (generat de EF) - proprietati de baza
    // User.Extensions.cs (scris de tine) - metode custom, validari
    //
    // ATRIBUTUL [Table("Users")]
    // Specifica EXPLICIT numele tabelului din baza de date
    // Util cand numele clasei difera de numele tabelului
    // Sau cand vrei sa fii explicit (claritate)
    [Table("Users")]
    public partial class User
    {
        // CONSTRUCTORUL - Initializare colectii
        //
        // DE CE INITIALIZAM COLECTIILE?
        // Pentru a evita NullReferenceException cand accesezi relatiile
        // Fara initializare, user.Products ar fi null pana cand EF incarca datele
        // Cu initializare, user.Products e o lista goala (nu null)
        //
        // CAND SE APELEAZA?
        // La crearea unui nou User: var user = new User();
        // Si cand EF materializeaza un User din baza de date
        public User()
        {
            // Initializare colectii pentru relatiile One-to-Many
            // Un User poate avea mai multe Products (daca e StoreOwner)
            this.Products = new HashSet<Product>();

            // Un User poate avea mai multe Orders (daca e Customer)
            this.Orders = new HashSet<Order>();

            // Un User poate avea mai multe Reviews (daca e Customer)
            this.Reviews = new HashSet<Review>();

            // Un User poate avea mai multe SupportTickets deschise
            this.SupportTicketsCreated = new HashSet<SupportTicket>();

            // Un User poate avea mai multe SupportTickets asignate (daca e Agent)
            this.SupportTicketsAssigned = new HashSet<SupportTicket>();

            // Un User poate avea mai multe TicketMessages
            this.TicketMessages = new HashSet<TicketMessage>();
        }

        // PROPRIETATI - Corespund coloanelor din tabel

        // UserID - Cheia primara
        //
        // [Key] - Marcheaza aceasta proprietate ca Primary Key
        // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        // - Valoarea se genereaza automat de SQL Server (IDENTITY)
        // - Nu trebuie sa setezi manual UserID la Insert
        //
        // DE CE INT SI NU GUID?
        // INT e mai simplu si mai performant pentru majoritatea cazurilor
        // GUID (Guid) e util pentru sisteme distribuite
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        // Username - Numele de utilizator pentru login
        //
        // [Required] - Nu poate fi NULL (validare EF)
        // [StringLength(50)] - Maxim 50 caractere (validare EF)
        // Aceste atribute sunt pentru validare, nu afecteaza baza de date
        // (schema DB e deja definita in SQL)
        //
        // NVARCHAR(50) in SQL = string in C#
        // NVARCHAR suporta Unicode (caractere internationale)
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        // Email - Adresa de email (unica)
        //
        // Folosit pentru:
        // 1. Comunicare (trimitere email-uri)
        // 2. Login alternativ (email + parola)
        // 3. Recuperare parola
        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        // HashedPassword - Parola criptata
        //
        // IMPORTANT: Nu stocam NICIODATA parole in clar!
        // Folosim SHA-256 hash pentru securitate
        //
        // LA LOGIN:
        // 1. Utilizatorul introduce parola
        // 2. O criptam cu SHA-256
        // 3. Comparam hash-ul cu cel din baza de date
        //
        // DACA BAZA DE DATE E COMPROMISA:
        // Atacatorul vede doar hash-uri, nu parole reale
        // Hash-urile nu se pot inversa (one-way function)
        [Required]
        [StringLength(255)]
        public string HashedPassword { get; set; }

        // UserRole - Tipul utilizatorului
        //
        // VALORI POSIBILE:
        // "StoreOwner" - Proprietarul magazinului (admin)
        // "Customer" - Client obisnuit
        // "CustomerService" - Agent de suport
        //
        // Constrangerea CHECK din SQL garanteaza ca doar aceste valori sunt permise
        // In C#, am putea folosi enum, dar string e mai flexibil
        [Required]
        [StringLength(20)]
        public string UserRole { get; set; }

        // FirstName, LastName - Numele real
        //
        // NULL-able (?) pentru ca nu sunt obligatorii
        // Utilizatorul le poate completa mai tarziu in profil
        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        // PhoneNumber - Numar de telefon
        //
        // Folosit pentru:
        // 1. Contact in caz de probleme cu comanda
        // 2. SMS notifications (optional)
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        // Address - Adresa pentru livrare
        //
        // TEXT lung pentru adresa completa:
        // "Str. Exemplu, Nr. 10, Bl. A, Sc. 1, Ap. 5, Bucuresti, 012345"
        [StringLength(500)]
        public string Address { get; set; }

        // CreatedDate - Data crearii contului
        //
        // DEFAULT in SQL: GETDATE() = momentul curent
        // Util pentru:
        // 1. Statistici (cati utilizatori noi pe luna)
        // 2. Sortare (utilizatori recenti)
        public DateTime CreatedDate { get; set; }

        // IsActive - Flag pentru soft delete
        //
        // DE CE SOFT DELETE?
        // Nu stergem utilizatori din baza de date pentru ca:
        // 1. Pastram istoricul comenzilor
        // 2. Putem reactiva conturi
        // 3. Audit trail
        //
        // In loc de DELETE, setam IsActive = false
        // La query-uri, filtram: WHERE IsActive = 1
        public bool IsActive { get; set; }

        // PROPRIETATI DE NAVIGARE - Relatii cu alte tabele
        //
        // CE SUNT PROPRIETATILE DE NAVIGARE?
        // Permit accesarea entitatilor RELATIONATE direct din cod
        // Exemplu: user.Products returneaza toate produsele user-ului
        //
        // EF foloseste Foreign Keys pentru a incarca datele
        // In functie de strategia de incarcare:
        // - Lazy Loading: se incarca automat la acces (dezactivat la noi)
        // - Eager Loading: se incarca cu .Include() in query
        // - Explicit Loading: se incarca manual cu .Load()

        // Products - Produsele adaugate de acest user (daca e StoreOwner)
        //
        // virtual permite Lazy Loading (desi l-am dezactivat)
        // ICollection permite orice tip de colectie (List, HashSet, etc.)
        //
        // Relatia: User (1) -> Products (Many)
        // Inverse: Product.StoreOwner
        public virtual ICollection<Product> Products { get; set; }

        // Orders - Comenzile plasate de acest user (daca e Customer)
        //
        // Relatia: User (1) -> Orders (Many)
        // Inverse: Order.Customer
        public virtual ICollection<Order> Orders { get; set; }

        // Reviews - Review-urile scrise de acest user
        //
        // Relatia: User (1) -> Reviews (Many)
        // Inverse: Review.Customer
        public virtual ICollection<Review> Reviews { get; set; }

        // SupportTicketsCreated - Ticket-urile deschise de acest user
        //
        // Relatia: User (1) -> SupportTickets (Many) pe CustomerID
        // Inverse: SupportTicket.Customer
        public virtual ICollection<SupportTicket> SupportTicketsCreated { get; set; }

        // SupportTicketsAssigned - Ticket-urile asignate acestui user (daca e Agent)
        //
        // Relatia: User (1) -> SupportTickets (Many) pe AssignedToID
        // Inverse: SupportTicket.AssignedTo
        public virtual ICollection<SupportTicket> SupportTicketsAssigned { get; set; }

        // TicketMessages - Mesajele trimise de acest user
        //
        // Relatia: User (1) -> TicketMessages (Many)
        // Inverse: TicketMessage.User
        public virtual ICollection<TicketMessage> TicketMessages { get; set; }

        // PROPRIETATI CALCULATE - Business Logic
        //
        // Acestea NU sunt in baza de date
        // Sunt calculate la runtime din alte proprietati
        // [NotMapped] spune EF sa NU incerce sa le mapeze la coloane

        // FullName - Numele complet al utilizatorului
        //
        // Combina FirstName si LastName pentru afisare
        // Exemplu: "John Doe"
        //
        // DE CE PROPRIETATE CALCULATA?
        // Nu stocam date redundante in baza de date
        // Se calculeaza ori de cate ori e nevoie
        [NotMapped]
        public string FullName
        {
            get
            {
                // Combinam FirstName si LastName
                // .Trim() elimina spatiile de la inceput/sfarsit
                // Daca ambele sunt null/goale, returnam Username
                var name = $"{FirstName} {LastName}".Trim();
                return string.IsNullOrEmpty(name) ? Username : name;
            }
        }

        // RoleDisplayName - Numele rolului pentru afisare in UI
        //
        // Transforma valorile din baza de date in text prietenos
        // Cu emoji-uri pentru vizibilitate
        [NotMapped]
        public string RoleDisplayName
        {
            get
            {
                // Switch expression (C# 8.0+)
                // Mai concis decat switch statement traditional
                return UserRole switch
                {
                    "StoreOwner" => "Store Owner",
                    "Customer" => "Customer",
                    "CustomerService" => "Support Agent",
                    _ => UserRole  // Default: returneaza valoarea originala
                };
            }
        }

        // METODE DE VALIDARE

        // IsValidEmail - Verifica daca email-ul are format valid
        //
        // Returneaza true daca email-ul e valid, false altfel
        // Folosim System.Net.Mail.MailAddress pentru validare
        // Aceasta clasa arunca exceptie pentru email-uri invalide
        public bool IsValidEmail()
        {
            // Verificare null/empty
            if (string.IsNullOrWhiteSpace(Email))
                return false;

            try
            {
                // MailAddress parseaza email-ul
                // Daca formatul e invalid, arunca FormatException
                var addr = new System.Net.Mail.MailAddress(Email);

                // Verificam ca adresa parsata e identica cu input-ul
                // Previne cazuri edge case unde parsarea "repara" email-ul
                return addr.Address == Email;
            }
            catch
            {
                // Orice exceptie inseamna email invalid
                return false;
            }
        }
    }
}
