using System.Collections.Generic;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // INTERFATA IPRODUCTSERVICE - Contract pentru serviciul de produse
    //
    // CE OFERA ACEST SERVICIU?
    // Toate operatiile legate de produse:
    // - CRUD complet (Create, Read, Update, Delete)
    // - Cautare si filtrare
    // - Gestionare categorii
    // - Gestionare tag-uri (relatia Many-to-Many)
    //
    // DEMONSTREAZA CONCEPTE DIN CURS:
    // - LINQ to Entities (toate metodele)
    // - Eager Loading cu Include (GetAllProducts)
    // - Relatii One-to-Many (Product -> Category)
    // - Relatii Many-to-Many (Products <-> Tags)
    public interface IProductService
    {
        // CRUD - READ

        // GetAllProducts - Returneaza toate produsele active
        //
        // INCLUDE (Eager Loading):
        // - Category (pentru afisare nume categorie)
        // - Inventory (pentru afisare stoc)
        // - StoreOwner (pentru afisare cine l-a adaugat)
        List<Product> GetAllProducts();

        // GetProductById - Returneaza un produs dupa ID
        //
        // FOLOSESTE: Explicit Loading pentru relatii
        // Entry().Reference().Load() pentru Category, Inventory
        // Entry().Collection().Load() pentru Reviews, Tags
        Product GetProductById(int productId);

        // GetProductsByCategory - Produsele dintr-o categorie
        //
        // PARAMETRU: categoryId - ID-ul categoriei
        List<Product> GetProductsByCategory(int categoryId);

        // GetProductsByOwner - Produsele adaugate de un owner
        //
        // PARAMETRU: ownerId - ID-ul StoreOwner-ului
        List<Product> GetProductsByOwner(int ownerId);

        // SearchProducts - Cautare text in produse
        //
        // PARAMETRU: searchTerm - Termenul de cautat
        //
        // CAUTA IN:
        // - ProductName
        // - Description
        // - Category.CategoryName
        List<Product> SearchProducts(string searchTerm);

        // CRUD - CREATE

        // AddProduct - Adauga un produs nou
        //
        // PARAMETRU: product - Produsul de adaugat
        //
        // OPERATII:
        // 1. Valideaza datele
        // 2. Seteaza CreatedDate si IsActive
        // 3. Salveaza produsul
        // 4. Creeaza Inventory pentru produs (One-to-One)
        //
        // RETURNEAZA: true daca a reusit
        bool AddProduct(Product product);

        // CRUD - UPDATE

        // UpdateProduct - Actualizeaza un produs existent
        //
        // PARAMETRU: product - Produsul cu datele modificate
        //
        // RETURNEAZA: true daca a reusit
        bool UpdateProduct(Product product);

        // CRUD - DELETE

        // DeleteProduct - Soft delete produs
        //
        // PARAMETRU: productId - ID-ul produsului
        //
        // Seteaza IsActive = false (nu sterge fizic)
        bool DeleteProduct(int productId);

        // CATEGORII

        // GetAllCategories - Returneaza toate categoriile
        List<Category> GetAllCategories();

        // GetCategoryById - Returneaza o categorie dupa ID
        Category GetCategoryById(int categoryId);

        // AddCategory - Adauga o categorie noua
        bool AddCategory(Category category);

        // UpdateCategory - Actualizeaza o categorie
        bool UpdateCategory(Category category);

        // DeleteCategory - Sterge o categorie (doar daca nu are produse)
        bool DeleteCategory(int categoryId);

        // TAG-URI (Many-to-Many)

        // GetAllTags - Returneaza toate tag-urile
        List<Tag> GetAllTags();

        // GetProductTags - Returneaza tag-urile unui produs
        //
        // Demonstreaza navigarea relatiei Many-to-Many
        List<Tag> GetProductTags(int productId);

        // AddTagToProduct - Asociaza un tag la un produs
        //
        // PARAMETRI:
        // - productId: ID-ul produsului
        // - tagId: ID-ul tag-ului
        //
        // OPERATIE:
        // Adauga rand in tabelul ProductTags (gestionat de EF)
        bool AddTagToProduct(int productId, int tagId);

        // RemoveTagFromProduct - Elimina asocierea tag-produs
        bool RemoveTagFromProduct(int productId, int tagId);

        // AddTag - Creeaza un tag nou
        bool AddTag(Tag tag);

        // INTEROGARI AVANSATE

        // GetActiveProducts - Doar produsele active si cu stoc
        List<Product> GetActiveProducts();

        // GetFeaturedProducts - Produsele recomandate (Best Sellers, etc.)
        List<Product> GetFeaturedProducts(int count = 10);

        // GetRelatedProducts - Produse similare (din aceeasi categorie)
        List<Product> GetRelatedProducts(int productId, int count = 5);
    }
}
