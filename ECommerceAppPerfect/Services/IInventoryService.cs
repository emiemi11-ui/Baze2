using System.Collections.Generic;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // INTERFATA IINVENTORYSERVICE - Contract pentru serviciul de inventar/stoc
    //
    // CE ESTE INVENTORY-UL?
    // Inventory gestioneaza STOCUL fiecarui produs
    // Este componenta critica pentru un magazin online
    // Raspunde la intrebari precum:
    // - Cate bucati avem din produsul X?
    // - Ce produse trebuie reaprovizionate?
    // - Putem onora aceasta comanda?
    //
    // RELATIA ONE-TO-ONE CU PRODUCT (CERINTA DIN CURS!):
    // Aceasta este relatia One-to-One ceruta explicit in proiect
    // Fiecare Product are EXACT un Inventory
    // Fiecare Inventory apartine EXACT unui Product
    // Constrangerea UNIQUE pe ProductID garanteaza unicitatea
    //
    // CUM SE REALIZEAZA ONE-TO-ONE IN SQL?
    // CREATE TABLE Inventory (
    //     ProductID INT NOT NULL UNIQUE,  -- UNIQUE face relatia One-to-One!
    //     FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
    // )
    //
    // FARA UNIQUE ar fi One-to-Many (un produs ar putea avea multe inventory-uri)
    // CU UNIQUE un produs poate avea MAXIM un inventory
    //
    // DE CE TABEL SEPARAT SI NU COLOANE IN PRODUCTS?
    // 1. SEPARAREA RESPONSABILITATILOR (SRP)
    //    Product: informatii despre produs (nume, pret, descriere)
    //    Inventory: informatii despre stoc (cantitate, minim)
    //
    // 2. FRECVENTE DIFERITE DE UPDATE
    //    Product se modifica rar (schimbi pretul o data pe luna)
    //    Inventory se modifica frecvent (la fiecare vanzare)
    //
    // 3. DEMONSTRAREA CONCEPTULUI ONE-TO-ONE
    //    Conform cerintelor proiectului
    //
    // 4. POSIBILITATE EXTENSIE
    //    Putem adauga functionalitati de warehouse management
    //    fara a modifica structura produselor
    //
    // CONCEPTE DIN CURS DEMONSTRATE:
    // - Relatia One-to-One (pag. 8)
    // - Navigare proprietati (pag. 10)
    // - LINQ to Entities (pag. 11-12)
    // - SaveChanges() pentru persistenta (pag. 14)
    public interface IInventoryService
    {
        // CRUD - READ (CITIRE)

        // GetAllInventories - Returneaza toate inregistrarile de inventar
        //
        // INCLUDE (Eager Loading):
        // - Product: pentru a afisa numele produsului alaturi de stoc
        // - Product.Category: pentru filtrare/grupare pe categorii
        //
        // RETURNEAZA: Lista tuturor inventory-urilor cu produsele asociate
        //
        // FOLOSIRE: Dashboard de management al stocului
        List<Inventory> GetAllInventories();

        // GetInventoryById - Returneaza un inventory dupa ID-ul propriu
        //
        // PARAMETRU: inventoryId - ID-ul inventory-ului (NU ProductID!)
        //
        // RETURNEAZA: Inventory cu Product incarcat, sau null
        Inventory GetInventoryById(int inventoryId);

        // GetInventoryByProductId - Returneaza inventory-ul unui produs
        //
        // ACEASTA DEMONSTREAZA RELATIA ONE-TO-ONE!
        // Returneaza exact UN inventory pentru UN produs
        //
        // PARAMETRU: productId - ID-ul produsului
        //
        // RETURNEAZA: Inventory-ul produsului, sau null daca nu exista
        //
        // QUERY SQL GENERAT:
        // SELECT * FROM Inventory WHERE ProductID = @productId
        // Va returna maxim un rand datorita constrangerii UNIQUE
        Inventory GetInventoryByProductId(int productId);

        // GetLowStockInventories - Produsele cu stoc sub minim
        //
        // CONDITIE: StockQuantity < MinimumStock
        //
        // FOLOSIRE:
        // - Alerte "Low Stock" in dashboard
        // - Lista pentru reaprovizionare
        //
        // RETURNEAZA: Lista inventory-urilor cu stoc scazut
        // Ordonate descrescator dupa "cat de mult sub minim"
        List<Inventory> GetLowStockInventories();

        // GetOutOfStockInventories - Produsele epuizate
        //
        // CONDITIE: StockQuantity == 0
        //
        // FOLOSIRE: Alerte "Out of Stock"
        //
        // RETURNEAZA: Lista inventory-urilor cu stoc 0
        List<Inventory> GetOutOfStockInventories();

        // GetInventoriesByCategory - Stocul produselor dintr-o categorie
        //
        // PARAMETRU: categoryId - ID-ul categoriei
        //
        // DEMONSTREAZA: Navigarea relatiei Inventory -> Product -> Category
        //
        // RETURNEAZA: Inventarele produselor din acea categorie
        List<Inventory> GetInventoriesByCategory(int categoryId);

        // CRUD - CREATE (CREARE)

        // CreateInventory - Creeaza un inventory pentru un produs
        //
        // PARAMETRI:
        // - productId: produsul pentru care cream inventory
        // - stockQuantity: stocul initial (default: 0)
        // - minimumStock: pragul minim (default: 5)
        //
        // ATENTIE: Un produs nu poate avea MAI MULT de un inventory!
        // Daca deja exista, metoda returneaza false
        //
        // RETURNEAZA: true daca s-a creat, false daca exista deja
        //
        // NOTA: In practica, inventory-ul se creeaza automat
        // cand adaugam un produs (vezi ProductService.AddProduct)
        bool CreateInventory(int productId, int stockQuantity = 0, int minimumStock = 5);

        // CRUD - UPDATE (ACTUALIZARE)

        // UpdateStock - Actualizeaza cantitatea din stoc
        //
        // PARAMETRI:
        // - productId: produsul de actualizat
        // - newQuantity: noua cantitate
        //
        // ATENTIE: newQuantity nu poate fi negativ!
        //
        // RETURNEAZA: true daca s-a actualizat
        bool UpdateStock(int productId, int newQuantity);

        // IncreaseStock - Mareste stocul (reaprovizionare)
        //
        // PARAMETRI:
        // - productId: produsul de reaprovizionat
        // - quantity: cate bucati adaugam (trebuie > 0)
        //
        // FOLOSIRE: Cand primim marfa de la furnizor
        //
        // RETURNEAZA: true daca s-a adaugat
        bool IncreaseStock(int productId, int quantity);

        // DecreaseStock - Scade stocul (vanzare)
        //
        // PARAMETRI:
        // - productId: produsul vandut
        // - quantity: cate bucati s-au vandut
        //
        // ATENTIE: Nu poate scadea sub 0!
        // Daca quantity > StockQuantity, returneaza false
        //
        // RETURNEAZA: true daca s-a scazut
        //
        // NOTA: In practica, scaderea se face automat
        // la plasarea comenzii (vezi OrderService.PlaceOrder)
        bool DecreaseStock(int productId, int quantity);

        // UpdateMinimumStock - Actualizeaza pragul minim
        //
        // PARAMETRI:
        // - productId: produsul
        // - newMinimum: noul prag minim (trebuie >= 0)
        //
        // FOLOSIRE: StoreOwner ajusteaza pragul pentru produse
        // Produse populare -> prag mai mare
        // Produse rare -> prag mai mic
        //
        // RETURNEAZA: true daca s-a actualizat
        bool UpdateMinimumStock(int productId, int newMinimum);

        // INTEROGARI SPECIFICE

        // CanFulfillOrder - Putem onora o comanda pentru acest produs?
        //
        // PARAMETRI:
        // - productId: produsul
        // - quantity: cantitatea ceruta
        //
        // RETURNEAZA: true daca StockQuantity >= quantity
        //
        // FOLOSIRE: Verificare la adaugare in cos sau la checkout
        bool CanFulfillOrder(int productId, int quantity);

        // GetStockQuantity - Returneaza doar cantitatea din stoc
        //
        // PARAMETRU: productId - produsul
        //
        // RETURNEAZA:
        // - Cantitatea din stoc
        // - 0 daca produsul nu exista sau nu are inventory
        //
        // FOLOSIRE: Afisare rapida pe pagina produsului
        int GetStockQuantity(int productId);

        // IsLowStock - Verifica daca produsul e low stock
        //
        // PARAMETRU: productId
        //
        // RETURNEAZA: true daca StockQuantity < MinimumStock
        bool IsLowStock(int productId);

        // IsOutOfStock - Verifica daca produsul e epuizat
        //
        // PARAMETRU: productId
        //
        // RETURNEAZA: true daca StockQuantity == 0
        bool IsOutOfStock(int productId);

        // STATISTICI

        // GetLowStockCount - Cate produse sunt low stock
        //
        // RETURNEAZA: Numarul de produse cu stoc sub minim
        //
        // FOLOSIRE: Badge in dashboard "12 products low stock"
        int GetLowStockCount();

        // GetOutOfStockCount - Cate produse sunt epuizate
        //
        // RETURNEAZA: Numarul de produse cu stoc 0
        int GetOutOfStockCount();

        // GetTotalStockValue - Valoarea totala a stocului
        //
        // CALCUL: SUM(StockQuantity * Product.Price)
        //
        // FOLOSIRE: Raport financiar - valoarea inventarului
        //
        // RETURNEAZA: Suma valorii tuturor produselor in stoc
        decimal GetTotalStockValue();

        // OPERATII BULK (IN MASA)

        // BulkUpdateStock - Actualizeaza stocul pentru mai multe produse
        //
        // PARAMETRU: updates - dictionar {ProductID: NewQuantity}
        //
        // FOLOSIRE: Import din fisier Excel, sincronizare cu warehouse
        //
        // RETURNEAZA: Numarul de inventory-uri actualizate cu succes
        int BulkUpdateStock(Dictionary<int, int> updates);
    }
}
