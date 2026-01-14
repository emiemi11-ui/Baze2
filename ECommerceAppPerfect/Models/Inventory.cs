using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA INVENTORY - Entitatea pentru stocul produselor
    //
    // CE ESTE INVENTORY?
    // Inventory gestioneaza STOCUL fiecarui produs
    // Tine evidenta:
    // - Cate bucati avem in stoc (StockQuantity)
    // - Sub ce nivel trebuie sa recomandeze reaprovizionare (MinimumStock)
    // - Cand s-a actualizat ultima data (LastUpdated)
    //
    // RELATIA ONE-TO-ONE CU PRODUCT
    // Aceasta este relatia ceruta explicit in cerintele proiectului!
    //
    // CUM FUNCTIONEAZA ONE-TO-ONE?
    // - Fiecare Product are EXACT un Inventory
    // - Fiecare Inventory apartine EXACT unui Product
    // - Constrangerea UNIQUE pe ProductID garanteaza unicitatea
    //
    // DE CE ONE-TO-ONE SI NU COLOANE IN PRODUCT?
    // 1. Separarea responsabilitatilor (Single Responsibility Principle)
    //    - Product: informatii despre produs (nume, pret, descriere)
    //    - Inventory: informatii despre stoc (cantitate, minim)
    //
    // 2. Diferite frecvente de update
    //    - Product se modifica rar (numele, pretul)
    //    - Inventory se modifica frecvent (la fiecare vanzare)
    //
    // 3. Demonstrarea relatiei One-to-One pentru proiect
    //
    // 4. Posibilitatea de a avea entitati fara inventory (produse virtuale)
    //
    // CUM SE REALIZEAZA ONE-TO-ONE IN SQL?
    // CREATE TABLE Inventory (
    //     ...
    //     ProductID INT NOT NULL UNIQUE,  -- UNIQUE face relatia One-to-One!
    //     FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
    // )
    //
    // Fara UNIQUE, ar fi One-to-Many (un produs ar putea avea multe inventories)
    // Cu UNIQUE, un produs poate avea MAXIM un inventory
    [Table("Inventory")]
    public partial class Inventory
    {
        // PROPRIETATI - Coloanele din tabel

        // InventoryID - Cheia primara PROPRIE
        //
        // In One-to-One, ai doua optiuni pentru PK:
        // 1. PK proprie (InventoryID) - cum am facut noi
        // 2. PK = FK (ProductID ca PK) - dependent entity
        //
        // Am ales optiunea 1 pentru simplitate si flexibilitate
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InventoryID { get; set; }

        // ProductID - Foreign Key UNICA catre Products
        //
        // ACEASTA E CHEIA RELATIEI ONE-TO-ONE!
        //
        // Constrangerea UNIQUE in SQL garanteaza ca:
        // - Fiecare ProductID apare MAXIM o data in Inventory
        // - Deci un produs poate avea maxim un inventory
        //
        // [Index(IsUnique = true)] - echivalent cu UNIQUE in EF
        // (desi constrangerea e deja in SQL)
        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        // StockQuantity - Cate bucati avem in stoc
        //
        // CHECK (StockQuantity >= 0) in SQL - nu poate fi negativ
        // 0 = out of stock
        //
        // SE MODIFICA:
        // - Scade la fiecare vanzare
        // - Creste la fiecare reaprovizionare
        public int StockQuantity { get; set; }

        // MinimumStock - Stocul minim recomandat
        //
        // Cand StockQuantity < MinimumStock, se afiseaza alerta "Low Stock"
        // DEFAULT 5 in SQL - valoare rezonabila pentru majoritatea produselor
        //
        // POATE FI DIFERIT pentru fiecare produs:
        // - Produse populare: MinimumStock = 20
        // - Produse rare: MinimumStock = 2
        public int MinimumStock { get; set; }

        // LastUpdated - Cand s-a modificat ultima data stocul
        //
        // Se actualizeaza la:
        // - Fiecare vanzare
        // - Fiecare reaprovizionare
        // - Ajustari manuale de stoc
        //
        // Util pentru:
        // - Audit trail
        // - Detectarea problemelor (stoc neactualizat de mult timp)
        public DateTime LastUpdated { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Product - Produsul asociat
        //
        // RELATIA ONE-TO-ONE (partea inversa)
        // Aceasta e proprietatea de navigare catre produs
        //
        // ACCESARE:
        // inventory.Product.ProductName - numele produsului
        // inventory.Product.Price - pretul produsului
        public virtual Product Product { get; set; }

        // PROPRIETATI CALCULATE

        // IsLowStock - Este stocul sub minim?
        //
        // True daca StockQuantity < MinimumStock
        // Folosit pentru alertele din dashboard
        [NotMapped]
        public bool IsLowStock => StockQuantity < MinimumStock;

        // IsOutOfStock - Este complet epuizat?
        //
        // True daca StockQuantity == 0
        // Produsele out of stock nu pot fi comandate
        [NotMapped]
        public bool IsOutOfStock => StockQuantity == 0;

        // StockNeeded - Cate bucati trebuie comandate
        //
        // Diferenta dintre MinimumStock si StockQuantity
        // Daca stocul e OK, returneaza 0
        //
        // EXEMPLU:
        // StockQuantity = 3, MinimumStock = 10
        // StockNeeded = 10 - 3 = 7 bucati de comandat
        [NotMapped]
        public int StockNeeded
        {
            get
            {
                if (StockQuantity >= MinimumStock)
                    return 0;

                return MinimumStock - StockQuantity;
            }
        }

        // StockStatus - Statusul stocului ca text
        //
        // Pentru afisare in UI:
        // - "Out of Stock" - stoc 0
        // - "Low Stock (5)" - sub minim, cu cantitatea actuala
        // - "In Stock (50)" - OK, cu cantitatea actuala
        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (IsOutOfStock)
                    return "Out of Stock";

                if (IsLowStock)
                    return $"Low Stock ({StockQuantity})";

                return $"In Stock ({StockQuantity})";
            }
        }

        // StockStatusColor - Culoarea pentru afisare status
        //
        // Rosu pentru out of stock, portocaliu pentru low stock, verde pentru OK
        [NotMapped]
        public string StockStatusColor
        {
            get
            {
                if (IsOutOfStock)
                    return "#F44336"; // Red

                if (IsLowStock)
                    return "#FF9800"; // Orange

                return "#4CAF50"; // Green
            }
        }

        // StockPercentage - Procentul de stoc (fata de minim)
        //
        // Util pentru progress bar in UI
        // 100% = stocul e egal cu minimul
        // >100% = stocul e peste minim
        // <100% = stocul e sub minim
        //
        // NOTA: Limitam la 100% pentru progress bar
        [NotMapped]
        public double StockPercentage
        {
            get
            {
                if (MinimumStock == 0)
                    return StockQuantity > 0 ? 100 : 0;

                double percentage = (double)StockQuantity / MinimumStock * 100;

                // Limitam la 100% pentru UI
                return Math.Min(percentage, 100);
            }
        }

        // METODE DE BUSINESS LOGIC

        // CanFulfill - Putem onora o comanda de o anumita cantitate?
        //
        // Verifica daca avem suficient stoc
        // Folosit la adaugare in cos si checkout
        //
        // PARAMETRU: quantity - cantitatea dorita
        // RETURNEAZA: true daca avem stoc, false altfel
        public bool CanFulfill(int quantity)
        {
            return StockQuantity >= quantity;
        }

        // ReduceStock - Scade stocul dupa o vanzare
        //
        // PARAMETRU: quantity - cantitatea vanduta
        // RETURNEAZA: true daca a reusit, false daca nu e stoc suficient
        //
        // NOTA: Aceasta metoda NU salveaza in DB!
        // Trebuie sa apelezi context.SaveChanges() dupa
        public bool ReduceStock(int quantity)
        {
            if (!CanFulfill(quantity))
                return false;

            StockQuantity -= quantity;
            LastUpdated = DateTime.Now;
            return true;
        }

        // IncreaseStock - Creste stocul dupa o reaprovizionare
        //
        // PARAMETRU: quantity - cantitatea adaugata
        //
        // NOTA: Nu verifica limita maxima (nu avem una definita)
        public void IncreaseStock(int quantity)
        {
            if (quantity <= 0)
                return;

            StockQuantity += quantity;
            LastUpdated = DateTime.Now;
        }
    }
}
