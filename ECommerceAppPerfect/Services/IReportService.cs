using System;
using System.Collections.Generic;

namespace ECommerceAppPerfect.Services
{
    // INTERFATA IREPORTSERVICE - Contract pentru serviciul de rapoarte
    //
    // CE ESTE ACEST SERVICIU?
    // Un serviciu dedicat pentru rapoarte si statistici complexe
    // Demonstreaza utilizarea RAW SQL si STORED PROCEDURES in Entity Framework
    //
    // DE CE RAW SQL SI STORED PROCEDURES?
    //
    // CAND FOLOSIM RAW SQL?
    // 1. Query-uri complexe greu de exprimat in LINQ
    // 2. Optimizari specifice SQL (hints, CTEs, window functions)
    // 3. Compatibilitate cu cod SQL existent
    // 4. Rapoarte care necesita agregari complexe
    //
    // CAND FOLOSIM STORED PROCEDURES?
    // 1. PERFORMANTA: Planul de executie e pre-compilat
    // 2. SECURITATE: Putem da permisiuni doar pe procedura
    // 3. REUSABILITATE: Acelasi cod SQL apelat din mai multe locuri
    // 4. MENTENANTA: SQL-ul e in baza de date, nu in cod
    //
    // METODE DIN ENTITY FRAMEWORK PENTRU RAW SQL:
    //
    // 1. Database.SqlQuery<T>(sql, params)
    //    Executa SELECT si mapeaza rezultatele la tipul T
    //    Returneaza IEnumerable<T>
    //    Folosit pentru: citire date, rapoarte
    //
    // 2. Database.ExecuteSqlCommand(sql, params)
    //    Executa INSERT/UPDATE/DELETE
    //    Returneaza numarul de randuri afectate
    //    Folosit pentru: modificari in masa, operatii DDL
    //
    // STORED PROCEDURES DIN BAZA DE DATE:
    // - GetLowStockProducts: produsele cu stoc sub minim
    // - GetSalesStatistics: statistici de vanzari
    // - GetPopularProducts: produsele cele mai vandute
    //
    // CONCEPTE DIN CURS DEMONSTRATE (pag. 14-15):
    // - Database.SqlQuery() pentru query-uri SELECT
    // - Database.ExecuteSqlCommand() pentru INSERT/UPDATE/DELETE
    // - Apelarea Stored Procedures din EF
    // - Maparea rezultatelor la DTO-uri
    public interface IReportService
    {
        // RAPOARTE DIN STORED PROCEDURES

        // GetLowStockProducts - Produse cu stoc sub minim
        //
        // APELEAZA: Stored Procedure GetLowStockProducts
        //
        // RETURNEAZA: Lista de LowStockProductDTO cu:
        // - ProductID, ProductName, Price
        // - StockQuantity, MinimumStock
        // - CategoryName
        // - StockNeeded (cate bucati trebuie comandate)
        //
        // SQL IN STORED PROCEDURE:
        // SELECT p.ProductID, p.ProductName, p.Price,
        //        i.StockQuantity, i.MinimumStock, c.CategoryName,
        //        (i.MinimumStock - i.StockQuantity) AS StockNeeded
        // FROM Products p
        // INNER JOIN Inventory i ON p.ProductID = i.ProductID
        // INNER JOIN Categories c ON p.CategoryID = c.CategoryID
        // WHERE i.StockQuantity < i.MinimumStock AND p.IsActive = 1
        // ORDER BY StockNeeded DESC
        List<LowStockProductDTO> GetLowStockProducts();

        // GetSalesStatistics - Statistici de vanzari
        //
        // APELEAZA: Stored Procedure GetSalesStatistics
        //
        // PARAMETRI:
        // - startDate: data de inceput (optional, default: acum 1 luna)
        // - endDate: data de sfarsit (optional, default: acum)
        //
        // RETURNEAZA: SalesStatisticsDTO cu:
        // - TotalOrders: cate comenzi
        // - TotalRevenue: venit total
        // - AverageOrderValue: valoarea medie
        // - UniqueCustomers: cati clienti distincti
        // - ProductsSold: cate produse diferite
        SalesStatisticsDTO GetSalesStatistics(DateTime? startDate = null, DateTime? endDate = null);

        // GetPopularProducts - Cele mai populare produse
        //
        // APELEAZA: Stored Procedure GetPopularProducts
        //
        // PARAMETRU: topCount - cate produse sa returneze (default: 10)
        //
        // RETURNEAZA: Lista de PopularProductDTO cu:
        // - ProductID, ProductName, Price, CategoryName
        // - OrderCount: in cate comenzi a aparut
        // - TotalQuantitySold: cate bucati s-au vandut
        // - TotalRevenue: cat a generat produsul
        // - AverageRating: media review-urilor
        // - ReviewCount: cate review-uri are
        List<PopularProductDTO> GetPopularProducts(int topCount = 10);

        // RAPOARTE CU RAW SQL (Database.SqlQuery)

        // GetOrdersByPeriod - Comenzi grupate pe perioada
        //
        // DEMONSTREAZA: SqlQuery cu GROUP BY si agregari
        //
        // PARAMETRI:
        // - groupBy: "day", "week", "month", "year"
        // - startDate: de cand
        // - endDate: pana cand
        //
        // RETURNEAZA: Lista cu (Period, OrderCount, Revenue)
        List<PeriodReportDTO> GetOrdersByPeriod(string groupBy, DateTime startDate, DateTime endDate);

        // GetCategorySales - Vanzari per categorie
        //
        // DEMONSTREAZA: SqlQuery cu JOIN si GROUP BY
        //
        // RETURNEAZA: Lista cu (CategoryName, ProductCount, TotalSold, TotalRevenue)
        List<CategorySalesDTO> GetCategorySales(DateTime? startDate = null, DateTime? endDate = null);

        // GetCustomerReport - Top clienti
        //
        // DEMONSTREAZA: SqlQuery cu ORDER BY si TOP
        //
        // PARAMETRU: topCount - cati clienti (default: 10)
        //
        // RETURNEAZA: Lista cu (CustomerName, OrderCount, TotalSpent, LastOrderDate)
        List<CustomerReportDTO> GetTopCustomers(int topCount = 10);

        // GetInventoryValueByCategory - Valoarea inventarului per categorie
        //
        // DEMONSTREAZA: SqlQuery cu calcul agregat complex
        //
        // RETURNEAZA: Lista cu (CategoryName, ProductCount, TotalStock, TotalValue)
        List<InventoryValueDTO> GetInventoryValueByCategory();

        // GetDailyRevenue - Venitul zilnic pentru ultimele N zile
        //
        // PARAMETRU: days - cate zile in urma (default: 30)
        //
        // RETURNEAZA: Lista cu (Date, OrderCount, Revenue)
        List<DailyRevenueDTO> GetDailyRevenue(int days = 30);

        // OPERATII CU ExecuteSqlCommand

        // CleanupOldData - Sterge date vechi
        //
        // DEMONSTREAZA: ExecuteSqlCommand pentru DELETE in masa
        //
        // PARAMETRU: olderThanDays - sterge datele mai vechi de atatea zile
        //
        // OPERATII:
        // - Sterge ticket-urile Closed mai vechi de N zile
        // - Sterge comenzile Cancelled mai vechi de N zile
        //
        // RETURNEAZA: Numarul total de randuri sterse
        //
        // ATENTIE: Operatie periculoasa! Confirmare necesara in UI
        int CleanupOldData(int olderThanDays);

        // UpdateAllPrices - Actualizeaza preturile in masa
        //
        // DEMONSTREAZA: ExecuteSqlCommand pentru UPDATE in masa
        //
        // PARAMETRI:
        // - categoryId: categoria de actualizat (optional, null = toate)
        // - percentageChange: cat sa modifice (ex: 10 pentru +10%, -5 pentru -5%)
        //
        // RETURNEAZA: Numarul de produse actualizate
        //
        // SQL GENERAT:
        // UPDATE Products SET Price = Price * (1 + @percentage/100)
        // WHERE CategoryID = @categoryId
        int UpdateAllPrices(int? categoryId, decimal percentageChange);

        // RecalculateTotals - Recalculeaza totalurile comenzilor
        //
        // DEMONSTREAZA: ExecuteSqlCommand cu subquery
        //
        // OPERATIE: Actualizeaza TotalAmount din Orders
        //           bazat pe suma din OrderDetails
        //
        // SQL:
        // UPDATE o SET o.TotalAmount = (
        //     SELECT SUM(od.Subtotal) FROM OrderDetails od WHERE od.OrderID = o.OrderID
        // ) FROM Orders o
        //
        // RETURNEAZA: Numarul de comenzi actualizate
        //
        // FOLOSIRE: Corectare date dupa import sau erori
        int RecalculateTotals();

        // STATISTICI GENERALE

        // GetDashboardStats - Statistici pentru dashboard
        //
        // RETURNEAZA: Un obiect cu toate statisticile principale
        // - Total Orders, Total Revenue, Total Products
        // - Pending Orders, Low Stock Count
        // - Open Tickets, New Customers (ultimele 30 zile)
        DashboardStatsDTO GetDashboardStats();
    }

    // DTO-URI PENTRU RAPOARTE
    //
    // CE ESTE UN DTO?
    // Data Transfer Object - o clasa simpla pentru transfer de date
    // NU este o entitate EF, nu are navigari sau tracking
    //
    // DE CE DTO SI NU ENTITATI?
    // 1. Stored Procedures pot returna date care nu corespund 1:1 cu tabelele
    // 2. Rapoartele combina date din mai multe tabele
    // 3. DTO-urile sunt "flat" - nu au relatii complexe
    // 4. Performanta - nu se incarca relatii inutile
    //
    // CUM FUNCTIONEAZA CU SqlQuery<T>?
    // SqlQuery mapeaza coloanele din rezultat la proprietatile DTO-ului
    // Numele coloanelor trebuie sa coincida cu numele proprietatilor!
    // Sau folosim AS in SQL pentru a le potrivi

    // LowStockProductDTO - Pentru raportul de produse low stock
    //
    // MAPARE DE LA STORED PROCEDURE:
    // - ProductID -> ProductID
    // - ProductName -> ProductName
    // - Price -> Price
    // - StockQuantity -> StockQuantity
    // - MinimumStock -> MinimumStock
    // - CategoryName -> CategoryName
    // - StockNeeded -> StockNeeded (calculat in SP)
    public class LowStockProductDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int MinimumStock { get; set; }
        public string CategoryName { get; set; }
        public int StockNeeded { get; set; }
    }

    // SalesStatisticsDTO - Pentru raportul de statistici vanzari
    public class SalesStatisticsDTO
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int UniqueCustomers { get; set; }
        public int ProductsSold { get; set; }
    }

    // PopularProductDTO - Pentru raportul de produse populare
    public class PopularProductDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
        public int OrderCount { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    // PeriodReportDTO - Pentru raportul de comenzi pe perioada
    public class PeriodReportDTO
    {
        public string Period { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    // CategorySalesDTO - Pentru raportul de vanzari per categorie
    public class CategorySalesDTO
    {
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // CustomerReportDTO - Pentru raportul de top clienti
    public class CustomerReportDTO
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    // InventoryValueDTO - Pentru raportul de valoare inventar
    public class InventoryValueDTO
    {
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
        public int TotalStock { get; set; }
        public decimal TotalValue { get; set; }
    }

    // DailyRevenueDTO - Pentru raportul de venit zilnic
    public class DailyRevenueDTO
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    // DashboardStatsDTO - Pentru statisticile dashboard-ului
    public class DashboardStatsDTO
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int OpenTickets { get; set; }
        public int NewCustomersLast30Days { get; set; }
        public decimal RevenueLast30Days { get; set; }
        public int OrdersLast30Days { get; set; }
    }
}
