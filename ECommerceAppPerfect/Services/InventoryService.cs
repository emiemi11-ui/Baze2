using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // CLASA INVENTORYSERVICE - Implementarea serviciului de inventar/stoc
    //
    // ACEASTA CLASA DEMONSTREAZA RELATIA ONE-TO-ONE!
    //
    // CE ESTE ONE-TO-ONE?
    // O relatie unde fiecare entitate din A corespunde EXACT unei entitati din B
    // In cazul nostru: Product <-> Inventory
    // Un produs are exact un inventory
    // Un inventory apartine exact unui produs
    //
    // CUM SE REALIZEAZA IN SQL?
    // Prin constrangerea UNIQUE pe Foreign Key
    // ProductID INT NOT NULL UNIQUE
    // UNIQUE garanteaza ca nu pot exista 2 inventory-uri cu acelasi ProductID
    //
    // CUM SE NAVIGHEAZA IN ENTITY FRAMEWORK?
    // Product -> Inventory: product.Inventory (proprietate singulara, nu colectie)
    // Inventory -> Product: inventory.Product (proprietate singulara)
    //
    // DIFERENTA FATA DE ONE-TO-MANY:
    // One-to-Many: Category -> Products (categorie are ICollection<Product>)
    // One-to-One: Product -> Inventory (produs are Inventory singur)
    //
    // CONCEPTE DIN CURS DEMONSTRATE:
    // 1. Relatia One-to-One (pag. 8 din curs)
    // 2. Navigare proprietati de navigare singulare
    // 3. LINQ to Entities cu conditii pe relatii
    // 4. SaveChanges() pentru persistenta
    // 5. Eager Loading cu Include()
    public class InventoryService : IInventoryService, IDisposable
    {
        private bool _disposed = false;

        // HELPER - Creeaza un nou DbContext
        private ECommerceEntities GetContext()
        {
            return new ECommerceEntities();
        }

        // CRUD - READ (CITIRE)

        // GetAllInventories - Toate inventarele cu produsele lor
        //
        // DEMONSTREAZA: Eager Loading pe relatia One-to-One
        //
        // Include(i => i.Product) incarca produsul asociat
        // Aceasta e relatia ONE-TO-ONE - fiecare inventory are UN produs
        //
        // QUERY SQL GENERAT:
        // SELECT i.*, p.*, c.*
        // FROM Inventory i
        // LEFT JOIN Products p ON i.ProductID = p.ProductID
        // LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
        // WHERE p.IsActive = 1
        // ORDER BY p.ProductName
        public List<Inventory> GetAllInventories()
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Include(i => i.Product)              // ONE-TO-ONE: incarca produsul
                    .Include(i => i.Product.Category)     // Incarca si categoria produsului
                    .Where(i => i.Product.IsActive)       // Doar produse active
                    .OrderBy(i => i.Product.ProductName)
                    .ToList();
            }
        }

        // GetInventoryById - Returneaza un inventory dupa ID-ul propriu
        //
        // FOLOSIM Find() pentru cautare dupa PK
        // Apoi Explicit Loading pentru relatii
        //
        // DE CE EXPLICIT LOADING AICI?
        // Find() nu suporta Include()
        // Deci incarcam relatia manual cu Entry().Reference().Load()
        //
        // Reference() e pentru relatii singulare (One-to-One sau Many-to-One)
        // Collection() ar fi pentru relatii de colectie (One-to-Many)
        public Inventory GetInventoryById(int inventoryId)
        {
            using (var context = GetContext())
            {
                var inventory = context.Inventories.Find(inventoryId);

                if (inventory != null)
                {
                    // EXPLICIT LOADING pentru relatia ONE-TO-ONE
                    // Reference() pentru ca Product e singur, nu colectie
                    context.Entry(inventory)
                        .Reference(i => i.Product)
                        .Load();

                    // Incarcam si categoria daca avem produsul
                    if (inventory.Product != null)
                    {
                        context.Entry(inventory.Product)
                            .Reference(p => p.Category)
                            .Load();
                    }
                }

                return inventory;
            }
        }

        // GetInventoryByProductId - DEMONSTREAZA RELATIA ONE-TO-ONE
        //
        // ACEASTA METODA E ESENTIALA PENTRU INTELEGEREA ONE-TO-ONE!
        //
        // Cand cautam inventory dupa ProductID, primim EXACT UN rezultat
        // (sau null daca nu exista)
        // Nu primim niciodata o lista, pentru ca ProductID e UNIQUE
        //
        // QUERY SQL:
        // SELECT * FROM Inventory WHERE ProductID = @productId
        //
        // Datorita UNIQUE, SQL Server stie ca va returna maxim 1 rand
        // Si poate optimiza query-ul pentru asta
        public Inventory GetInventoryByProductId(int productId)
        {
            using (var context = GetContext())
            {
                // FirstOrDefault e potrivit pentru ONE-TO-ONE
                // Stim ca nu pot exista 2 inventory-uri pentru acelasi produs
                return context.Inventories
                    .Include(i => i.Product)
                    .Include(i => i.Product.Category)
                    .FirstOrDefault(i => i.ProductID == productId);
            }
        }

        // GetLowStockInventories - Produse cu stoc sub minim
        //
        // DEMONSTREAZA: Comparatie intre coloane in LINQ
        // i.StockQuantity < i.MinimumStock
        //
        // ORDER BY: Cele mai urgente primele
        // (MinimumStock - StockQuantity) DESC = cele care au nevoie de mai mult stoc
        public List<Inventory> GetLowStockInventories()
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Include(i => i.Product)
                    .Include(i => i.Product.Category)
                    .Where(i => i.StockQuantity < i.MinimumStock && i.Product.IsActive)
                    .OrderByDescending(i => i.MinimumStock - i.StockQuantity)
                    .ToList();
            }
        }

        // GetOutOfStockInventories - Produse epuizate
        //
        // DEMONSTREAZA: Filtrare simpla StockQuantity == 0
        public List<Inventory> GetOutOfStockInventories()
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Include(i => i.Product)
                    .Include(i => i.Product.Category)
                    .Where(i => i.StockQuantity == 0 && i.Product.IsActive)
                    .OrderBy(i => i.Product.ProductName)
                    .ToList();
            }
        }

        // GetInventoriesByCategory - Stoc pentru o categorie
        //
        // DEMONSTREAZA: Navigare prin relatii multiple
        // Inventory -> Product -> Category
        // Filtram dupa Product.CategoryID
        public List<Inventory> GetInventoriesByCategory(int categoryId)
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Include(i => i.Product)
                    .Include(i => i.Product.Category)
                    .Where(i => i.Product.CategoryID == categoryId && i.Product.IsActive)
                    .OrderBy(i => i.Product.ProductName)
                    .ToList();
            }
        }

        // CRUD - CREATE (CREARE)

        // CreateInventory - Creeaza inventory pentru un produs
        //
        // DEMONSTREAZA: Garantarea UNICITATII in One-to-One
        //
        // Inainte de a crea, verificam daca deja exista inventory
        // pentru acest produs. Daca da, nu cream altul!
        // Aceasta e esenta relatiei One-to-One
        //
        // NOTA: Constrangerea UNIQUE din SQL ar preveni oricum duplicatele
        // Dar e mai elegant sa verificam in cod si sa returnam false
        // decat sa lasam SQL-ul sa arunce exceptie
        public bool CreateInventory(int productId, int stockQuantity = 0, int minimumStock = 5)
        {
            if (stockQuantity < 0 || minimumStock < 0)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // VERIFICARE: Exista deja inventory pentru acest produs?
                    // In One-to-One, nu pot exista 2 inventory-uri pentru acelasi produs
                    bool exists = context.Inventories.Any(i => i.ProductID == productId);
                    if (exists)
                    {
                        // ONE-TO-ONE: deja exista, nu cream altul
                        return false;
                    }

                    // VERIFICARE: Produsul exista?
                    var productExists = context.Products.Any(p => p.ProductID == productId);
                    if (!productExists)
                        return false;

                    // CREARE INVENTORY
                    var inventory = new Inventory
                    {
                        ProductID = productId,       // ONE-TO-ONE: legatura cu produsul
                        StockQuantity = stockQuantity,
                        MinimumStock = minimumStock,
                        LastUpdated = DateTime.Now
                    };

                    context.Inventories.Add(inventory);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                // Exceptia poate fi din cauza UNIQUE constraint violation
                // daca doi useri incearca sa creeze simultan
                return false;
            }
        }

        // CRUD - UPDATE (ACTUALIZARE)

        // UpdateStock - Seteaza direct cantitatea
        //
        // DEMONSTREAZA: Update simplu pe entitate
        //
        // FLOW:
        // 1. Find() gaseste inventory-ul
        // 2. Modificam proprietatea StockQuantity
        // 3. SaveChanges() detecteaza modificarea si genereaza UPDATE
        //
        // EF genereaza: UPDATE Inventory SET StockQuantity = @qty, LastUpdated = @date
        //               WHERE InventoryID = @id
        public bool UpdateStock(int productId, int newQuantity)
        {
            if (newQuantity < 0)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // Gasim inventory-ul pentru acest produs
                    // FirstOrDefault pentru ONE-TO-ONE
                    var inventory = context.Inventories
                        .FirstOrDefault(i => i.ProductID == productId);

                    if (inventory == null)
                        return false;

                    // Update stoc
                    inventory.StockQuantity = newQuantity;
                    inventory.LastUpdated = DateTime.Now;

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // IncreaseStock - Adauga la stoc (reaprovizionare)
        //
        // DEMONSTREAZA: Operatie de incrementare
        //
        // In loc sa setam valoarea directa, o incrementam
        // StockQuantity = StockQuantity + quantity
        public bool IncreaseStock(int productId, int quantity)
        {
            if (quantity <= 0)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var inventory = context.Inventories
                        .FirstOrDefault(i => i.ProductID == productId);

                    if (inventory == null)
                        return false;

                    // Folosim metoda din model
                    inventory.IncreaseStock(quantity);

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // DecreaseStock - Scade din stoc (vanzare)
        //
        // DEMONSTREAZA: Validare business logic
        //
        // Nu putem scadea sub 0!
        // Verificam ca avem stoc suficient inainte de operatie
        public bool DecreaseStock(int productId, int quantity)
        {
            if (quantity <= 0)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var inventory = context.Inventories
                        .FirstOrDefault(i => i.ProductID == productId);

                    if (inventory == null)
                        return false;

                    // ReduceStock din model verifica daca avem stoc suficient
                    // Returneaza false daca nu
                    if (!inventory.ReduceStock(quantity))
                        return false;

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // UpdateMinimumStock - Actualizeaza pragul minim
        public bool UpdateMinimumStock(int productId, int newMinimum)
        {
            if (newMinimum < 0)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var inventory = context.Inventories
                        .FirstOrDefault(i => i.ProductID == productId);

                    if (inventory == null)
                        return false;

                    inventory.MinimumStock = newMinimum;
                    inventory.LastUpdated = DateTime.Now;

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // INTEROGARI SPECIFICE

        // CanFulfillOrder - Verificare rapida de stoc
        //
        // QUERY optimizat - nu incarcam tot obiectul
        // Doar verificam conditia direct in DB
        public bool CanFulfillOrder(int productId, int quantity)
        {
            if (quantity <= 0)
                return false;

            using (var context = GetContext())
            {
                // Any() cu conditie - returneaza true/false fara a incarca entitatea
                // SELECT CASE WHEN EXISTS(SELECT 1 FROM Inventory
                //        WHERE ProductID = @id AND StockQuantity >= @qty) THEN 1 ELSE 0 END
                return context.Inventories
                    .Any(i => i.ProductID == productId && i.StockQuantity >= quantity);
            }
        }

        // GetStockQuantity - Returneaza doar cantitatea
        //
        // DEMONSTREAZA: Proiectie cu Select()
        // Nu incarcam tot obiectul, doar valoarea care ne intereseaza
        public int GetStockQuantity(int productId)
        {
            using (var context = GetContext())
            {
                // Select() proiecteaza doar ce ne trebuie
                // FirstOrDefault() cu nullable
                var quantity = context.Inventories
                    .Where(i => i.ProductID == productId)
                    .Select(i => (int?)i.StockQuantity)
                    .FirstOrDefault();

                return quantity ?? 0;
            }
        }

        // IsLowStock - Verifica rapid daca e low stock
        public bool IsLowStock(int productId)
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Any(i => i.ProductID == productId &&
                             i.StockQuantity < i.MinimumStock);
            }
        }

        // IsOutOfStock - Verifica rapid daca e out of stock
        public bool IsOutOfStock(int productId)
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Any(i => i.ProductID == productId && i.StockQuantity == 0);
            }
        }

        // STATISTICI

        // GetLowStockCount - Cate produse sunt low stock
        //
        // DEMONSTREAZA: Count() cu conditie complexa
        //
        // QUERY:
        // SELECT COUNT(*) FROM Inventory i
        // INNER JOIN Products p ON i.ProductID = p.ProductID
        // WHERE i.StockQuantity < i.MinimumStock AND p.IsActive = 1
        public int GetLowStockCount()
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Count(i => i.StockQuantity < i.MinimumStock &&
                               i.Product.IsActive);
            }
        }

        // GetOutOfStockCount - Cate produse sunt epuizate
        public int GetOutOfStockCount()
        {
            using (var context = GetContext())
            {
                return context.Inventories
                    .Count(i => i.StockQuantity == 0 && i.Product.IsActive);
            }
        }

        // GetTotalStockValue - Valoarea totala a inventarului
        //
        // DEMONSTREAZA: Calcul agregat cu Sum() pe expresie
        //
        // CALCUL: SUM(StockQuantity * Price)
        //
        // NAVIGARE RELATIE ONE-TO-ONE:
        // i.Product.Price acceseaza pretul produsului prin relatia One-to-One
        //
        // QUERY SQL:
        // SELECT SUM(i.StockQuantity * p.Price) FROM Inventory i
        // INNER JOIN Products p ON i.ProductID = p.ProductID
        // WHERE p.IsActive = 1
        public decimal GetTotalStockValue()
        {
            using (var context = GetContext())
            {
                var value = context.Inventories
                    .Where(i => i.Product.IsActive)
                    .Sum(i => (decimal?)(i.StockQuantity * i.Product.Price));

                return value ?? 0;
            }
        }

        // OPERATII BULK (IN MASA)

        // BulkUpdateStock - Update pentru mai multe produse
        //
        // DEMONSTREAZA: Procesare in batch
        //
        // PARAMETRU: Dictionary<ProductID, NewQuantity>
        //
        // DE CE E UTIL?
        // - Import din Excel
        // - Sincronizare cu sistem extern de warehouse
        // - Actualizare stoc dupa inventar fizic
        //
        // TOATE updateurile se fac intr-o singura tranzactie
        // (un singur SaveChanges la final)
        public int BulkUpdateStock(Dictionary<int, int> updates)
        {
            if (updates == null || !updates.Any())
                return 0;

            try
            {
                using (var context = GetContext())
                {
                    int updatedCount = 0;

                    // Extragem ProductID-urile pentru un singur query
                    var productIds = updates.Keys.ToList();

                    // Incarcam toate inventarele necesare intr-un singur query
                    var inventories = context.Inventories
                        .Where(i => productIds.Contains(i.ProductID))
                        .ToList();

                    foreach (var inventory in inventories)
                    {
                        if (updates.TryGetValue(inventory.ProductID, out int newQuantity))
                        {
                            if (newQuantity >= 0) // Validare
                            {
                                inventory.StockQuantity = newQuantity;
                                inventory.LastUpdated = DateTime.Now;
                                updatedCount++;
                            }
                        }
                    }

                    // UN SINGUR SAVECHANGES PENTRU TOATE UPDATEURILE
                    // Aceasta e o optimizare importanta!
                    // In loc de N roundtrip-uri la DB, facem unul singur
                    context.SaveChanges();

                    return updatedCount;
                }
            }
            catch (Exception)
            {
                return 0;
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
                    // Elibereaza resurse managed (nu avem in acest caz)
                }

                _disposed = true;
            }
        }
    }
}
