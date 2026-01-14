using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // CLASA ORDERSERVICE - Implementarea serviciului de comenzi
    //
    // ACEASTA CLASA DEMONSTREAZA TOATE CONCEPTELE DIN CURS 10:
    //
    // 1. LINQ TO ENTITIES (pag. 11-12):
    // Folosim sintaxa Method Chain pentru query-uri
    // context.Orders.Where(o => o.OrderStatus == "Pending").ToList()
    // Se traduce automat in SQL: SELECT * FROM Orders WHERE OrderStatus = 'Pending'
    //
    // 2. EAGER LOADING (pag. 12-13):
    // Include() incarca entitatile relationate intr-un singur query
    // .Include(o => o.Customer).Include(o => o.OrderDetails)
    // Genereaza: SELECT * FROM Orders
    //            LEFT JOIN Users ON Orders.CustomerID = Users.UserID
    //            LEFT JOIN OrderDetails ON Orders.OrderID = OrderDetails.OrderID
    //
    // 3. NAVIGARE RELATII (pag. 8-11):
    // One-to-Many: Order -> OrderDetails (o comanda are multe linii)
    // Many-to-One: Order -> Customer (multe comenzi, un client)
    // Many-to-One: OrderDetail -> Product (multe linii, un produs)
    //
    // 4. SAVECHANGES() (pag. 14):
    // SaveChanges() persista toate modificarile in baza de date
    // EF genereaza automat INSERT, UPDATE, DELETE corespunzatoare
    //
    // 5. TRANZACTII (pag. 14-15):
    // PlaceOrder demonstreaza o tranzactie implicita
    // Toate modificarile se salveaza sau niciuna (atomicitate)
    //
    // PATTERN: Context-per-Operation
    // Fiecare metoda creeaza un DbContext nou cu using()
    // Aceasta garanteaza ca conexiunile se inchid corect
    public class OrderService : IOrderService, IDisposable
    {
        // FLAG DISPOSED - Pentru pattern-ul IDisposable
        private bool _disposed = false;

        // METODA HELPER - Creeaza un nou DbContext
        //
        // DE CE METODA SEPARATA?
        // Pentru a nu repeta "new ECommerceEntities()" in fiecare metoda
        // Si pentru a avea un singur punct de configurare
        //
        // DE CE CONTEXT NOU PENTRU FIECARE OPERATIE?
        // 1. DbContext NU e thread-safe
        // 2. Conexiunile se inchid automat cu "using"
        // 3. Evitam probleme de tracking (entitati modificate accidental)
        // 4. Fiecare operatie are un "snapshot" curat al datelor
        private ECommerceEntities GetContext()
        {
            return new ECommerceEntities();
        }

        // CRUD - READ (CITIRE)

        // GetAllOrders - Returneaza toate comenzile cu EAGER LOADING
        //
        // EAGER LOADING EXPLICAT:
        // Include() spune EF sa incarce relatiile in ACELASI query
        // Fara Include, ar face query-uri separate (N+1 problem)
        //
        // QUERY SQL GENERAT (aproximativ):
        // SELECT o.*, c.*, od.*, p.*
        // FROM Orders o
        // LEFT JOIN Users c ON o.CustomerID = c.UserID
        // LEFT JOIN OrderDetails od ON o.OrderID = od.OrderID
        // LEFT JOIN Products p ON od.ProductID = p.ProductID
        // ORDER BY o.OrderDate DESC
        //
        // OrderByDescending pune cele mai recente comenzi primele
        public List<Order> GetAllOrders()
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .Include(o => o.Customer)                    // Eager Loading - clientul
                    .Include(o => o.OrderDetails)                // Eager Loading - liniile comenzii
                    .Include(o => o.OrderDetails.Select(od => od.Product))  // Eager Loading - produsele
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
        }

        // GetOrderById - Returneaza o comanda cu toate detaliile
        //
        // DEMONSTREAZA: Combinatia de Include() pentru relatii multiple
        //
        // DE CE INCLUDEM TOATE RELATIILE?
        // Pentru pagina de detalii comanda avem nevoie de:
        // - Informatii client (Customer)
        // - Produsele comandate (OrderDetails -> Product)
        // - Stocul produselor (Product -> Inventory) - optional
        public Order GetOrderById(int orderId)
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .Include(o => o.OrderDetails.Select(od => od.Product.Inventory))
                    .FirstOrDefault(o => o.OrderID == orderId);
            }
        }

        // GetOrdersByCustomer - Istoricul comenzilor unui client
        //
        // LINQ Where clause filtreaza dupa CustomerID
        // Se traduce in: WHERE CustomerID = @customerId
        public List<Order> GetOrdersByCustomer(int customerId)
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .Where(o => o.CustomerID == customerId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
        }

        // GetOrdersByStatus - Filtrare dupa status
        //
        // UTILIZARE:
        // GetOrdersByStatus("Pending") - comenzile de procesat
        // GetOrdersByStatus("Shipped") - comenzile expediate
        public List<Order> GetOrdersByStatus(string status)
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .Where(o => o.OrderStatus == status)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
        }

        // GetRecentOrders - Ultimele N comenzi
        //
        // LINQ Take() limiteaza numarul de rezultate
        // Similar cu SQL TOP sau LIMIT
        //
        // OrderByDescending + Take = cele mai recente N comenzi
        public List<Order> GetRecentOrders(int count = 10)
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .OrderByDescending(o => o.OrderDate)
                    .Take(count)
                    .ToList();
            }
        }

        // GetOrdersByDateRange - Comenzile dintr-o perioada
        //
        // DEMONSTREAZA: Comparatii de date in LINQ
        // o.OrderDate >= startDate AND o.OrderDate <= endDate
        //
        // DbFunctions.TruncateTime ar putea fi folosit pentru a ignora ora
        // dar pentru rapoarte e mai precis sa includem si ora
        public List<Order> GetOrdersByDateRange(DateTime startDate, DateTime endDate)
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
        }

        // CRUD - CREATE (CREARE)

        // PlaceOrder - Plaseaza o comanda noua
        //
        // ACEASTA E CEA MAI COMPLEXA METODA DIN SERVICIU!
        // Demonstreaza:
        // 1. Crearea de entitati noi
        // 2. Relatii intre entitati (Order -> OrderDetails)
        // 3. Modificarea altor entitati (Inventory - scadere stoc)
        // 4. Calcule (TotalAmount)
        // 5. Atomicitate - toate operatiile sau niciuna
        //
        // FLOW:
        // 1. Validare parametri
        // 2. Creare Order header
        // 3. Pentru fiecare produs din lista:
        //    - Verificare existenta si stoc
        //    - Creare OrderDetail
        //    - Scadere stoc
        // 4. Calcul total
        // 5. SaveChanges() - totul se salveaza o singura data
        //
        // ATOMICITATE (TRANZACTIE IMPLICITA):
        // Daca oricare operatie esueaza, exceptia va face ca
        // SaveChanges() sa nu fie apelat, deci nimic nu se salveaza
        // In Entity Framework, un singur SaveChanges() e atomic
        public Order PlaceOrder(int customerId, List<OrderDetailInput> orderDetails,
                               string shippingAddress, string paymentMethod)
        {
            // VALIDARE INPUT
            // Verificam ca avem cel putin un produs in comanda
            if (orderDetails == null || !orderDetails.Any())
                return null;

            try
            {
                using (var context = GetContext())
                {
                    // VERIFICARE CLIENT
                    // Clientul trebuie sa existe si sa fie activ
                    var customer = context.Users.Find(customerId);
                    if (customer == null || !customer.IsActive)
                        return null;

                    // CREARE ORDER HEADER
                    // La inceput TotalAmount e 0, il calculam dupa ce adaugam liniile
                    var order = new Order
                    {
                        CustomerID = customerId,
                        OrderDate = DateTime.Now,
                        OrderStatus = "Pending",        // Comenzile noi sunt Pending
                        ShippingAddress = shippingAddress,
                        PaymentMethod = paymentMethod,
                        TotalAmount = 0                  // Se calculeaza mai jos
                    };

                    // Adaugam comanda in context (inca nu e salvata in DB)
                    context.Orders.Add(order);

                    // PROCESARE FIECARE LINIE DIN COMANDA
                    decimal totalAmount = 0;

                    foreach (var item in orderDetails)
                    {
                        // VERIFICARE PRODUS
                        // Produsul trebuie sa existe si sa fie activ
                        var product = context.Products
                            .Include(p => p.Inventory)
                            .FirstOrDefault(p => p.ProductID == item.ProductID && p.IsActive);

                        if (product == null)
                        {
                            // Produs inexistent sau inactiv - comanda esueaza
                            return null;
                        }

                        // VERIFICARE STOC
                        // Trebuie sa avem cantitatea ceruta in stoc
                        if (product.Inventory == null ||
                            product.Inventory.StockQuantity < item.Quantity)
                        {
                            // Stoc insuficient - comanda esueaza
                            return null;
                        }

                        // CREARE ORDER DETAIL
                        // UnitPrice e pretul CURENT al produsului
                        // Il salvam pentru ca pretul se poate schimba in viitor
                        var orderDetail = new OrderDetail
                        {
                            // OrderID se seteaza automat de EF cand salvam
                            // pentru ca am adaugat order-ul in context
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = product.Price
                            // Subtotal e coloana calculata in SQL, nu o setam noi
                        };

                        // Adaugam linia la comanda
                        // EF stie ca appartine acestei comenzi
                        order.OrderDetails.Add(orderDetail);

                        // SCADERE STOC
                        // Folosim metoda din model pentru a scade stocul
                        product.Inventory.ReduceStock(item.Quantity);

                        // CALCUL SUBTOTAL
                        // Adunam la total: cantitate * pret unitar
                        totalAmount += item.Quantity * product.Price;
                    }

                    // SETARE TOTAL COMANDA
                    order.TotalAmount = totalAmount;

                    // SAVECHANGES - Totul se salveaza acum
                    //
                    // Aceasta operatie face:
                    // 1. INSERT INTO Orders (...) VALUES (...)
                    // 2. INSERT INTO OrderDetails (...) pentru fiecare linie
                    // 3. UPDATE Inventory SET StockQuantity = ... pentru fiecare produs
                    //
                    // Toate in aceeasi tranzactie - sau reusesc toate, sau niciuna
                    context.SaveChanges();

                    // Returnam comanda cu OrderID populat de SQL Server
                    return order;
                }
            }
            catch (Exception)
            {
                // Log exception in productie
                // Orice exceptie inseamna ca comanda nu s-a plasat
                return null;
            }
        }

        // CRUD - UPDATE (ACTUALIZARE)

        // UpdateOrderStatus - Actualizeaza statusul comenzii
        //
        // DEMONSTREAZA: Pattern-ul "Find and Update"
        // 1. Gaseste entitatea dupa ID
        // 2. Modifica proprietatile
        // 3. SaveChanges() detecteaza modificarile si genereaza UPDATE
        //
        // CHANGE TRACKING:
        // EF urmareste automat modificarile pe entitatile incarcate
        // Cand apelam SaveChanges(), genereaza:
        // UPDATE Orders SET OrderStatus = @newStatus WHERE OrderID = @orderId
        public bool UpdateOrderStatus(int orderId, string newStatus)
        {
            // VALIDARE STATUS
            // Acceptam doar statusuri valide
            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // FIND - Gasim comanda
                    var order = context.Orders.Find(orderId);

                    if (order == null)
                        return false;

                    // REGULI DE TRANZITIE
                    // Nu putem anula o comanda deja expediata sau livrata
                    if (newStatus == "Cancelled" &&
                        (order.OrderStatus == "Shipped" || order.OrderStatus == "Delivered"))
                    {
                        return false;
                    }

                    // UPDATE - Schimbam statusul
                    order.OrderStatus = newStatus;

                    // SAVECHANGES - Salvam modificarea
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // CancelOrder - Anuleaza comanda si RESTABILESTE stocul
        //
        // OPERATIE COMPLEXA:
        // Nu doar schimbam statusul, ci si restabilim stocul!
        // Pentru fiecare produs din comanda, crestem inapoi cantitatea
        //
        // DEMONSTREAZA:
        // 1. Include() pentru a incarca OrderDetails
        // 2. Iterare pe colectie de navigare
        // 3. Modificare entitati relationate (Inventory)
        public bool CancelOrder(int orderId)
        {
            try
            {
                using (var context = GetContext())
                {
                    // EAGER LOADING - Incarcam comanda cu toate detaliile
                    var order = context.Orders
                        .Include(o => o.OrderDetails)
                        .Include(o => o.OrderDetails.Select(od => od.Product))
                        .Include(o => o.OrderDetails.Select(od => od.Product.Inventory))
                        .FirstOrDefault(o => o.OrderID == orderId);

                    if (order == null)
                        return false;

                    // VALIDARE - Poate fi anulata?
                    // Doar comenzile Pending sau Processing pot fi anulate
                    if (order.OrderStatus != "Pending" && order.OrderStatus != "Processing")
                        return false;

                    // RESTABILIRE STOC
                    // Pentru fiecare linie din comanda, crestem stocul inapoi
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product?.Inventory != null)
                        {
                            // IncreaseStock() din model creste cantitatea
                            detail.Product.Inventory.IncreaseStock(detail.Quantity);
                        }
                    }

                    // SETARE STATUS CANCELLED
                    order.OrderStatus = "Cancelled";

                    // SAVECHANGES - Salveaza:
                    // 1. UPDATE Orders SET OrderStatus = 'Cancelled'
                    // 2. UPDATE Inventory SET StockQuantity = ... (pentru fiecare produs)
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // UpdateShippingAddress - Actualizeaza adresa de livrare
        //
        // DEMONSTREAZA: Update simplu pe o singura proprietate
        public bool UpdateShippingAddress(int orderId, string newAddress)
        {
            if (string.IsNullOrWhiteSpace(newAddress))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var order = context.Orders.Find(orderId);

                    if (order == null)
                        return false;

                    // Doar comenzile neexpediate pot fi modificate
                    if (order.OrderStatus != "Pending" && order.OrderStatus != "Processing")
                        return false;

                    order.ShippingAddress = newAddress;
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

        // GetPendingOrders - Shortcut pentru comenzile in asteptare
        public List<Order> GetPendingOrders()
        {
            return GetOrdersByStatus("Pending");
        }

        // GetTodayOrders - Comenzile din ziua curenta
        //
        // DEMONSTREAZA: DbFunctions.TruncateTime pentru comparatii de date
        // TruncateTime elimina partea de timp, compara doar data
        //
        // ALTERNATIVA: Comparatie cu DateTime.Today (inceputul zilei)
        public List<Order> GetTodayOrders()
        {
            using (var context = GetContext())
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                return context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderDetails.Select(od => od.Product))
                    .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
        }

        // GetCustomerOrderCount - Cate comenzi are un client
        //
        // DEMONSTREAZA: LINQ Count() cu conditie
        // Se traduce in: SELECT COUNT(*) FROM Orders WHERE CustomerID = @id
        public int GetCustomerOrderCount(int customerId)
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .Count(o => o.CustomerID == customerId);
            }
        }

        // GetCustomerTotalSpent - Cat a cheltuit clientul
        //
        // DEMONSTREAZA: LINQ Sum() cu conditie
        // Excludem comenzile anulate
        //
        // QUERY GENERAT:
        // SELECT SUM(TotalAmount) FROM Orders
        // WHERE CustomerID = @id AND OrderStatus != 'Cancelled'
        public decimal GetCustomerTotalSpent(int customerId)
        {
            using (var context = GetContext())
            {
                var total = context.Orders
                    .Where(o => o.CustomerID == customerId && o.OrderStatus != "Cancelled")
                    .Sum(o => (decimal?)o.TotalAmount);

                // Sum() pe lista goala returneaza null, nu 0
                // De aceea folosim (decimal?) si ?? 0
                return total ?? 0;
            }
        }

        // STATISTICI PENTRU DASHBOARD

        // GetOrderCountByStatus - Distributia comenzilor pe status
        //
        // DEMONSTREAZA: LINQ GroupBy() cu proiectie in Dictionary
        //
        // QUERY GENERAT:
        // SELECT OrderStatus, COUNT(*) as Count
        // FROM Orders
        // GROUP BY OrderStatus
        //
        // ToDictionary() transforma rezultatul in Dictionary<string, int>
        public Dictionary<string, int> GetOrderCountByStatus()
        {
            using (var context = GetContext())
            {
                return context.Orders
                    .GroupBy(o => o.OrderStatus)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        // GetTotalRevenue - Venitul total in perioada data
        //
        // DEMONSTREAZA: Parametri optionali si Sum() conditionat
        public decimal GetTotalRevenue(DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var context = GetContext())
            {
                var query = context.Orders
                    .Where(o => o.OrderStatus != "Cancelled");

                // Aplicam filtru de data doar daca e specificat
                if (startDate.HasValue)
                    query = query.Where(o => o.OrderDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(o => o.OrderDate <= endDate.Value);

                var total = query.Sum(o => (decimal?)o.TotalAmount);

                return total ?? 0;
            }
        }

        // DISPOSE PATTERN

        // Dispose - Elibereaza resursele
        //
        // In cazul nostru, nu avem resurse de eliberat permanent
        // (context-urile se elibereaza cu "using")
        // Dar implementam pattern-ul pentru consistenta si pentru
        // cazul in care in viitor vom adauga resurse
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
                    // Elibereaza resurse managed
                    // (nu avem in acest caz)
                }

                _disposed = true;
            }
        }
    }
}
