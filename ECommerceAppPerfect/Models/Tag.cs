using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA TAG - Entitatea pentru etichete produse
    //
    // CE ESTE UN TAG?
    // Un tag este o eticheta care se poate atasa produselor
    // Tag-urile ajuta la:
    // - Categorisire suplimentara (pe langa categorii)
    // - Marketing (Best Seller, New Arrival)
    // - Filtrare (produse on sale, eco-friendly)
    //
    // EXEMPLE DE TAG-URI:
    // - "Best Seller" - produsele cele mai vandute
    // - "New Arrival" - produse nou adaugate
    // - "On Sale" - produse la reducere
    // - "Limited Stock" - stoc limitat
    // - "Premium" - produse premium
    // - "Eco-Friendly" - produse ecologice
    //
    // RELATIA MANY-TO-MANY CU PRODUCTS
    // Aceasta este relatia ceruta explicit in cerintele proiectului!
    //
    // CUM FUNCTIONEAZA MANY-TO-MANY?
    // - Un produs poate avea MULTE tag-uri
    // - Un tag poate fi pe MULTE produse
    //
    // IN BAZA DE DATE:
    // Relatia Many-to-Many se implementeaza cu un TABEL INTERMEDIAR:
    // ProductTags (ProductID, TagID)
    //
    // Acest tabel contine perechi produs-tag:
    // (1, 1) = Produs 1 are Tag 1
    // (1, 2) = Produs 1 are Tag 2
    // (2, 1) = Produs 2 are Tag 1
    //
    // IN ENTITY FRAMEWORK:
    // EF gestioneaza automat tabelul intermediar
    // Tu lucrezi direct cu colectiile: product.Tags, tag.Products
    [Table("Tags")]
    public partial class Tag
    {
        // CONSTRUCTORUL - Initializare colectie Products
        //
        // Pentru relatia Many-to-Many, ambele parti au colectii
        // Tag are colectie de Products
        // Product are colectie de Tags
        public Tag()
        {
            this.Products = new HashSet<Product>();
        }

        // PROPRIETATI - Coloanele din tabel

        // TagID - Cheia primara
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TagID { get; set; }

        // TagName - Numele tag-ului
        //
        // UNIQUE in baza de date - nu pot exista doua tag-uri cu acelasi nume
        //
        // Exemple: "Best Seller", "New Arrival", "On Sale"
        [Required]
        [StringLength(50)]
        public string TagName { get; set; }

        // TagColor - Culoarea tag-ului (cod HEX)
        //
        // Folosit pentru afisarea badge-ului in UI
        // Format: "#RRGGBB" sau "#RGB"
        //
        // EXEMPLE:
        // Best Seller: "#FFD700" (gold)
        // New Arrival: "#4CAF50" (green)
        // On Sale: "#F44336" (red)
        // Limited Stock: "#FF9800" (orange)
        // Premium: "#9C27B0" (purple)
        [StringLength(7)]
        public string TagColor { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Products - Produsele care au acest tag
        //
        // RELATIA MANY-TO-MANY (partea Products)
        // Un tag poate fi atasat la 0, 1 sau multe produse
        //
        // EF gestioneaza automat tabelul ProductTags
        // Cand faci tag.Products.Add(product), EF insereaza in ProductTags
        //
        // INCARCARE:
        // context.Tags.Include(t => t.Products).ToList()
        public virtual ICollection<Product> Products { get; set; }

        // PROPRIETATI CALCULATE

        // ProductCount - Cate produse au acest tag
        //
        // Util pentru afisare: "Best Seller (15 products)"
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

        // DisplayName - Numele pentru afisare cu culoare
        //
        // In UI, badge-ul va avea TagColor ca background
        // si TagName ca text
        [NotMapped]
        public string DisplayName => TagName;

        // HasColor - Are culoare definita?
        //
        // Pentru a stii daca aplicam culoare custom sau default
        [NotMapped]
        public bool HasColor => !string.IsNullOrEmpty(TagColor);

        // DefaultColor - Culoarea default daca nu e specificata
        //
        // Gri neutru pentru tag-uri fara culoare
        [NotMapped]
        public string DefaultColor => "#9E9E9E"; // Grey

        // EffectiveColor - Culoarea efectiva (TagColor sau default)
        //
        // Returneaza TagColor daca exista, altfel default
        [NotMapped]
        public string EffectiveColor => HasColor ? TagColor : DefaultColor;
    }
}
