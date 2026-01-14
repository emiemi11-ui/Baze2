using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ECommerceAppPerfect.Models
{
    // CLASA PRODUCT - Entitatea pentru produse
    //
    // PRODUSELE SUNT CENTRUL APLICATIEI E-COMMERCE
    // Tot se invarte in jurul lor:
    // - Clientii le cumpara
    // - Owner-ul le gestioneaza
    // - Inventory-ul le urmareste stocul
    // - Review-urile le evalueaza
    // - Tag-urile le categorizeaza
    //
    // RELATIILE PRODUCT:
    // - Category (Many-to-One): Fiecare produs apartine unei categorii
    // - StoreOwner (Many-to-One): Fiecare produs e adaugat de un owner
    // - Inventory (One-to-One): Fiecare produs are exact un inventory
    // - Reviews (One-to-Many): Un produs poate avea multe review-uri
    // - Tags (Many-to-Many): Un produs poate avea mai multe tag-uri
    // - OrderDetails (One-to-Many): Un produs poate aparea in mai multe comenzi
    //
    // RELATIA ONE-TO-ONE CU INVENTORY:
    // Aceasta e relatia ceruta explicit in cerinte
    // Fiecare produs are exact un rand in Inventory
    // Si fiecare Inventory apartine exact unui produs
    [Table("Products")]
    public partial class Product
    {
        // CONSTRUCTORUL - Initializare colectii
        //
        // Initializam toate colectiile pentru a evita NullReferenceException
        // HashSet e mai eficient decat List pentru colectii mari
        // si pentru operatii de cautare/adaugare
        public Product()
        {
            this.Reviews = new HashSet<Review>();
            this.Tags = new HashSet<Tag>();
            this.OrderDetails = new HashSet<OrderDetail>();
        }

        // PROPRIETATI - Coloanele din tabel

        // ProductID - Cheia primara
        //
        // IDENTITY = se genereaza automat la INSERT
        // Nu trebuie sa setezi ProductID cand creezi un produs nou
        // SQL Server il atribuie automat (1, 2, 3, ...)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }

        // ProductName - Numele produsului
        //
        // Acesta apare in:
        // - Lista de produse
        // - Cosul de cumparaturi
        // - Facturi
        // - Rezultate cautare
        //
        // Trebuie sa fie DESCRIPTIV si UNIC (pentru SEO si UX)
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        // Description - Descrierea detaliata
        //
        // NVARCHAR(MAX) in SQL = string fara limita in C#
        // Poate contine:
        // - Specificatii tehnice
        // - Beneficii
        // - Instructiuni de utilizare
        // - etc.
        //
        // NULL-able pentru ca nu e obligatorie
        public string Description { get; set; }

        // Price - Pretul produsului
        //
        // DECIMAL(18,2) in SQL = decimal in C#
        // 18 = total cifre
        // 2 = cifre dupa virgula
        //
        // DE CE DECIMAL SI NU DOUBLE?
        // decimal e EXACT - nu are erori de rotunjire
        // double e aproximativ - poate avea erori (0.1 + 0.2 != 0.3)
        // Pentru BANI, INTOTDEAUNA foloseste decimal!
        //
        // CHECK (Price >= 0) in SQL previne preturi negative
        [Required]
        public decimal Price { get; set; }

        // CategoryID - Foreign Key catre Categories
        //
        // [ForeignKey("Category")] leaga aceasta proprietate
        // de proprietatea de navigare Category
        //
        // EF foloseste aceasta valoare pentru a incarca Category
        // si pentru JOIN-uri in query-uri
        [Required]
        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        // StoreOwnerID - Foreign Key catre Users
        //
        // Indica CINE a adaugat acest produs
        // Trebuie sa fie un User cu UserRole = "StoreOwner"
        //
        // NOTA: Nu avem constrangere SQL pentru asta
        // Verificam in business logic (Services)
        [Required]
        [ForeignKey("StoreOwner")]
        public int StoreOwnerID { get; set; }

        // ImageURL - Calea catre imaginea produsului
        //
        // Poate fi:
        // - Cale relativa: "/images/product1.jpg"
        // - URL absolut: "https://cdn.example.com/images/product1.jpg"
        //
        // NULL-able pentru ca imaginea e optionala
        [StringLength(500)]
        public string ImageURL { get; set; }

        // CreatedDate - Data adaugarii produsului
        //
        // DEFAULT GETDATE() in SQL = momentul curent
        // Util pentru:
        // - Sortare "produse noi"
        // - Statistici
        public DateTime CreatedDate { get; set; }

        // IsActive - Flag pentru soft delete
        //
        // Nu stergem produse pentru ca:
        // 1. Pot fi in comenzi existente
        // 2. Pot avea review-uri
        // 3. Pot fi reactivate
        //
        // IsActive = false = produs "sters" (nu apare in catalog)
        public bool IsActive { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Category - Categoria produsului
        //
        // Relatia Many-to-One: Multe produse pot fi intr-o categorie
        // EF incarca automat Category cand folosesti .Include()
        // sau explicit cu Entry().Reference().Load()
        //
        // EXEMPLU EAGER LOADING:
        // context.Products.Include(p => p.Category).ToList()
        public virtual Category Category { get; set; }

        // StoreOwner - Utilizatorul care a adaugat produsul
        //
        // Relatia Many-to-One: Un owner poate avea multe produse
        public virtual User StoreOwner { get; set; }

        // Inventory - Stocul produsului
        //
        // RELATIA ONE-TO-ONE!
        // Fiecare produs are exact un Inventory
        // Constrangerea UNIQUE pe ProductID in tabelul Inventory garanteaza asta
        //
        // ACCESARE:
        // product.Inventory.StockQuantity - cate bucati avem
        // product.Inventory.MinimumStock - sub ce nivel avertizam
        public virtual Inventory Inventory { get; set; }

        // Reviews - Review-urile produsului
        //
        // Relatia One-to-Many: Un produs poate avea multe review-uri
        //
        // ACCESARE:
        // product.Reviews.Average(r => r.Rating) - media rating-urilor
        // product.Reviews.Count() - cate review-uri are
        public virtual ICollection<Review> Reviews { get; set; }

        // Tags - Etichetele produsului
        //
        // RELATIA MANY-TO-MANY!
        // Un produs poate avea multe tag-uri
        // Un tag poate fi pe multe produse
        //
        // In baza de date, relatia e prin tabelul ProductTags
        // EF gestioneaza automat tabelul intermediar
        //
        // ADAUGARE TAG:
        // product.Tags.Add(tag);
        // context.SaveChanges();
        // -> EF insereaza automat in ProductTags
        public virtual ICollection<Tag> Tags { get; set; }

        // OrderDetails - Liniile de comanda care contin acest produs
        //
        // Relatia One-to-Many: Un produs poate fi in multe OrderDetails
        // Util pentru statistici: cate bucati s-au vandut din acest produs
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        // PROPRIETATI CALCULATE - Business Logic

        // PriceFormatted - Pretul formatat pentru afisare
        //
        // Formatul: "1,299.99 RON"
        // N2 = Number with 2 decimal places
        //
        // DE CE NU FORMATAM IN UI?
        // Pentru consistenta - acelasi format peste tot
        // Si pentru separarea responsabilitatilor
        [NotMapped]
        public string PriceFormatted => $"{Price:N2} RON";

        // IsLowStock - Este stocul sub minim?
        //
        // Returneaza true daca trebuie reaprovizionare
        // Folosit pentru alertele "Low Stock" in dashboard
        //
        // LOGICA:
        // Daca StockQuantity < MinimumStock, e low stock
        // Exemplu: Stock=3, Minimum=5 -> IsLowStock=true
        [NotMapped]
        public bool IsLowStock
        {
            get
            {
                // Verificare null safety
                // Inventory poate fi null daca nu s-a incarcat
                if (Inventory == null)
                    return false;

                return Inventory.StockQuantity < Inventory.MinimumStock;
            }
        }

        // StockStatus - Statusul stocului ca text
        //
        // Pentru afisare in UI:
        // - "Out of Stock" - stoc 0
        // - "Low Stock" - sub minim
        // - "In Stock" - OK
        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (Inventory == null)
                    return "Unknown";

                if (Inventory.StockQuantity == 0)
                    return "Out of Stock";

                if (IsLowStock)
                    return "Low Stock";

                return "In Stock";
            }
        }

        // AverageRating - Media rating-urilor
        //
        // Calculeaza media review-urilor
        // Returneaza null daca nu sunt review-uri
        //
        // NOTA: Aceasta e o operatie costisitoare daca ai multe review-uri
        // Pentru performanta, ai putea stoca media in baza de date
        // si o actualizezi la fiecare review nou
        [NotMapped]
        public double? AverageRating
        {
            get
            {
                // Verificare null si empty
                if (Reviews == null || !Reviews.Any())
                    return null;

                // Average pe rating-uri
                return Reviews.Average(r => r.Rating);
            }
        }

        // ReviewCount - Numarul de review-uri
        //
        // Util pentru afisare: "4.5 stars (23 reviews)"
        [NotMapped]
        public int ReviewCount
        {
            get
            {
                if (Reviews == null)
                    return 0;

                return Reviews.Count;
            }
        }

        // TotalSold - Cate bucati s-au vandut
        //
        // Sumarizeaza Quantity din toate OrderDetails
        // Util pentru "Best Sellers" si statistici
        [NotMapped]
        public int TotalSold
        {
            get
            {
                if (OrderDetails == null || !OrderDetails.Any())
                    return 0;

                return OrderDetails.Sum(od => od.Quantity);
            }
        }
    }
}
