using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ECommerceAppPerfect.Models
{
    // CLASA ORDER - Entitatea pentru comenzi
    //
    // CE ESTE O COMANDA?
    // O comanda reprezinta o tranzactie de cumparare
    // Clientul selecteaza produse, le pune in cos, si plaseaza comanda
    // Comanda contine informatii despre:
    // - CINE a comandat (CustomerID)
    // - CAND a comandat (OrderDate)
    // - CE a comandat (OrderDetails)
    // - CAT a platit (TotalAmount)
    // - UNDE livram (ShippingAddress)
    // - CUM plateste (PaymentMethod)
    // - IN CE STARE e (OrderStatus)
    //
    // PATTERN HEADER-DETAIL:
    // Orders = HEADER (informatii generale)
    // OrderDetails = DETAIL (liniile comenzii)
    // Acest pattern e standard pentru documente cu mai multe linii
    // (facturi, comenzi, bonuri, etc.)
    //
    // WORKFLOW COMANDA (OrderStatus):
    // 1. Pending - comanda tocmai plasata
    // 2. Processing - se pregateste (impachetare, verificare stoc)
    // 3. Shipped - expediata catre client
    // 4. Delivered - ajunsa la client
    // 5. Cancelled - anulata (de client sau magazin)
    //
    // RELATII:
    // - Customer (Many-to-One): Fiecare comanda apartine unui client
    // - OrderDetails (One-to-Many): O comanda are multe linii
    [Table("Orders")]
    public partial class Order
    {
        // CONSTRUCTORUL - Initializare colectie OrderDetails
        public Order()
        {
            this.OrderDetails = new HashSet<OrderDetail>();
        }

        // PROPRIETATI - Coloanele din tabel

        // OrderID - Cheia primara
        //
        // Numarul comenzii (1, 2, 3, ...)
        // Afisat clientului ca referinta: "Order #1234"
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderID { get; set; }

        // CustomerID - Foreign Key catre Users
        //
        // Clientul care a plasat comanda
        // TREBUIE sa fie un User cu UserRole = "Customer"
        [Required]
        [ForeignKey("Customer")]
        public int CustomerID { get; set; }

        // OrderDate - Data si ora plasarii comenzii
        //
        // DEFAULT GETDATE() = momentul exact cand s-a plasat
        // Util pentru:
        // - Istoric comenzi (sortat descrescator)
        // - Rapoarte de vanzari pe perioade
        // - SLA tracking (cat de repede procesam comenzile)
        public DateTime OrderDate { get; set; }

        // TotalAmount - Suma totala a comenzii
        //
        // Aceasta e suma platita de client
        // Calculata din OrderDetails: SUM(Quantity * UnitPrice)
        //
        // DE CE O STOCAM SI NU O CALCULAM?
        // 1. Performance - nu recalculam la fiecare afisare
        // 2. Audit - preturile se pot schimba, dar comanda ramane fix
        // 3. Taxe/discounturi - pot fi aplicate la nivel de comanda
        public decimal TotalAmount { get; set; }

        // OrderStatus - Starea curenta a comenzii
        //
        // VALORI POSIBILE (definite in SQL cu CHECK):
        // - "Pending" - in asteptare
        // - "Processing" - in procesare
        // - "Shipped" - expediata
        // - "Delivered" - livrata
        // - "Cancelled" - anulata
        //
        // DEFAULT "Pending" - comenzile noi sunt in asteptare
        [Required]
        [StringLength(20)]
        public string OrderStatus { get; set; }

        // ShippingAddress - Adresa de livrare
        //
        // Poate fi diferita de adresa utilizatorului
        // Clientul o poate schimba la checkout
        // Formatul: "Str. X, Nr. Y, Oras, Cod Postal"
        [StringLength(500)]
        public string ShippingAddress { get; set; }

        // PaymentMethod - Metoda de plata
        //
        // EXEMPLE:
        // - "Credit Card" - card de credit
        // - "PayPal" - platforma PayPal
        // - "Cash on Delivery" - ramburs
        // - "Bank Transfer" - transfer bancar
        [StringLength(50)]
        public string PaymentMethod { get; set; }

        // PROPRIETATI DE NAVIGARE

        // Customer - Clientul care a plasat comanda
        //
        // RELATIA MANY-TO-ONE
        // Multe comenzi pot fi de la acelasi client
        //
        // Acces: order.Customer.FullName - numele clientului
        public virtual User Customer { get; set; }

        // OrderDetails - Liniile comenzii
        //
        // RELATIA ONE-TO-MANY
        // O comanda are una sau mai multe linii
        // Fiecare linie = un produs cu cantitate si pret
        //
        // Acces: order.OrderDetails.Sum(od => od.Subtotal) - totalul calculat
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        // PROPRIETATI CALCULATE

        // OrderNumber - Numarul comenzii formatat
        //
        // Format: "#00001" (cu zero-uri de completare)
        // Arata mai profesional decat doar "1"
        [NotMapped]
        public string OrderNumber => $"#{OrderID:D5}";

        // ItemCount - Cate tipuri de produse are comanda
        //
        // Numara liniile (OrderDetails), nu cantitatile
        // Exemplu: 2 iPhone + 1 Headphones = 2 items (2 linii)
        [NotMapped]
        public int ItemCount
        {
            get
            {
                if (OrderDetails == null)
                    return 0;

                return OrderDetails.Count;
            }
        }

        // TotalQuantity - Cate produse in total (suma cantitatilor)
        //
        // Numara toate bucatile comandate
        // Exemplu: 2 iPhone + 1 Headphones = 3 bucati
        [NotMapped]
        public int TotalQuantity
        {
            get
            {
                if (OrderDetails == null || !OrderDetails.Any())
                    return 0;

                return OrderDetails.Sum(od => od.Quantity);
            }
        }

        // CalculatedTotal - Totalul calculat din OrderDetails
        //
        // Suma Subtotal-urilor din toate liniile
        // Ar trebui sa fie egal cu TotalAmount (pentru verificare)
        [NotMapped]
        public decimal CalculatedTotal
        {
            get
            {
                if (OrderDetails == null || !OrderDetails.Any())
                    return 0;

                return OrderDetails.Sum(od => od.Subtotal);
            }
        }

        // TotalFormatted - Totalul formatat pentru afisare
        //
        // Format: "1,649.98 RON"
        [NotMapped]
        public string TotalFormatted => $"{TotalAmount:N2} RON";

        // StatusColor - Culoarea pentru afisare status
        //
        // Fiecare status are o culoare asociata pentru UX
        // Verde pentru succes, rosu pentru anulat, etc.
        [NotMapped]
        public string StatusColor
        {
            get
            {
                return OrderStatus switch
                {
                    "Pending" => "#FF9800",      // Orange - in asteptare
                    "Processing" => "#2196F3",  // Blue - in lucru
                    "Shipped" => "#9C27B0",     // Purple - in drum
                    "Delivered" => "#4CAF50",   // Green - livrat
                    "Cancelled" => "#F44336",   // Red - anulat
                    _ => "#757575"              // Grey - default
                };
            }
        }

        // CanBeCancelled - Poate fi anulata comanda?
        //
        // Comenzile pot fi anulate doar daca nu au fost inca expediate
        // Dupa expediere, clientul trebuie sa faca retur
        [NotMapped]
        public bool CanBeCancelled
        {
            get
            {
                // Poate fi anulata daca e Pending sau Processing
                return OrderStatus == "Pending" || OrderStatus == "Processing";
            }
        }

        // DaysSinceOrder - Cate zile au trecut de la comanda
        //
        // Util pentru SLA si tracking
        // Exemplu: "Ordered 3 days ago"
        [NotMapped]
        public int DaysSinceOrder
        {
            get
            {
                return (DateTime.Now - OrderDate).Days;
            }
        }
    }
}
