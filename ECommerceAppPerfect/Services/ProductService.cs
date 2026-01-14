using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // CLASA PRODUCTSERVICE - Implementarea serviciului de produse
    //
    // ACEASTA CLASA DEMONSTREAZA TOATE CONCEPTELE DIN CURS 10:
    //
    // 1. LINQ to Entities (pag. 11-12):
    // - Sintaxa Query: from p in context.Products where p.IsActive select p
    // - Sintaxa Method: context.Products.Where(p => p.IsActive).ToList()
    // Ambele se traduc in acelasi SQL!
    //
    // 2. EAGER LOADING (pag. 12-13):
    // Include() incarca entitatile relationate IMEDIAT
    // .Include(p => p.Category) -> LEFT JOIN Categories
    //
    // 3. EXPLICIT LOADING (pag. 13):
    // Entry().Reference().Load() - pentru relatii One-to-One/Many-to-One
    // Entry().Collection().Load() - pentru relatii One-to-Many
    //
    // 4. RELATII (pag. 8-11):
    // - One-to-One: Product <-> Inventory
    // - One-to-Many: Category -> Products
    // - Many-to-Many: Products <-> Tags (prin ProductTags)
    public class ProductService : IProductService, IDisposable
    {
        private bool _disposed = false;

        // HELPER - Creeaza context nou
        private ECommerceEntities GetContext()
        {
            return new ECommerceEntities();
        }

        // CRUD - READ

        // GetAllProducts - Cu EAGER LOADING
        //
        // Include() incarca entitatile relationate intr-un singur query
        // FARA Include: Ai N+1 query-uri (1 pentru produse + 1 pentru fiecare relatie)
        // CU Include: Un singur query cu JOIN-uri
        //
        // QUERY SQL GENERAT:
        // SELECT * FROM Products p
        // LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
        // LEFT JOIN Inventory i ON p.ProductID = i.ProductID
        // LEFT JOIN Users u ON p.StoreOwnerID = u.UserID
        // WHERE p.IsActive = 1
        // ORDER BY p.ProductName
        public List<Product> GetAllProducts()
        {
            using (var context = GetContext())
            {
                return context.Products
                    .Include(p => p.Category)        // Eager Loading categoria
                    .Include(p => p.Inventory)       // Eager Loading inventory (One-to-One!)
                    .Include(p => p.StoreOwner)      // Eager Loading owner
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.ProductName)
                    .ToList();
            }
        }

        // GetProductById - Cu EXPLICIT LOADING
        //
        // Find() gaseste produsul dupa PK (rapid, foloseste cache)
        // Entry().Reference().Load() incarca explicit o relatie single
        // Entry().Collection().Load() incarca explicit o colectie
        //
        // DE CE EXPLICIT SI NU EAGER?
        // Pentru ca Find() nu suporta Include()
        // Si pentru a demonstra Explicit Loading din curs
        public Product GetProductById(int productId)
        {
            using (var context = GetContext())
            {
                // Find() cauta dupa Primary Key
                // Mai rapid decat FirstOrDefault() pentru PK
                var product = context.Products.Find(productId);

                if (product != null)
                {
                    // EXPLICIT LOADING - Reference pentru relatii single

                    // Category: Many-to-One (un produs are o categorie)
                    context.Entry(product)
                        .Reference(p => p.Category)
                        .Load();

                    // Inventory: One-to-One (un produs are un inventory)
                    context.Entry(product)
                        .Reference(p => p.Inventory)
                        .Load();

                    // StoreOwner: Many-to-One
                    context.Entry(product)
                        .Reference(p => p.StoreOwner)
                        .Load();

                    // EXPLICIT LOADING - Collection pentru colectii

                    // Reviews: One-to-Many (un produs are multe reviews)
                    context.Entry(product)
                        .Collection(p => p.Reviews)
                        .Load();

                    // Tags: Many-to-Many (un produs are multe tags)
                    context.Entry(product)
                        .Collection(p => p.Tags)
                        .Load();
                }

                return product;
            }
        }

        // GetProductsByCategory - Filtrare dupa categorie
        //
        // Demonstreaza LINQ Where clause
        // WHERE CategoryID = @categoryId AND IsActive = 1
        public List<Product> GetProductsByCategory(int categoryId)
        {
            using (var context = GetContext())
            {
                return context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Where(p => p.CategoryID == categoryId && p.IsActive)
                    .OrderBy(p => p.ProductName)
                    .ToList();
            }
        }

        // GetProductsByOwner - Produsele unui owner
        public List<Product> GetProductsByOwner(int ownerId)
        {
            using (var context = GetContext())
            {
                return context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Where(p => p.StoreOwnerID == ownerId && p.IsActive)
                    .OrderBy(p => p.ProductName)
                    .ToList();
            }
        }

        // SearchProducts - Cautare full-text (simplificata)
        //
        // Contains() se traduce in SQL LIKE '%searchTerm%'
        // Cautam in mai multe coloane cu OR
        public List<Product> SearchProducts(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllProducts();

            searchTerm = searchTerm.ToLower();

            using (var context = GetContext())
            {
                return context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Where(p => p.IsActive &&
                        (p.ProductName.ToLower().Contains(searchTerm) ||
                         p.Description.ToLower().Contains(searchTerm) ||
                         p.Category.CategoryName.ToLower().Contains(searchTerm)))
                    .OrderBy(p => p.ProductName)
                    .ToList();
            }
        }

        // CRUD - CREATE

        // AddProduct - Adauga produs nou
        //
        // DEMONSTREAZA: SaveChanges() si adaugare entitati relationate
        public bool AddProduct(Product product)
        {
            if (product == null)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // Setam valori default
                    product.CreatedDate = DateTime.Now;
                    product.IsActive = true;

                    // Adaugam produsul
                    context.Products.Add(product);

                    // SaveChanges() face INSERT in DB
                    // Product.ProductID se populeaza automat (IDENTITY)
                    context.SaveChanges();

                    // Cream Inventory pentru produs (relatie One-to-One)
                    // Fiecare produs TREBUIE sa aiba inventory
                    var inventory = new Inventory
                    {
                        ProductID = product.ProductID,  // FK catre produsul nou creat
                        StockQuantity = 0,               // Default: fara stoc
                        MinimumStock = 5,                // Default: minim 5 bucati
                        LastUpdated = DateTime.Now
                    };

                    context.Inventories.Add(inventory);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                // Log exception in productie
                return false;
            }
        }

        // CRUD - UPDATE

        // UpdateProduct - Actualizeaza produs
        //
        // DEMONSTREAZA: Change Tracking
        // EF urmareste modificarile si genereaza UPDATE doar pentru coloanele schimbate
        public bool UpdateProduct(Product product)
        {
            if (product == null)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // Gasim produsul existent
                    var existingProduct = context.Products.Find(product.ProductID);

                    if (existingProduct == null)
                        return false;

                    // Actualizam campurile
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.CategoryID = product.CategoryID;
                    existingProduct.ImageURL = product.ImageURL;
                    existingProduct.IsActive = product.IsActive;

                    // SaveChanges() detecteaza modificarile
                    // Genereaza: UPDATE Products SET ... WHERE ProductID = @id
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // CRUD - DELETE

        // DeleteProduct - Soft delete
        //
        // Nu stergem fizic pentru ca:
        // - Produsul poate fi in comenzi existente
        // - Poate avea reviews
        // - Pastram istoricul
        public bool DeleteProduct(int productId)
        {
            try
            {
                using (var context = GetContext())
                {
                    var product = context.Products.Find(productId);

                    if (product == null)
                        return false;

                    product.IsActive = false;
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // CATEGORII

        public List<Category> GetAllCategories()
        {
            using (var context = GetContext())
            {
                return context.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToList();
            }
        }

        public Category GetCategoryById(int categoryId)
        {
            using (var context = GetContext())
            {
                return context.Categories.Find(categoryId);
            }
        }

        public bool AddCategory(Category category)
        {
            if (category == null)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // Verificam unicitatea numelui
                    if (context.Categories.Any(c => c.CategoryName == category.CategoryName))
                        return false;

                    context.Categories.Add(category);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UpdateCategory(Category category)
        {
            if (category == null)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var existing = context.Categories.Find(category.CategoryID);
                    if (existing == null)
                        return false;

                    existing.CategoryName = category.CategoryName;
                    existing.Description = category.Description;
                    existing.IconCode = category.IconCode;

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteCategory(int categoryId)
        {
            try
            {
                using (var context = GetContext())
                {
                    // Verificam daca are produse
                    if (context.Products.Any(p => p.CategoryID == categoryId))
                        return false;

                    var category = context.Categories.Find(categoryId);
                    if (category == null)
                        return false;

                    context.Categories.Remove(category);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // TAG-URI - MANY-TO-MANY

        public List<Tag> GetAllTags()
        {
            using (var context = GetContext())
            {
                return context.Tags
                    .OrderBy(t => t.TagName)
                    .ToList();
            }
        }

        // GetProductTags - Navigare relatie Many-to-Many
        //
        // DEMONSTREAZA: Accesarea colectiei de navigare
        // product.Tags returneaza toate tag-urile asociate
        public List<Tag> GetProductTags(int productId)
        {
            using (var context = GetContext())
            {
                var product = context.Products
                    .Include(p => p.Tags)  // Many-to-Many navigation
                    .FirstOrDefault(p => p.ProductID == productId);

                return product?.Tags.ToList() ?? new List<Tag>();
            }
        }

        // AddTagToProduct - Adauga asociere Many-to-Many
        //
        // DEMONSTREAZA: Cum EF gestioneaza tabelul intermediar
        // product.Tags.Add(tag) -> INSERT INTO ProductTags (ProductID, TagID)
        public bool AddTagToProduct(int productId, int tagId)
        {
            try
            {
                using (var context = GetContext())
                {
                    var product = context.Products
                        .Include(p => p.Tags)
                        .FirstOrDefault(p => p.ProductID == productId);

                    var tag = context.Tags.Find(tagId);

                    if (product == null || tag == null)
                        return false;

                    // Verificam daca asocierea exista deja
                    if (product.Tags.Any(t => t.TagID == tagId))
                        return true; // Deja exista, nu e eroare

                    // Adaugam asocierea
                    // EF va face INSERT in ProductTags automat
                    product.Tags.Add(tag);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // RemoveTagFromProduct - Elimina asociere Many-to-Many
        //
        // product.Tags.Remove(tag) -> DELETE FROM ProductTags WHERE ProductID = @pid AND TagID = @tid
        public bool RemoveTagFromProduct(int productId, int tagId)
        {
            try
            {
                using (var context = GetContext())
                {
                    var product = context.Products
                        .Include(p => p.Tags)
                        .FirstOrDefault(p => p.ProductID == productId);

                    var tag = product?.Tags.FirstOrDefault(t => t.TagID == tagId);

                    if (product == null || tag == null)
                        return false;

                    // Eliminam asocierea
                    product.Tags.Remove(tag);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool AddTag(Tag tag)
        {
            if (tag == null)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    if (context.Tags.Any(t => t.TagName == tag.TagName))
                        return false;

                    context.Tags.Add(tag);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // INTEROGARI AVANSATE

        // GetActiveProducts - Cu stoc disponibil
        public List<Product> GetActiveProducts()
        {
            using (var context = GetContext())
            {
                return context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Where(p => p.IsActive && p.Inventory.StockQuantity > 0)
                    .OrderBy(p => p.ProductName)
                    .ToList();
            }
        }

        // GetFeaturedProducts - Produse recomandate
        //
        // Criteriu: Tag "Best Seller" sau cele mai vandute
        public List<Product> GetFeaturedProducts(int count = 10)
        {
            using (var context = GetContext())
            {
                return context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Tags)
                    .Where(p => p.IsActive &&
                        p.Tags.Any(t => t.TagName == "Best Seller"))
                    .Take(count)
                    .ToList();
            }
        }

        // GetRelatedProducts - Produse similare
        public List<Product> GetRelatedProducts(int productId, int count = 5)
        {
            using (var context = GetContext())
            {
                var product = context.Products.Find(productId);
                if (product == null)
                    return new List<Product>();

                return context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Where(p => p.CategoryID == product.CategoryID &&
                               p.ProductID != productId &&
                               p.IsActive)
                    .Take(count)
                    .ToList();
            }
        }

        // DISPOSE
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
