using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA ORDERDETAIL - Entitatea pentru liniile comenzilor
    //
    // CE ESTE UN ORDER DETAIL?
    // Este o LINIE dintr-o comanda
    // Contine informatii despre UN produs comandat:
    // - Ce produs (ProductID)
    // - Cate bucati (Quantity)
    // - La ce pret (UnitPrice)
    // - Total linie (Subtotal)
    //
    // DE CE TABEL SEPARAT?
    // O comanda poate avea MAI MULTE produse diferite
    // Pattern-ul Header-Detail separa:
    // - Header (Order): informatii generale despre comanda
    // - Detail (OrderDetail): produsele comandate
    //
    // EXEMPLU:
    // Order #1 (Header):
    //   CustomerID: 2
    //   TotalAmount: 1649.98
    //
    // OrderDetails (Detail):
    //   Line 1: iPhone x1 @ 1299.99 = 1299.99
    //   Line 2: Headphones x1 @ 349.99 = 349.99
    //   Total: 1649.98
    //
    // RELATII:
    // - Order (Many-to-One): Fiecare linie apartine unei comenzi
    // - Product (Many-to-One): Fiecare linie refera un produs
    //
    // NOTA: Stocam UnitPrice pentru ca pretul produsului se poate schimba
    // Dar comanda trebuie sa pastreze pretul de la momentul cumpararii
    [Table("OrderDetails")]
    public partial class OrderDetail
    {
        // PROPRIETATI - Coloanele din tabel

        // OrderDetailID - Cheia primara
        //
        // Fiecare linie are ID unic
        // Nu trebuie sa fie unique per comanda (e global unic)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderDetailID { get; set; }

        // OrderID - Foreign Key catre Orders
        //
        // La ce comanda apartine aceasta linie
        // ON DELETE CASCADE in SQL: daca stergi comanda, se sterg si liniile
        [Required]
        [ForeignKey("Order")]
        public int OrderID { get; set; }

        // ProductID - Foreign Key catre Products
        //
        // Ce produs s-a comandat
        // NOTA: Produsul nu se sterge cand stergi comanda
        [Required]
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        // Quantity - Cate bucati s-au comandat
        //
        // CHECK (Quantity > 0) in SQL - nu poti comanda 0 sau negativ
        // Minim 1 bucata per linie
        [Required]
        public int Quantity { get; set; }

        // UnitPrice - Pretul per bucata LA MOMENTUL COMENZII
        //
        // IMPORTANT: Aceasta NU este Product.Price actual!
        // Este pretul de la momentul cand s-a plasat comanda
        //
        // DE CE STOCAM PRETUL?
        // Pretul produsului se poate schimba in timp
        // Dar comanda trebuie sa ramana cu pretul original
        // Altfel, facturile nu ar mai corespunde
        //
        // EXEMPLU:
        // Azi: Product.Price = 999.99, cumperi
        // Maine: Product.Price creste la 1099.99
        // Comanda ta ramane cu UnitPrice = 999.99
        public decimal UnitPrice { get; set; }

        // Subtotal - Coloana CALCULATA
        //
        // Subtotal = Quantity * UnitPrice
        //
        // PERSISTED in SQL inseamna ca valoarea e stocata fizic
        // Nu se recalculeaza la fiecare SELECT
        //
        // COMPUTED COLUMN in SQL:
        // Subtotal AS (Quantity * UnitPrice) PERSISTED
        //
        // IN C#:
        // [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        // spune EF sa nu incerce sa scrie in aceasta coloana
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal Subtotal { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Order - Comanda parinte
        //
        // RELATIA MANY-TO-ONE
        // Multe linii pot fi in aceeasi comanda
        //
        // Acces: orderDetail.Order.Customer.FullName
        public virtual Order Order { get; set; }

        // Product - Produsul comandat
        //
        // RELATIA MANY-TO-ONE
        // Acelasi produs poate fi in multe comenzi diferite
        //
        // Acces: orderDetail.Product.ProductName
        public virtual Product Product { get; set; }

        // PROPRIETATI CALCULATE

        // SubtotalFormatted - Subtotalul formatat pentru afisare
        //
        // Format: "1,299.99 RON"
        [NotMapped]
        public string SubtotalFormatted => $"{Subtotal:N2} RON";

        // UnitPriceFormatted - Pretul unitar formatat
        //
        // Format: "1,299.99 RON"
        [NotMapped]
        public string UnitPriceFormatted => $"{UnitPrice:N2} RON";

        // LineDescription - Descrierea liniei pentru afisare
        //
        // Format: "2x iPhone @ 1,299.99 RON = 2,599.98 RON"
        // Sau mai simplu: "iPhone x 2"
        [NotMapped]
        public string LineDescription
        {
            get
            {
                string productName = Product?.ProductName ?? $"Product #{ProductID}";
                return $"{productName} x {Quantity}";
            }
        }

        // CalculatedSubtotal - Subtotalul calculat (pentru verificare)
        //
        // Ar trebui sa fie egal cu Subtotal din baza de date
        // Util pentru debugging sau cand Subtotal nu s-a incarcat
        [NotMapped]
        public decimal CalculatedSubtotal => Quantity * UnitPrice;
    }
}
