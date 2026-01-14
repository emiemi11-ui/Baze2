using System;
using System.Collections.Generic;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // INTERFATA IORDERSERVICE - Contract pentru serviciul de comenzi
    //
    // CE ESTE O COMANDA IN E-COMMERCE?
    // O comanda reprezinta tranzactia de cumparare a unui client
    // Contine informatii despre:
    // - CINE a cumparat (CustomerID)
    // - CE a cumparat (OrderDetails - lista de produse)
    // - CAND a cumparat (OrderDate)
    // - CAT a platit (TotalAmount)
    // - UNDE livram (ShippingAddress)
    // - IN CE STARE e comanda (OrderStatus)
    //
    // PATTERN-UL HEADER-DETAIL:
    // Order = HEADER (informatii generale despre comanda)
    // OrderDetails = DETAIL (liniile comenzii - produsele)
    // Acest pattern e standard pentru documente cu mai multe linii
    //
    // WORKFLOW-UL UNEI COMENZI (OrderStatus):
    // 1. Pending - comanda tocmai plasata, asteapta procesare
    // 2. Processing - se pregateste (verificare stoc, impachetare)
    // 3. Shipped - expediata catre client
    // 4. Delivered - ajunsa la client
    // 5. Cancelled - anulata (de client sau magazin)
    //
    // RELATII DEMONSTRATE:
    // - One-to-Many: Order -> OrderDetails (o comanda are multe linii)
    // - Many-to-One: Order -> Customer (multe comenzi, un client)
    // - Many-to-One: OrderDetail -> Product (multe linii, un produs)
    //
    // CONCEPTE DIN CURS DEMONSTRATE:
    // - LINQ to Entities pentru interogari
    // - Eager Loading cu Include() pentru relatii
    // - SaveChanges() pentru salvare in baza de date
    // - Tranzactii pentru operatii multiple (plasare comanda + scadere stoc)
    public interface IOrderService
    {
        // CRUD - READ (CITIRE)

        // GetAllOrders - Returneaza toate comenzile din sistem
        //
        // FOLOSIRE: Dashboard-ul StoreOwner-ului pentru a vedea toate comenzile
        //
        // INCLUDE (Eager Loading):
        // - Customer: pentru a afisa numele clientului
        // - OrderDetails: pentru a vedea produsele comandate
        // - OrderDetails.Product: pentru a afisa numele produselor
        //
        // RETURNEAZA: Lista tuturor comenzilor, sortate descrescator dupa data
        List<Order> GetAllOrders();

        // GetOrderById - Returneaza o comanda specifica dupa ID
        //
        // PARAMETRU: orderId - ID-ul comenzii de returnat
        //
        // INCLUDE (Eager Loading):
        // - Customer: informatii despre client
        // - OrderDetails: liniile comenzii
        // - OrderDetails.Product: produsele din fiecare linie
        //
        // RETURNEAZA:
        // - Order complet cu toate relatiile incarcate
        // - null daca nu exista comanda cu acel ID
        //
        // FOLOSIRE:
        // - Pagina de detalii comanda
        // - Generare factura
        // - Procesare comanda de catre StoreOwner
        Order GetOrderById(int orderId);

        // GetOrdersByCustomer - Returneaza comenzile unui client
        //
        // PARAMETRU: customerId - ID-ul clientului
        //
        // FOLOSIRE: "My Orders" - istoricul comenzilor clientului
        //
        // INCLUDE: Customer, OrderDetails, OrderDetails.Product
        //
        // RETURNEAZA: Lista comenzilor clientului, cele mai recente primele
        List<Order> GetOrdersByCustomer(int customerId);

        // GetOrdersByStatus - Returneaza comenzile cu un anumit status
        //
        // PARAMETRU: status - "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
        //
        // FOLOSIRE:
        // - StoreOwner vede comenzile Pending pentru a le procesa
        // - StoreOwner vede comenzile Processing pentru a le expedia
        //
        // RETURNEAZA: Lista comenzilor cu statusul dat
        List<Order> GetOrdersByStatus(string status);

        // GetRecentOrders - Returneaza ultimele N comenzi
        //
        // PARAMETRU: count - cate comenzi sa returneze (default: 10)
        //
        // FOLOSIRE: Dashboard - sectiunea "Recent Orders"
        //
        // RETURNEAZA: Ultimele N comenzi, ordonate descrescator dupa data
        List<Order> GetRecentOrders(int count = 10);

        // GetOrdersByDateRange - Comenzile dintr-o perioada
        //
        // PARAMETRI:
        // - startDate: data de inceput a perioadei
        // - endDate: data de sfarsit a perioadei
        //
        // FOLOSIRE: Rapoarte de vanzari pe perioade
        //
        // RETURNEAZA: Comenzile plasate in intervalul specificat
        List<Order> GetOrdersByDateRange(DateTime startDate, DateTime endDate);

        // CRUD - CREATE (CREARE)

        // PlaceOrder - Plaseaza o comanda noua
        //
        // ACEASTA ESTE CEA MAI IMPORTANTA METODA!
        // Reprezinta momentul cand clientul confirma cumparatura
        //
        // PARAMETRI:
        // - customerId: clientul care plaseaza comanda
        // - orderDetails: lista de (ProductID, Quantity) pentru produsele comandate
        // - shippingAddress: adresa de livrare
        // - paymentMethod: metoda de plata
        //
        // FLOW INTERN:
        // 1. Creeaza obiectul Order cu status "Pending"
        // 2. Pentru fiecare produs din lista:
        //    a. Verifica daca produsul exista si e activ
        //    b. Verifica daca avem stoc suficient
        //    c. Creeaza OrderDetail cu pretul CURENT al produsului
        //    d. Scade stocul din Inventory
        // 3. Calculeaza TotalAmount din toate OrderDetails
        // 4. Salveaza comanda in baza de date (SaveChanges)
        //
        // RETURNEAZA:
        // - Order nou creat (cu OrderID populat)
        // - null daca a esuat (stoc insuficient, produs inexistent, etc.)
        //
        // NOTA IMPORTANTA:
        // Aceasta operatie trebuie sa fie ATOMICA (tranzactie)
        // Fie se salveaza comanda SI se scade stocul, fie nimic
        Order PlaceOrder(int customerId, List<OrderDetailInput> orderDetails,
                        string shippingAddress, string paymentMethod);

        // CRUD - UPDATE (ACTUALIZARE)

        // UpdateOrderStatus - Schimba statusul comenzii
        //
        // PARAMETRI:
        // - orderId: ID-ul comenzii
        // - newStatus: noul status ("Pending", "Processing", "Shipped", "Delivered", "Cancelled")
        //
        // REGULI DE TRANZITIE:
        // - Pending -> Processing (cand StoreOwner incepe procesarea)
        // - Processing -> Shipped (cand se expediaza)
        // - Shipped -> Delivered (cand ajunge la client)
        // - Pending/Processing -> Cancelled (anulare)
        // - NU poti anula o comanda deja Shipped sau Delivered
        //
        // RETURNEAZA: true daca statusul a fost actualizat
        bool UpdateOrderStatus(int orderId, string newStatus);

        // CancelOrder - Anuleaza o comanda
        //
        // PARAMETRU: orderId - ID-ul comenzii de anulat
        //
        // OPERATII:
        // 1. Verifica ca comanda poate fi anulata (Pending sau Processing)
        // 2. Seteaza OrderStatus = "Cancelled"
        // 3. RESTABILESTE stocul pentru fiecare produs din comanda
        //
        // RETURNEAZA:
        // - true daca anularea a reusit
        // - false daca comanda nu poate fi anulata (deja shipped/delivered)
        bool CancelOrder(int orderId);

        // UpdateShippingAddress - Actualizeaza adresa de livrare
        //
        // PARAMETRI:
        // - orderId: ID-ul comenzii
        // - newAddress: noua adresa de livrare
        //
        // NOTA: Se poate modifica doar pentru comenzi Pending sau Processing
        //
        // RETURNEAZA: true daca adresa a fost actualizata
        bool UpdateShippingAddress(int orderId, string newAddress);

        // INTEROGARI SPECIFICE

        // GetPendingOrders - Comenzile care asteapta procesare
        //
        // Shortcut pentru GetOrdersByStatus("Pending")
        // Folosit frecvent de StoreOwner
        List<Order> GetPendingOrders();

        // GetTodayOrders - Comenzile de azi
        //
        // FOLOSIRE: Dashboard - "Today's Orders"
        //
        // RETURNEAZA: Toate comenzile plasate in ziua curenta
        List<Order> GetTodayOrders();

        // GetCustomerOrderCount - Cate comenzi are un client
        //
        // PARAMETRU: customerId - ID-ul clientului
        //
        // FOLOSIRE: Statistici client, badge-uri (ex: "Gold Customer - 50+ orders")
        //
        // RETURNEAZA: Numarul total de comenzi ale clientului
        int GetCustomerOrderCount(int customerId);

        // GetCustomerTotalSpent - Cat a cheltuit un client in total
        //
        // PARAMETRU: customerId - ID-ul clientului
        //
        // FOLOSIRE: Statistici client, praguri pentru reduceri
        //
        // RETURNEAZA: Suma totala cheltuita (din comenzile ne-anulate)
        decimal GetCustomerTotalSpent(int customerId);

        // STATISTICI PENTRU DASHBOARD

        // GetOrderCountByStatus - Numar comenzi per status
        //
        // RETURNEAZA: Dictionar cu statusul si numarul de comenzi
        // Exemplu: { "Pending": 5, "Processing": 3, "Shipped": 10, ... }
        //
        // FOLOSIRE: Dashboard - grafic cu distributia comenzilor
        Dictionary<string, int> GetOrderCountByStatus();

        // GetTotalRevenue - Venitul total (comenzi ne-anulate)
        //
        // PARAMETRI (optionali):
        // - startDate: de cand (default: de la inceput)
        // - endDate: pana cand (default: acum)
        //
        // RETURNEAZA: Suma TotalAmount din toate comenzile ne-anulate
        decimal GetTotalRevenue(DateTime? startDate = null, DateTime? endDate = null);
    }

    // CLASA HELPER - OrderDetailInput
    //
    // ACEASTA CLASA NU E O ENTITATE!
    // Este un DTO (Data Transfer Object) folosit pentru a transmite
    // informatiile necesare la plasarea unei comenzi
    //
    // DE CE AVEM NEVOIE?
    // Cand clientul plaseaza o comanda, nu trimite obiecte OrderDetail complete
    // Trimite doar ProductID si Quantity
    // UnitPrice si Subtotal se calculeaza in service
    //
    // EXEMPLU FOLOSIRE:
    // var items = new List<OrderDetailInput>
    // {
    //     new OrderDetailInput { ProductID = 1, Quantity = 2 },
    //     new OrderDetailInput { ProductID = 5, Quantity = 1 }
    // };
    // var order = orderService.PlaceOrder(customerId, items, address, payment);
    public class OrderDetailInput
    {
        // ProductID - ID-ul produsului de comandat
        //
        // Se valideaza in PlaceOrder ca exista si e activ
        public int ProductID { get; set; }

        // Quantity - Cate bucati se comanda
        //
        // Se valideaza ca avem stoc suficient
        // Trebuie sa fie > 0
        public int Quantity { get; set; }
    }
}
