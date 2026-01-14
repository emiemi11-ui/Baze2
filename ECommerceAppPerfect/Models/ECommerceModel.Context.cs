using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using System.Linq;

namespace ECommerceAppPerfect.Models
{
    // CLASA ECommerceEntities - Database Context (DbContext)
    //
    // CE ESTE DBCONTEXT?
    // DbContext este clasa principala din Entity Framework pentru acces la baza de date
    // Reprezinta o SESIUNE cu baza de date
    // Prin ea faci toate operatiile: citire, scriere, stergere
    //
    // IN DB FIRST:
    // Aceasta clasa este generata AUTOMAT de Entity Framework din EDMX
    // Cand folosesti wizard-ul "ADO.NET Entity Data Model" in Visual Studio,
    // EF analizeaza baza de date si genereaza aceasta clasa
    //
    // IN ACEST PROIECT:
    // Am scris manual clasa pentru a demonstra cum arata
    // In practica, o lasi pe EF sa o genereze din EDMX
    //
    // CE FACE ACEASTA CLASA?
    // 1. Se conecteaza la baza de date (prin connection string)
    // 2. Expune DbSet-uri pentru fiecare tabel (Users, Products, etc.)
    // 3. Permite query-uri LINQ pe aceste DbSet-uri
    // 4. Urmareste modificarile (Change Tracking)
    // 5. Salveaza modificarile in DB cu SaveChanges()
    //
    // EXEMPLU DE FOLOSIRE:
    // using (var context = new ECommerceEntities())
    // {
    //     var products = context.Products.ToList();
    //     var user = context.Users.Find(1);
    //     context.SaveChanges();
    // }
    //
    // DE CE "using"?
    // DbContext implementeaza IDisposable
    // "using" garanteaza ca conexiunea se inchide dupa utilizare
    // Altfel ai avea memory leaks si conexiuni deschise
    public partial class ECommerceEntities : DbContext
    {
        // CONSTRUCTORUL - Initializare DbContext
        //
        // base("name=ECommerceEntities") apeleaza constructorul DbContext
        // cu numele connection string-ului din App.config
        //
        // "name=ECommerceEntities" spune EF sa caute in App.config
        // un connection string cu numele "ECommerceEntities"
        //
        // ALTERNATIVA: Poti pasa direct connection string-ul
        // base("data source=...;initial catalog=...") - dar nu e recomandat
        // E mai bine sa tii connection string-ul in App.config
        // pentru ca poti schimba fara a recompila
        public ECommerceEntities()
            : base("name=ECommerceEntities")
        {
            // DEZACTIVAM LAZY LOADING
            //
            // CE ESTE LAZY LOADING?
            // Lazy Loading incarca automat entitatile relationate cand le accesezi
            // Exemplu: product.Category se incarca automat la primul acces
            //
            // DE CE IL DEZACTIVAM?
            // 1. Pentru control explicit asupra query-urilor
            // 2. Pentru a evita problema N+1 queries
            //    (daca ai 100 produse, ar face 101 query-uri)
            // 3. Conform cerintelor cursului - folosim Eager/Explicit Loading
            //
            // CUM INCARCAM RELATIILE FARA LAZY LOADING?
            // 1. Eager Loading: .Include(p => p.Category)
            // 2. Explicit Loading: context.Entry(product).Reference(p => p.Category).Load()
            this.Configuration.LazyLoadingEnabled = false;

            // DEZACTIVAM PROXY-URILE
            //
            // CE SUNT PROXY-URILE?
            // EF creeaza clase derivate la runtime (proxy-uri) pentru entitati
            // Aceste proxy-uri permit Lazy Loading si Change Tracking automat
            //
            // DE CE LE DEZACTIVAM?
            // 1. Am dezactivat Lazy Loading deja
            // 2. Simplifica debugging-ul (vezi clasele reale, nu proxy-uri)
            // 3. Evita probleme cu serializarea (proxy-urile nu se serializeaza bine)
            this.Configuration.ProxyCreationEnabled = false;
        }

        // DBSET-URI PENTRU FIECARE TABEL
        //
        // CE ESTE UN DBSET?
        // DbSet<TEntity> reprezinta o COLECTIE de entitati din baza de date
        // Fiecare DbSet corespunde unui TABEL din baza de date
        //
        // CUM FUNCTIONEAZA?
        // - DbSet implementeaza IQueryable - poti face query-uri LINQ pe el
        // - Query-urile LINQ sunt traduse in SQL de EF
        // - Modificarile sunt urmarite (Change Tracking)
        //
        // EXEMPLU:
        // context.Products.Where(p => p.Price > 100).ToList()
        // Se traduce in: SELECT * FROM Products WHERE Price > 100
        //
        // NOTA: virtual permite override in clasele derivate (daca ai nevoie)

        // USERS - Tabelul cu utilizatorii
        // Contine: StoreOwner, Customer, CustomerService
        public virtual DbSet<User> Users { get; set; }

        // PRODUCTS - Tabelul cu produsele
        // Relatii: Category (Many-to-One), Inventory (One-to-One), Tags (Many-to-Many)
        public virtual DbSet<Product> Products { get; set; }

        // CATEGORIES - Tabelul cu categoriile
        // Relatie: Products (One-to-Many)
        public virtual DbSet<Category> Categories { get; set; }

        // ORDERS - Tabelul cu comenzile
        // Relatii: Customer (Many-to-One), OrderDetails (One-to-Many)
        public virtual DbSet<Order> Orders { get; set; }

        // ORDERDETAILS - Liniile comenzilor
        // Relatii: Order (Many-to-One), Product (Many-to-One)
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }

        // INVENTORIES - Stocul produselor
        // Relatie: Product (One-to-One)
        public virtual DbSet<Inventory> Inventories { get; set; }

        // REVIEWS - Review-urile produselor
        // Relatii: Product (Many-to-One), Customer (Many-to-One)
        public virtual DbSet<Review> Reviews { get; set; }

        // TAGS - Etichetele produselor
        // Relatie: Products (Many-to-Many prin ProductTags)
        public virtual DbSet<Tag> Tags { get; set; }

        // SUPPORTTICKETS - Ticket-urile de suport
        // Relatii: Customer (Many-to-One), AssignedTo (Many-to-One), Messages (One-to-Many)
        public virtual DbSet<SupportTicket> SupportTickets { get; set; }

        // TICKETMESSAGES - Mesajele din ticket-uri
        // Relatii: Ticket (Many-to-One), User (Many-to-One)
        public virtual DbSet<TicketMessage> TicketMessages { get; set; }

        // STORESETTINGS - Setarile magazinului
        // Tabel key-value, fara relatii
        public virtual DbSet<StoreSetting> StoreSettings { get; set; }

        // METODA OnModelCreating - Configurare Model
        //
        // CE FACE ACEASTA METODA?
        // Permite configurarea suplimentara a modelului EF
        // Se apeleaza o singura data cand EF construieste modelul
        //
        // IN DB FIRST:
        // Aceasta metoda ar trebui sa fie GOALA sau sa nu existe!
        // De ce? Pentru ca tot modelul vine din EDMX
        // EDMX-ul contine deja toate configurarile (mapari, relatii, etc.)
        //
        // NOTA IMPORTANTA:
        // In DB First REAL, aceasta metoda NU contine throw UnintentionalCodeFirstException
        // Acea exceptie e specifica pentru a preveni utilizarea gresita
        // In implementarea noastra, o lasam goala pentru compatibilitate
        //
        // CAND AI FOLOSI ACEASTA METODA?
        // In Code First sau Model First, pentru:
        // - Configurare Fluent API (modelBuilder.Entity<>().HasKey())
        // - Mapari custom (Table, Column names)
        // - Configurare relatii complexe
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // IN DB FIRST, NU CONFIGUREZI NIMIC AICI!
            // Tot vine din EDMX
            //
            // Daca ai configura ceva aici, ar intra in conflict cu EDMX-ul
            // si ai avea erori sau comportament neasteptat
            //
            // LASAM METODA GOALA conform cerintelor DB First

            // Configuram doar tabelul intermediar ProductTags pentru relatia Many-to-Many
            // Aceasta e o exceptie - EF nu stie automat ca ProductTags e tabel de legatura
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Tags)
                .WithMany(t => t.Products)
                .Map(m =>
                {
                    m.ToTable("ProductTags");
                    m.MapLeftKey("ProductID");
                    m.MapRightKey("TagID");
                });
        }

        // METODE PENTRU STORED PROCEDURES
        //
        // IN DB FIRST CU EDMX:
        // Stored Procedures se importa automat ca "Function Imports"
        // EF genereaza metode pentru fiecare SP importata
        //
        // IN IMPLEMENTAREA NOASTRA:
        // Le apelam direct cu Database.SqlQuery() in Services
        // Vezi ReportService.cs pentru exemple
        //
        // EXEMPLU DE FUNCTION IMPORT GENERAT DE EDMX:
        // public virtual ObjectResult<LowStockProductDTO> GetLowStockProducts()
        // {
        //     return ((IObjectContextAdapter)this).ObjectContext
        //         .ExecuteFunction<LowStockProductDTO>("GetLowStockProducts");
        // }
    }
}
