using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // CLASA REPORTSERVICE - Implementarea serviciului de rapoarte
    //
    // ACEASTA CLASA DEMONSTREAZA RAW SQL SI STORED PROCEDURES IN ENTITY FRAMEWORK!
    //
    // CONCEPTE DIN CURS (pag. 14-15):
    //
    // 1. Database.SqlQuery<T>(sql, params)
    //    Executa query SQL si mapeaza rezultatele la tipul T
    //    T poate fi o entitate sau un DTO
    //
    //    EXEMPLU:
    //    var results = context.Database.SqlQuery<ProductDTO>(
    //        "SELECT ProductID, ProductName FROM Products WHERE Price > @price",
    //        new SqlParameter("@price", 100)
    //    ).ToList();
    //
    // 2. Database.ExecuteSqlCommand(sql, params)
    //    Executa comenzi SQL care modifica date (INSERT, UPDATE, DELETE)
    //    Returneaza numarul de randuri afectate
    //
    //    EXEMPLU:
    //    int rowsAffected = context.Database.ExecuteSqlCommand(
    //        "UPDATE Products SET Price = Price * 1.1 WHERE CategoryID = @catId",
    //        new SqlParameter("@catId", 5)
    //    );
    //
    // 3. Apelarea Stored Procedures
    //    Se face tot cu SqlQuery sau ExecuteSqlCommand
    //    Sintaxa: "EXEC NumeProcedura @param1, @param2"
    //
    //    EXEMPLU:
    //    var results = context.Database.SqlQuery<LowStockDTO>(
    //        "EXEC GetLowStockProducts"
    //    ).ToList();
    //
    // DE CE RAW SQL IN LOC DE LINQ?
    // 1. PERFORMANTA: Query-uri optimizate manual
    // 2. COMPLEXITATE: CTE-uri, Window Functions, etc.
    // 3. COMPATIBILITATE: Cod SQL existent
    // 4. STORED PROCEDURES: Logica centralizata in DB
    //
    // ATENTIE LA SQL INJECTION!
    // MEREU folositi parametri (@param) in loc de concatenare de string-uri!
    // GRESIT: "SELECT * FROM Users WHERE Name = '" + name + "'"
    // CORECT: "SELECT * FROM Users WHERE Name = @name", new SqlParameter("@name", name)
    public class ReportService : IReportService, IDisposable
    {
        private bool _disposed = false;

        // HELPER - Creeaza un nou DbContext
        private ECommerceEntities GetContext()
        {
            return new ECommerceEntities();
        }

        // RAPOARTE DIN STORED PROCEDURES

        // GetLowStockProducts - Apeleaza SP GetLowStockProducts
        //
        // DEMONSTREAZA: SqlQuery pe Stored Procedure fara parametri
        //
        // STORED PROCEDURE (din scriptul SQL):
        // CREATE PROCEDURE GetLowStockProducts AS
        // SELECT p.ProductID, p.ProductName, p.Price,
        //        i.StockQuantity, i.MinimumStock, c.CategoryName,
        //        (i.MinimumStock - i.StockQuantity) AS StockNeeded
        // FROM Products p
        // INNER JOIN Inventory i ON p.ProductID = i.ProductID
        // INNER JOIN Categories c ON p.CategoryID = c.CategoryID
        // WHERE i.StockQuantity < i.MinimumStock AND p.IsActive = 1
        // ORDER BY StockNeeded DESC
        //
        // MAPARE:
        // Coloanele din SP sunt mapate automat la proprietatile din LowStockProductDTO
        // Numele trebuie sa coincida!
        public List<LowStockProductDTO> GetLowStockProducts()
        {
            using (var context = GetContext())
            {
                // SqlQuery<T> executa query si mapeaza la T
                // "EXEC" apeleaza stored procedure
                return context.Database
                    .SqlQuery<LowStockProductDTO>("EXEC GetLowStockProducts")
                    .ToList();
            }
        }

        // GetSalesStatistics - Apeleaza SP GetSalesStatistics cu parametri
        //
        // DEMONSTREAZA: SqlQuery pe Stored Procedure CU parametri
        //
        // STORED PROCEDURE accepta @StartDate si @EndDate
        // Daca sunt NULL, SP foloseste valori default
        //
        // PARAMETRI SQL:
        // SqlParameter creeaza un parametru pentru query
        // Previne SQL Injection si gestioneaza tipurile de date
        public SalesStatisticsDTO GetSalesStatistics(DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var context = GetContext())
            {
                // CREARE PARAMETRI
                // DBNull.Value pentru parametri NULL
                var parameters = new[]
                {
                    new SqlParameter("@StartDate", (object)startDate ?? DBNull.Value),
                    new SqlParameter("@EndDate", (object)endDate ?? DBNull.Value)
                };

                // APELARE SP CU PARAMETRI
                var result = context.Database
                    .SqlQuery<SalesStatisticsDTO>(
                        "EXEC GetSalesStatistics @StartDate, @EndDate",
                        parameters)
                    .FirstOrDefault();

                // Returnam rezultat sau un DTO gol
                return result ?? new SalesStatisticsDTO();
            }
        }

        // GetPopularProducts - Apeleaza SP GetPopularProducts
        //
        // DEMONSTREAZA: SqlQuery cu parametru de tip INT
        public List<PopularProductDTO> GetPopularProducts(int topCount = 10)
        {
            using (var context = GetContext())
            {
                var param = new SqlParameter("@TopCount", topCount);

                return context.Database
                    .SqlQuery<PopularProductDTO>(
                        "EXEC GetPopularProducts @TopCount",
                        param)
                    .ToList();
            }
        }

        // RAPOARTE CU RAW SQL (Database.SqlQuery)

        // GetOrdersByPeriod - Comenzi grupate pe perioada
        //
        // DEMONSTREAZA: SqlQuery cu SQL complex scris manual
        //
        // GROUPING:
        // - day: CONVERT(DATE, OrderDate)
        // - week: DATEPART(YEAR, OrderDate) + '-W' + DATEPART(WEEK, OrderDate)
        // - month: FORMAT(OrderDate, 'yyyy-MM')
        // - year: YEAR(OrderDate)
        //
        // SQL DINAMIC:
        // Construim SQL-ul in functie de parametrul groupBy
        // DAR parametrii pentru date sunt tot SqlParameter (sigur!)
        public List<PeriodReportDTO> GetOrdersByPeriod(string groupBy, DateTime startDate, DateTime endDate)
        {
            // DETERMINAM EXPRESIA DE GRUPARE
            string periodExpression;
            switch (groupBy.ToLower())
            {
                case "day":
                    periodExpression = "CONVERT(VARCHAR(10), o.OrderDate, 120)";
                    break;
                case "week":
                    periodExpression = "CONVERT(VARCHAR(4), YEAR(o.OrderDate)) + '-W' + " +
                                      "RIGHT('0' + CONVERT(VARCHAR(2), DATEPART(WEEK, o.OrderDate)), 2)";
                    break;
                case "month":
                    periodExpression = "CONVERT(VARCHAR(7), o.OrderDate, 120)";
                    break;
                case "year":
                    periodExpression = "CONVERT(VARCHAR(4), YEAR(o.OrderDate))";
                    break;
                default:
                    periodExpression = "CONVERT(VARCHAR(10), o.OrderDate, 120)";
                    break;
            }

            // CONSTRUIM SQL-UL
            // NOTA: periodExpression NU contine date de la user, e sigur
            // Datele de la user (@StartDate, @EndDate) sunt parametrizate
            string sql = $@"
                SELECT
                    {periodExpression} AS Period,
                    COUNT(*) AS OrderCount,
                    ISNULL(SUM(o.TotalAmount), 0) AS Revenue
                FROM Orders o
                WHERE o.OrderDate >= @StartDate
                  AND o.OrderDate <= @EndDate
                  AND o.OrderStatus != 'Cancelled'
                GROUP BY {periodExpression}
                ORDER BY Period";

            using (var context = GetContext())
            {
                var parameters = new[]
                {
                    new SqlParameter("@StartDate", startDate),
                    new SqlParameter("@EndDate", endDate)
                };

                return context.Database
                    .SqlQuery<PeriodReportDTO>(sql, parameters)
                    .ToList();
            }
        }

        // GetCategorySales - Vanzari per categorie
        //
        // DEMONSTREAZA: SqlQuery cu multiple JOINs si agregari
        //
        // SQL:
        // SELECT c.CategoryName,
        //        COUNT(DISTINCT p.ProductID) AS ProductCount,
        //        ISNULL(SUM(od.Quantity), 0) AS TotalSold,
        //        ISNULL(SUM(od.Subtotal), 0) AS TotalRevenue
        // FROM Categories c
        // LEFT JOIN Products p ON c.CategoryID = p.CategoryID
        // LEFT JOIN OrderDetails od ON p.ProductID = od.ProductID
        // GROUP BY c.CategoryID, c.CategoryName
        public List<CategorySalesDTO> GetCategorySales(DateTime? startDate = null, DateTime? endDate = null)
        {
            string dateFilter = "";
            var parameters = new List<SqlParameter>();

            // CONSTRUIM FILTRUL DE DATE (optional)
            if (startDate.HasValue && endDate.HasValue)
            {
                dateFilter = @"INNER JOIN Orders o ON od.OrderID = o.OrderID
                              WHERE o.OrderDate >= @StartDate AND o.OrderDate <= @EndDate
                                AND o.OrderStatus != 'Cancelled'";
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
                parameters.Add(new SqlParameter("@EndDate", endDate.Value));
            }

            string sql = $@"
                SELECT
                    c.CategoryName,
                    COUNT(DISTINCT p.ProductID) AS ProductCount,
                    ISNULL(SUM(od.Quantity), 0) AS TotalSold,
                    ISNULL(SUM(od.Subtotal), 0) AS TotalRevenue
                FROM Categories c
                LEFT JOIN Products p ON c.CategoryID = p.CategoryID AND p.IsActive = 1
                LEFT JOIN OrderDetails od ON p.ProductID = od.ProductID
                {dateFilter}
                GROUP BY c.CategoryID, c.CategoryName
                ORDER BY TotalRevenue DESC";

            using (var context = GetContext())
            {
                return context.Database
                    .SqlQuery<CategorySalesDTO>(sql, parameters.ToArray())
                    .ToList();
            }
        }

        // GetTopCustomers - Top clienti dupa cheltuieli
        //
        // DEMONSTREAZA: SqlQuery cu TOP, JOINs, agregari
        public List<CustomerReportDTO> GetTopCustomers(int topCount = 10)
        {
            string sql = @"
                SELECT TOP (@TopCount)
                    u.UserID AS CustomerID,
                    ISNULL(u.FirstName + ' ' + u.LastName, u.Username) AS CustomerName,
                    u.Email,
                    COUNT(o.OrderID) AS OrderCount,
                    ISNULL(SUM(o.TotalAmount), 0) AS TotalSpent,
                    MAX(o.OrderDate) AS LastOrderDate
                FROM Users u
                LEFT JOIN Orders o ON u.UserID = o.CustomerID AND o.OrderStatus != 'Cancelled'
                WHERE u.UserRole = 'Customer' AND u.IsActive = 1
                GROUP BY u.UserID, u.Username, u.FirstName, u.LastName, u.Email
                ORDER BY TotalSpent DESC";

            using (var context = GetContext())
            {
                var param = new SqlParameter("@TopCount", topCount);
                return context.Database
                    .SqlQuery<CustomerReportDTO>(sql, param)
                    .ToList();
            }
        }

        // GetInventoryValueByCategory - Valoarea inventarului
        //
        // DEMONSTREAZA: SqlQuery cu calcul agregat (StockQuantity * Price)
        public List<InventoryValueDTO> GetInventoryValueByCategory()
        {
            string sql = @"
                SELECT
                    c.CategoryName,
                    COUNT(p.ProductID) AS ProductCount,
                    ISNULL(SUM(i.StockQuantity), 0) AS TotalStock,
                    ISNULL(SUM(i.StockQuantity * p.Price), 0) AS TotalValue
                FROM Categories c
                LEFT JOIN Products p ON c.CategoryID = p.CategoryID AND p.IsActive = 1
                LEFT JOIN Inventory i ON p.ProductID = i.ProductID
                GROUP BY c.CategoryID, c.CategoryName
                ORDER BY TotalValue DESC";

            using (var context = GetContext())
            {
                return context.Database
                    .SqlQuery<InventoryValueDTO>(sql)
                    .ToList();
            }
        }

        // GetDailyRevenue - Venitul zilnic
        //
        // DEMONSTREAZA: SqlQuery cu DATEADD pentru ultimele N zile
        public List<DailyRevenueDTO> GetDailyRevenue(int days = 30)
        {
            string sql = @"
                SELECT
                    CONVERT(DATE, o.OrderDate) AS Date,
                    COUNT(*) AS OrderCount,
                    ISNULL(SUM(o.TotalAmount), 0) AS Revenue
                FROM Orders o
                WHERE o.OrderDate >= DATEADD(DAY, -@Days, GETDATE())
                  AND o.OrderStatus != 'Cancelled'
                GROUP BY CONVERT(DATE, o.OrderDate)
                ORDER BY Date";

            using (var context = GetContext())
            {
                var param = new SqlParameter("@Days", days);
                return context.Database
                    .SqlQuery<DailyRevenueDTO>(sql, param)
                    .ToList();
            }
        }

        // OPERATII CU ExecuteSqlCommand

        // CleanupOldData - Sterge date vechi
        //
        // DEMONSTREAZA: ExecuteSqlCommand pentru DELETE
        //
        // ExecuteSqlCommand:
        // - Executa SQL care modifica date
        // - Returneaza numarul de randuri afectate
        // - NU returneaza rezultate (pentru asta e SqlQuery)
        //
        // ATENTIE: Operatie periculoasa!
        // In productie, ar trebui backup inainte si confirmare
        public int CleanupOldData(int olderThanDays)
        {
            if (olderThanDays < 30)
                return 0;  // Safety: minim 30 zile

            using (var context = GetContext())
            {
                int totalDeleted = 0;
                var param = new SqlParameter("@Days", olderThanDays);

                // STERGERE TICKET-URI CLOSED VECHI
                // Mesajele se sterg automat prin CASCADE
                string deleteTickets = @"
                    DELETE FROM SupportTickets
                    WHERE Status = 'Closed'
                      AND CreatedDate < DATEADD(DAY, -@Days, GETDATE())";

                totalDeleted += context.Database.ExecuteSqlCommand(
                    deleteTickets,
                    new SqlParameter("@Days", olderThanDays));

                // NOTA: Comenzile de obicei NU se sterg (audit trail)
                // Dar pentru demo, stergem comenzile Cancelled vechi

                return totalDeleted;
            }
        }

        // UpdateAllPrices - Actualizeaza preturile in masa
        //
        // DEMONSTREAZA: ExecuteSqlCommand pentru UPDATE
        //
        // percentageChange:
        // - 10 = +10% (pret * 1.10)
        // - -5 = -5% (pret * 0.95)
        //
        // FORMULA: Price = Price * (1 + percentageChange/100)
        public int UpdateAllPrices(int? categoryId, decimal percentageChange)
        {
            // VALIDARE - limitam modificarea la +-50%
            if (percentageChange < -50 || percentageChange > 50)
                return 0;

            using (var context = GetContext())
            {
                string sql;
                SqlParameter[] parameters;

                // CALCUL MULTIPLIER
                decimal multiplier = 1 + (percentageChange / 100);

                if (categoryId.HasValue)
                {
                    // UPDATE DOAR PENTRU O CATEGORIE
                    sql = @"UPDATE Products
                           SET Price = ROUND(Price * @Multiplier, 2)
                           WHERE CategoryID = @CategoryID AND IsActive = 1";
                    parameters = new[]
                    {
                        new SqlParameter("@Multiplier", multiplier),
                        new SqlParameter("@CategoryID", categoryId.Value)
                    };
                }
                else
                {
                    // UPDATE PENTRU TOATE PRODUSELE
                    sql = @"UPDATE Products
                           SET Price = ROUND(Price * @Multiplier, 2)
                           WHERE IsActive = 1";
                    parameters = new[]
                    {
                        new SqlParameter("@Multiplier", multiplier)
                    };
                }

                return context.Database.ExecuteSqlCommand(sql, parameters);
            }
        }

        // RecalculateTotals - Recalculeaza totalurile comenzilor
        //
        // DEMONSTREAZA: ExecuteSqlCommand cu subquery
        //
        // FOLOSIRE:
        // - Dupa import de date
        // - Dupa corectii manuale in OrderDetails
        // - Pentru verificare integritate
        public int RecalculateTotals()
        {
            string sql = @"
                UPDATE o
                SET o.TotalAmount = ISNULL(
                    (SELECT SUM(od.Subtotal)
                     FROM OrderDetails od
                     WHERE od.OrderID = o.OrderID), 0)
                FROM Orders o";

            using (var context = GetContext())
            {
                return context.Database.ExecuteSqlCommand(sql);
            }
        }

        // STATISTICI GENERALE

        // GetDashboardStats - Toate statisticile pentru dashboard
        //
        // DEMONSTREAZA: Multiple query-uri agregate
        //
        // NOTA: In productie, pentru performanta, am putea face
        // un singur query complex sau un stored procedure
        public DashboardStatsDTO GetDashboardStats()
        {
            using (var context = GetContext())
            {
                var stats = new DashboardStatsDTO();

                // TOTAL COMENZI (non-cancelled)
                stats.TotalOrders = context.Orders
                    .Count(o => o.OrderStatus != "Cancelled");

                // TOTAL VENIT
                stats.TotalRevenue = context.Orders
                    .Where(o => o.OrderStatus != "Cancelled")
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;

                // TOTAL PRODUSE ACTIVE
                stats.TotalProducts = context.Products
                    .Count(p => p.IsActive);

                // TOTAL CLIENTI ACTIVI
                stats.TotalCustomers = context.Users
                    .Count(u => u.UserRole == "Customer" && u.IsActive);

                // COMENZI PENDING
                stats.PendingOrders = context.Orders
                    .Count(o => o.OrderStatus == "Pending");

                // PRODUSE LOW STOCK
                stats.LowStockCount = context.Inventories
                    .Count(i => i.StockQuantity < i.MinimumStock && i.Product.IsActive);

                // PRODUSE OUT OF STOCK
                stats.OutOfStockCount = context.Inventories
                    .Count(i => i.StockQuantity == 0 && i.Product.IsActive);

                // TICKET-URI DESCHISE
                stats.OpenTickets = context.SupportTickets
                    .Count(t => t.Status == "Open" || t.Status == "InProgress");

                // STATISTICI ULTIMELE 30 ZILE
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);

                stats.NewCustomersLast30Days = context.Users
                    .Count(u => u.UserRole == "Customer" &&
                               u.CreatedDate >= thirtyDaysAgo);

                stats.OrdersLast30Days = context.Orders
                    .Count(o => o.OrderDate >= thirtyDaysAgo &&
                               o.OrderStatus != "Cancelled");

                stats.RevenueLast30Days = context.Orders
                    .Where(o => o.OrderDate >= thirtyDaysAgo &&
                               o.OrderStatus != "Cancelled")
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;

                return stats;
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
                    // Elibereaza resurse managed
                }

                _disposed = true;
            }
        }
    }
}
