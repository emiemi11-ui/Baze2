using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA CATEGORY - Entitatea pentru categorii de produse
    //
    // CE SUNT CATEGORIILE?
    // Categoriile sunt grupuri logice pentru organizarea produselor
    // Ajuta clientii sa gaseasca rapid ce cauta
    // Permit filtrare si navigare structurata
    //
    // EXEMPLE DE CATEGORII:
    // - Electronics (telefoane, laptopuri, gadget-uri)
    // - Clothing (haine, incaltaminte)
    // - Books (carti fizice, ebook-uri)
    // - Home & Garden (decor, gradina)
    // - Sports (echipament sportiv, fitness)
    //
    // RELATIA CU PRODUCTS:
    // One-to-Many: O categorie poate avea MULTE produse
    // Dar un produs apartine unei SINGURE categorii
    //
    // NOTA: In sisteme mai complexe, ai putea avea:
    // - Categorii ierarhice (Electronics > Phones > Smartphones)
    // - Produse in mai multe categorii (Many-to-Many)
    // Pentru simplitate, folosim structura plata (flat)
    [Table("Categories")]
    public partial class Category
    {
        // CONSTRUCTORUL - Initializare colectie Products
        //
        // HashSet pentru performanta la adaugare/cautare
        // Initializat pentru a evita NullReferenceException
        public Category()
        {
            this.Products = new HashSet<Product>();
        }

        // PROPRIETATI - Coloanele din tabel

        // CategoryID - Cheia primara
        //
        // IDENTITY pentru auto-increment
        // Valori: 1, 2, 3, ...
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryID { get; set; }

        // CategoryName - Numele categoriei
        //
        // UNIQUE in baza de date - nu pot exista doua categorii cu acelasi nume
        // Required pentru ca o categorie fara nume nu are sens
        //
        // Acest nume apare in:
        // - Meniul de navigare
        // - Filtrul de categorii
        // - Pagina de produs
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        // Description - Descrierea categoriei
        //
        // Optionala - pentru a explica ce contine categoria
        // Poate aparea in:
        // - Pagina categoriei (header)
        // - Tooltip la hover
        // - SEO meta description
        [StringLength(500)]
        public string Description { get; set; }

        // IconCode - Codul Unicode pentru iconita
        //
        // Folosim caractere Unicode (emoji) pentru iconite simple
        // Exemple:
        //   "Electronics" - telefonul mobil
        //   "Clothing" - tricoul
        //   "Books" - cartile
        //
        // DE CE UNICODE SI NU IMAGINI?
        // 1. Simplu de implementat
        // 2. Scalabil (nu se pixeleaza)
        // 3. Functioneaza peste tot
        // 4. Nu necesita fisiere separate
        //
        // ALTERNATIVA: Font icons (FontAwesome, Material Icons)
        // Ar fi cod de stil "fa-phone" si CSS-ul l-ar afisa
        [StringLength(10)]
        public string IconCode { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Products - Produsele din aceasta categorie
        //
        // RELATIA ONE-TO-MANY
        // O categorie poate avea 0, 1 sau multe produse
        //
        // EF populeaza aceasta colectie cand:
        // 1. Eager Loading: context.Categories.Include(c => c.Products)
        // 2. Explicit Loading: context.Entry(category).Collection(c => c.Products).Load()
        //
        // FOLOSIRE:
        // category.Products.Count() - cate produse are categoria
        // category.Products.Where(p => p.IsActive) - produsele active din categorie
        public virtual ICollection<Product> Products { get; set; }

        // PROPRIETATI CALCULATE

        // ProductCount - Numarul de produse din categorie
        //
        // Util pentru afisare: "Electronics (45 products)"
        //
        // NOTA: Numara TOATE produsele, inclusiv cele inactive
        // Pentru doar cele active, ar trebui filtru suplimentar
        [NotMapped]
        public int ProductCount
        {
            get
            {
                if (Products == null)
                    return 0;

                return Products.Count;
            }
        }

        // ActiveProductCount - Numarul de produse ACTIVE din categorie
        //
        // Numara doar produsele care au IsActive = true
        // Acestea sunt produsele vizibile pentru clienti
        [NotMapped]
        public int ActiveProductCount
        {
            get
            {
                if (Products == null)
                    return 0;

                // LINQ Count cu predicat
                // Numara doar elementele care satisfac conditia
                return Products.Count(p => p.IsActive);
            }
        }

        // DisplayName - Numele pentru afisare cu iconita
        //
        // Combina IconCode si CategoryName pentru UI
        // Exemplu: " Electronics"
        [NotMapped]
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(IconCode))
                    return CategoryName;

                return $"{IconCode} {CategoryName}";
            }
        }
    }
}
