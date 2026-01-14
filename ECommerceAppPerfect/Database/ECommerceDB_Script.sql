-- Scriptul SQL pentru crearea bazei de date E-Commerce
--
-- ACEST FISIER este PRIMUL PAS in abordarea DB First
-- In DB First, baza de date se creeaza INAINTE de cod
-- Apoi Entity Framework genereaza clasele automat din structura bazei de date
--
-- CE CONTINE ACEST SCRIPT?
-- 1. Crearea bazei de date ECommerceDB
-- 2. Tabelele principale (Users, Products, Categories, etc.)
-- 3. Relatiile intre tabele (Foreign Keys)
-- 4. Indexuri pentru performanta
-- 5. Stored Procedures pentru rapoarte
-- 6. Date de test pentru a putea testa aplicatia imediat
--
-- RELATIILE DIN BAZA DE DATE (conform cerintelor cursului):
-- One-to-One: Product <-> Inventory (fiecare produs are exact un inventory)
-- One-to-Many: User -> Products (un owner are multe produse)
-- One-to-Many: Category -> Products (o categorie are multe produse)
-- One-to-Many: Order -> OrderDetails (o comanda are multe linii)
-- Many-to-Many: Products <-> Tags (prin tabelul ProductTags)
--
-- CUM SE FOLOSESTE ACEST SCRIPT?
-- 1. Deschide SQL Server Management Studio (SSMS)
-- 2. Conecteaza-te la serverul SQL local (. sau localhost sau .\SQLEXPRESS)
-- 3. Click pe New Query
-- 4. Copy-paste continutul acestui fisier
-- 5. Click Execute (sau apasa F5)
-- 6. Verifica ca baza de date s-a creat fara erori

USE master;
GO

-- Stergem baza de date daca exista deja
-- Asta e util pentru a putea rula scriptul de mai multe ori fara erori
-- ALTER DATABASE ... SET SINGLE_USER forteaza deconectarea tuturor utilizatorilor
-- WITH ROLLBACK IMMEDIATE anuleaza toate tranzactiile active
--
-- DE CE FACEM ASTA?
-- Daca baza de date exista si are conexiuni active, DROP DATABASE ar esua
-- Setand SINGLE_USER, fortam inchiderea tuturor conexiunilor
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ECommerceDB')
BEGIN
    ALTER DATABASE ECommerceDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ECommerceDB;
END
GO

-- Cream baza de date noua
-- CREATE DATABASE creeaza o baza de date goala cu setarile default
-- Numele bazei de date va fi folosit in connection string
CREATE DATABASE ECommerceDB;
GO

-- Selectam baza de date pentru a lucra in ea
-- USE schimba contextul - toate comenzile urmatoare se executa in ECommerceDB
USE ECommerceDB;
GO

-- TABELUL USERS
-- Acest tabel stocheaza toti utilizatorii aplicatiei
--
-- EXISTA 3 TIPURI DE UTILIZATORI (definite prin coloana UserRole):
-- 1. StoreOwner - Proprietarul magazinului
--    Ce poate face: gestioneaza produse, comenzi, inventory, settings
--    Este cel care administreaza tot magazinul
--
-- 2. Customer - Clientul obisnuit
--    Ce poate face: navigheaza produse, cumpara, lasa review-uri, deschide tickets
--    Este utilizatorul final care cumpara produse
--
-- 3. CustomerService - Agentul de suport
--    Ce poate face: gestioneaza tickets, raspunde la intrebari, ajuta clientii
--    Este intermediarul intre magazin si clienti
--
-- COLOANELE EXPLICATE:
-- UserID: Cheia primara, se incrementeaza automat (IDENTITY)
-- Username: Numele unic de utilizator pentru login
-- Email: Email-ul unic (folosit si pentru comunicare)
-- HashedPassword: Parola criptata cu SHA-256 (NU stocam parole in clar!)
-- UserRole: Tipul utilizatorului (constrans la cele 3 valori permise)
-- FirstName, LastName: Numele real pentru afisare
-- PhoneNumber: Pentru contact si livrare
-- Address: Adresa pentru livrare
-- CreatedDate: Cand s-a creat contul (default: momentul curent)
-- IsActive: Flag pentru soft delete (nu stergem utilizatori, ii dezactivam)
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    HashedPassword NVARCHAR(255) NOT NULL,
    UserRole NVARCHAR(20) NOT NULL CHECK (UserRole IN ('StoreOwner', 'Customer', 'CustomerService')),
    FirstName NVARCHAR(50) NULL,
    LastName NVARCHAR(50) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Address NVARCHAR(500) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);

-- TABELUL CATEGORIES
-- Acest tabel stocheaza categoriile de produse
--
-- DE CE AVEM NEVOIE DE CATEGORII?
-- Pentru a organiza produsele in grupuri logice
-- Clientii pot filtra produsele dupa categorie
-- Ajuta la navigare si la gasirea rapida a produselor
--
-- COLOANELE EXPLICATE:
-- CategoryID: Cheia primara, auto-increment
-- CategoryName: Numele categoriei (trebuie sa fie unic)
-- Description: Descriere optionala a categoriei
-- IconCode: Cod Unicode pentru iconita (ex: pentru Electronics)
--           Folosim Unicode pentru ca e simplu si functioneaza oriunde
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    IconCode NVARCHAR(10) NULL
);

-- TABELUL PRODUCTS
-- Acest tabel stocheaza toate produsele din magazin
-- Este tabelul central al aplicatiei - totul se invarte in jurul produselor
--
-- RELATII CU ALTE TABELE:
-- CategoryID -> Categories: Fiecare produs apartine unei categorii (Many-to-One)
-- StoreOwnerID -> Users: Fiecare produs e adaugat de un owner (Many-to-One)
-- Products -> Inventory: Fiecare produs are un singur inventory (One-to-One)
-- Products -> Reviews: Fiecare produs poate avea multe review-uri (One-to-Many)
-- Products <-> Tags: Relatia Many-to-Many prin ProductTags
--
-- COLOANELE EXPLICATE:
-- ProductID: Cheia primara, auto-increment
-- ProductName: Numele produsului (afisat clientilor)
-- Description: Descriere detaliata (poate fi foarte lunga - NVARCHAR(MAX))
-- Price: Pretul in RON, cu 2 zecimale (DECIMAL(18,2))
--        CHECK (Price >= 0) previne preturi negative
-- CategoryID: Foreign Key catre Categories
-- StoreOwnerID: Foreign Key catre Users (cel care a adaugat produsul)
-- ImageURL: Calea catre imaginea produsului
-- CreatedDate: Cand s-a adaugat produsul
-- IsActive: Flag pentru soft delete
CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL CHECK (Price >= 0),
    CategoryID INT NOT NULL,
    StoreOwnerID INT NOT NULL,
    ImageURL NVARCHAR(500) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT FK_Products_Users FOREIGN KEY (StoreOwnerID) REFERENCES Users(UserID)
);

-- TABELUL INVENTORY
-- Acest tabel gestioneaza stocul pentru fiecare produs
-- Este in relatie ONE-TO-ONE cu Products
--
-- DE CE TABEL SEPARAT SI NU COLOANE IN PRODUCTS?
-- 1. Separarea responsabilitatilor (Single Responsibility Principle)
-- 2. Inventory se updateaza frecvent, Products mai rar
-- 3. Putem avea logica separata pentru gestionarea stocului
-- 4. Demonstreaza relatia One-to-One ceruta in proiect
--
-- CUM REALIZAM ONE-TO-ONE?
-- Prin constrangerea UNIQUE pe ProductID
-- Asta garanteaza ca un produs poate avea MAXIM un rand in Inventory
--
-- COLOANELE EXPLICATE:
-- InventoryID: Cheia primara proprie (optional pentru One-to-One)
-- ProductID: Foreign Key UNIC catre Products
--            UNIQUE face relatia One-to-One (nu One-to-Many)
-- StockQuantity: Cate bucati avem in stoc
-- MinimumStock: Sub ce nivel trebuie sa recomandeze reaprovizionare
--               Folosit pentru alertele "Low Stock"
-- LastUpdated: Cand s-a modificat ultima oara stocul
--
-- ON DELETE CASCADE:
-- Daca stergem un produs, se sterge automat si inventory-ul lui
-- E logic - nu vrem inventory pentru produse care nu mai exista
CREATE TABLE Inventory (
    InventoryID INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT NOT NULL UNIQUE,
    StockQuantity INT NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0),
    MinimumStock INT NOT NULL DEFAULT 5 CHECK (MinimumStock >= 0),
    LastUpdated DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Inventory_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
);

-- TABELUL ORDERS
-- Acest tabel stocheaza comenzile plasate de clienti
-- Este header-ul comenzii - informatiile generale
--
-- WORKFLOW-UL UNEI COMENZI (OrderStatus):
-- 1. Pending - Comanda tocmai plasata, asteapta procesare
-- 2. Processing - Se pregateste comanda (impachetare)
-- 3. Shipped - Comanda a fost expediata
-- 4. Delivered - Comanda a ajuns la client
-- 5. Cancelled - Comanda anulata (de client sau magazin)
--
-- RELATII:
-- CustomerID -> Users: Cine a plasat comanda (Many-to-One)
-- Orders -> OrderDetails: Ce produse contine comanda (One-to-Many)
--
-- COLOANELE EXPLICATE:
-- OrderID: Cheia primara
-- CustomerID: Clientul care a plasat comanda
-- OrderDate: Cand s-a plasat comanda
-- TotalAmount: Suma totala (calculata din OrderDetails)
-- OrderStatus: Starea curenta a comenzii
-- ShippingAddress: Unde se livreaza
-- PaymentMethod: Cum a platit clientul
CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (TotalAmount >= 0),
    OrderStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending'
        CHECK (OrderStatus IN ('Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled')),
    ShippingAddress NVARCHAR(500) NULL,
    PaymentMethod NVARCHAR(50) NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (CustomerID) REFERENCES Users(UserID)
);

-- TABELUL ORDERDETAILS
-- Acest tabel stocheaza liniile (itemele) fiecarei comenzi
-- Este relatia ONE-TO-MANY cu Orders
--
-- DE CE TABEL SEPARAT?
-- O comanda poate avea MULTE produse diferite
-- Fiecare produs din comanda are cantitate si pret propriu
-- Asta e pattern-ul clasic Header-Detail (Orders = Header, OrderDetails = Detail)
--
-- EXEMPLU:
-- Order #1 contine:
-- - OrderDetail: 2x iPhone la 1299.99 = 2599.98
-- - OrderDetail: 1x Headphones la 349.99 = 349.99
-- Total Order: 2949.97
--
-- COLOANELE EXPLICATE:
-- OrderDetailID: Cheia primara
-- OrderID: Foreign Key catre comanda parinte
-- ProductID: Ce produs s-a comandat
-- Quantity: Cate bucati
-- UnitPrice: Pretul la momentul comenzii (se salveaza pentru ca pretul se poate schimba)
-- Subtotal: Coloana CALCULATA automat (Quantity * UnitPrice)
--           PERSISTED inseamna ca se salveaza fizic, nu se calculeaza de fiecare data
CREATE TABLE OrderDetails (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    Subtotal AS (Quantity * UnitPrice) PERSISTED,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- TABELUL SUPPORTTICKETS
-- Acest tabel stocheaza ticket-urile de suport deschise de clienti
-- Este sistemul de help desk al aplicatiei
--
-- WORKFLOW-UL UNUI TICKET (Status):
-- 1. Open - Ticket nou, asteapta sa fie preluat
-- 2. InProgress - Un agent lucreaza la el
-- 3. Resolved - Problema a fost rezolvata
-- 4. Closed - Ticket inchis definitiv
--
-- PRIORITATILE (Priority):
-- Low - Intrebari generale, nu e urgent
-- Medium - Probleme obisnuite
-- High - Probleme urgente (comenzi pierdute, etc.)
--
-- RELATII:
-- CustomerID -> Users: Cine a deschis ticket-ul
-- AssignedToID -> Users: Care agent se ocupa de ticket (poate fi NULL)
-- SupportTickets -> TicketMessages: Mesajele din conversatie (One-to-Many)
CREATE TABLE SupportTickets (
    TicketID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    Subject NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Open'
        CHECK (Status IN ('Open', 'InProgress', 'Resolved', 'Closed')),
    Priority NVARCHAR(20) NOT NULL DEFAULT 'Medium'
        CHECK (Priority IN ('Low', 'Medium', 'High')),
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    AssignedToID INT NULL,
    ResolvedDate DATETIME NULL,
    CONSTRAINT FK_SupportTickets_Customer FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    CONSTRAINT FK_SupportTickets_Assigned FOREIGN KEY (AssignedToID) REFERENCES Users(UserID)
);

-- TABELUL TICKETMESSAGES
-- Acest tabel stocheaza mesajele din conversatia unui ticket
-- Este ca un chat intre client si agent
--
-- DE CE AVEM NEVOIE?
-- Un ticket poate avea o conversatie cu mai multe mesaje
-- Clientul intreaba, agentul raspunde, clientul clarifica, etc.
-- Avem nevoie de istoric complet al conversatiei
--
-- COLOANELE EXPLICATE:
-- MessageID: Cheia primara
-- TicketID: La ce ticket apartine mesajul
-- UserID: Cine a trimis mesajul (client sau agent)
-- MessageText: Continutul mesajului
-- MessageDate: Cand s-a trimis
-- IsFromCustomer: Flag pentru a stii rapid daca e de la client sau agent
--                 True = mesaj de la client
--                 False = mesaj de la agent
CREATE TABLE TicketMessages (
    MessageID INT IDENTITY(1,1) PRIMARY KEY,
    TicketID INT NOT NULL,
    UserID INT NOT NULL,
    MessageText NVARCHAR(MAX) NOT NULL,
    MessageDate DATETIME NOT NULL DEFAULT GETDATE(),
    IsFromCustomer BIT NOT NULL,
    CONSTRAINT FK_TicketMessages_Tickets FOREIGN KEY (TicketID) REFERENCES SupportTickets(TicketID) ON DELETE CASCADE,
    CONSTRAINT FK_TicketMessages_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- TABELUL REVIEWS
-- Acest tabel stocheaza review-urile produselor
-- Clientii pot lasa pareri si note pentru produsele cumparate
--
-- REGULI:
-- Rating intre 1 si 5 stele
-- Un client poate lasa UN SINGUR review per produs (UNIQUE constraint)
-- IsVerifiedPurchase arata daca clientul a cumparat produsul
--
-- DE CE UNIQUE (ProductID, CustomerID)?
-- Pentru a preveni spam-ul - un client nu poate lasa 100 de review-uri
-- la acelasi produs pentru a-i creste sau scadea rating-ul
CREATE TABLE Reviews (
    ReviewID INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT NOT NULL,
    CustomerID INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(1000) NULL,
    ReviewDate DATETIME NOT NULL DEFAULT GETDATE(),
    IsVerifiedPurchase BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (CustomerID) REFERENCES Users(UserID),
    CONSTRAINT UQ_Reviews_ProductCustomer UNIQUE (ProductID, CustomerID)
);

-- TABELUL TAGS
-- Acest tabel stocheaza etichetele (tag-urile) pentru produse
-- Tag-urile sunt folosite pentru filtrare si marketing
--
-- EXEMPLE DE TAG-URI:
-- "Best Seller" - produse populare
-- "New Arrival" - produse noi
-- "On Sale" - produse la reducere
-- "Limited Stock" - stoc limitat
--
-- DE CE AVEM NEVOIE?
-- Un produs poate avea MULTE tag-uri (iPhone e si Best Seller si Premium)
-- Un tag poate fi pe MULTE produse (multe produse sunt Best Seller)
-- Asta e o relatie MANY-TO-MANY
--
-- TagColor este codul HEX pentru culoarea badge-ului in UI
-- Exemplu: #FFD700 = gold pentru Best Seller
CREATE TABLE Tags (
    TagID INT IDENTITY(1,1) PRIMARY KEY,
    TagName NVARCHAR(50) NOT NULL UNIQUE,
    TagColor NVARCHAR(7) NULL
);

-- TABELUL PRODUCTTAGS
-- Acest tabel este TABELUL DE LEGATURA pentru relatia Many-to-Many
-- Leaga Products de Tags
--
-- CUM FUNCTIONEAZA MANY-TO-MANY?
-- In bazele de date relationale, nu poti avea direct Many-to-Many
-- Trebuie un tabel intermediar care are:
-- - Foreign Key catre prima tabela (ProductID)
-- - Foreign Key catre a doua tabela (TagID)
-- - Cheia primara compusa din ambele (ProductID, TagID)
--
-- EXEMPLU:
-- Product #1 (iPhone) are Tag #1 (Best Seller) si Tag #5 (Premium)
-- ProductTags va avea:
-- (1, 1) - iPhone e Best Seller
-- (1, 5) - iPhone e Premium
--
-- Cheia primara compusa PRIMARY KEY (ProductID, TagID) garanteaza
-- ca nu putem adauga acelasi tag de doua ori la acelasi produs
CREATE TABLE ProductTags (
    ProductID INT NOT NULL,
    TagID INT NOT NULL,
    PRIMARY KEY (ProductID, TagID),
    CONSTRAINT FK_ProductTags_Products FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    CONSTRAINT FK_ProductTags_Tags FOREIGN KEY (TagID) REFERENCES Tags(TagID) ON DELETE CASCADE
);

-- TABELUL STORESETTINGS
-- Acest tabel stocheaza configurarile magazinului
-- Este un tabel key-value pentru setari diverse
--
-- DE CE KEY-VALUE SI NU COLOANE FIXE?
-- Pentru ca setarile se pot schimba - putem adauga setari noi
-- fara sa modificam structura bazei de date
--
-- TIPURI DE SETARI (SettingType):
-- Text - pentru string-uri (numele magazinului, email)
-- Color - pentru culori HEX (#2196F3)
-- Boolean - pentru da/nu (true/false)
-- Number - pentru numere (TVA, prag livrare gratuita)
--
-- EXEMPLE:
-- StoreName = "TechStore Premium" (Text)
-- PrimaryColor = "#2196F3" (Color)
-- TaxRate = "19" (Number - procent TVA)
-- AllowGuestCheckout = "true" (Boolean)
CREATE TABLE StoreSettings (
    SettingID INT IDENTITY(1,1) PRIMARY KEY,
    SettingKey NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(500) NULL,
    SettingType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    LastUpdated DATETIME NOT NULL DEFAULT GETDATE()
);

-- INDEXURI PENTRU PERFORMANTA
-- Indexurile sunt ca un cuprins la o carte - ajuta SQL Server
-- sa gaseasca datele mai repede fara sa scaneze tot tabelul
--
-- CAND FOLOSIM INDEXURI?
-- Pe coloanele care apar frecvent in:
-- - WHERE (filtrare): WHERE CategoryID = 5
-- - JOIN: INNER JOIN Categories ON Products.CategoryID = Categories.CategoryID
-- - ORDER BY: ORDER BY OrderDate DESC
--
-- ATENTIE: Indexurile incetinesc INSERT/UPDATE dar accelereaza SELECT
-- Trebuie gasite un echilibru - nu punem index pe TOATE coloanele
--
-- INDEXURILE CREATE MAI JOS:
-- IX_Products_CategoryID - pentru filtrare produse dupa categorie
-- IX_Products_StoreOwnerID - pentru a vedea produsele unui owner
-- IX_Products_IsActive - pentru a filtra doar produsele active
-- IX_Orders_CustomerID - pentru istoricul comenzilor unui client
-- IX_Orders_OrderStatus - pentru filtrare comenzi dupa status
-- etc.
CREATE INDEX IX_Products_CategoryID ON Products(CategoryID);
CREATE INDEX IX_Products_StoreOwnerID ON Products(StoreOwnerID);
CREATE INDEX IX_Products_IsActive ON Products(IsActive);
CREATE INDEX IX_Inventory_StockQuantity ON Inventory(StockQuantity);
CREATE INDEX IX_Orders_CustomerID ON Orders(CustomerID);
CREATE INDEX IX_Orders_OrderStatus ON Orders(OrderStatus);
CREATE INDEX IX_Orders_OrderDate ON Orders(OrderDate);
CREATE INDEX IX_OrderDetails_OrderID ON OrderDetails(OrderID);
CREATE INDEX IX_OrderDetails_ProductID ON OrderDetails(ProductID);
CREATE INDEX IX_SupportTickets_CustomerID ON SupportTickets(CustomerID);
CREATE INDEX IX_SupportTickets_Status ON SupportTickets(Status);
CREATE INDEX IX_SupportTickets_AssignedToID ON SupportTickets(AssignedToID);
CREATE INDEX IX_Reviews_ProductID ON Reviews(ProductID);
CREATE INDEX IX_Reviews_CustomerID ON Reviews(CustomerID);
GO

-- STORED PROCEDURE: GetLowStockProducts
-- Aceasta procedura returneaza produsele cu stoc sub minimul stabilit
--
-- DE CE STORED PROCEDURE SI NU QUERY SIMPLU?
-- 1. Performance - procedurile sunt precompilate
-- 2. Security - putem da permisiuni doar pe procedura, nu pe tabele
-- 3. Reusability - o scriem o data, o folosim oriunde
-- 4. Cerinta proiect - demonstreaza Function Imports in EDM
--
-- CE RETURNEAZA?
-- ProductID, ProductName, Price - informatii despre produs
-- StockQuantity, MinimumStock - stocul actual si minimul
-- CategoryName - categoria produsului
-- StockNeeded - cate bucati trebuie comandate (MinimumStock - StockQuantity)
--
-- LOGICA:
-- WHERE i.StockQuantity < i.MinimumStock - doar produsele cu stoc mic
-- AND p.IsActive = 1 - doar produsele active
-- ORDER BY StockNeeded DESC - cele mai urgente primele
CREATE PROCEDURE GetLowStockProducts
AS
BEGIN
    SELECT p.ProductID, p.ProductName, p.Price, i.StockQuantity, i.MinimumStock,
           c.CategoryName, (i.MinimumStock - i.StockQuantity) AS StockNeeded
    FROM Products p
    INNER JOIN Inventory i ON p.ProductID = i.ProductID
    INNER JOIN Categories c ON p.CategoryID = c.CategoryID
    WHERE i.StockQuantity < i.MinimumStock AND p.IsActive = 1
    ORDER BY (i.MinimumStock - i.StockQuantity) DESC;
END
GO

-- STORED PROCEDURE: GetSalesStatistics
-- Aceasta procedura returneaza statistici despre vanzari
--
-- PARAMETRI:
-- @StartDate - de cand sa calculeze (optional, default: acum 1 luna)
-- @EndDate - pana cand sa calculeze (optional, default: acum)
--
-- ISNULL(@StartDate, ...) inseamna:
-- Daca @StartDate e NULL, foloseste valoarea default
-- DATEADD(MONTH, -1, GETDATE()) = acum minus 1 luna
--
-- CE RETURNEAZA?
-- TotalOrders - cate comenzi s-au plasat
-- TotalRevenue - cat s-a incasat
-- AverageOrderValue - valoarea medie a unei comenzi
-- UniqueCustomers - cati clienti distincti au comandat
-- ProductsSold - cate produse diferite s-au vandut
--
-- WHERE o.OrderStatus NOT IN ('Cancelled')
-- Nu numaram comenzile anulate - alea nu sunt vanzari reale
CREATE PROCEDURE GetSalesStatistics
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL
AS
BEGIN
    SET @StartDate = ISNULL(@StartDate, DATEADD(MONTH, -1, GETDATE()));
    SET @EndDate = ISNULL(@EndDate, GETDATE());

    SELECT
        COUNT(DISTINCT o.OrderID) AS TotalOrders,
        SUM(o.TotalAmount) AS TotalRevenue,
        AVG(o.TotalAmount) AS AverageOrderValue,
        COUNT(DISTINCT o.CustomerID) AS UniqueCustomers,
        COUNT(DISTINCT od.ProductID) AS ProductsSold
    FROM Orders o
    LEFT JOIN OrderDetails od ON o.OrderID = od.OrderID
    WHERE o.OrderDate BETWEEN @StartDate AND @EndDate
        AND o.OrderStatus NOT IN ('Cancelled');
END
GO

-- STORED PROCEDURE: GetPopularProducts
-- Aceasta procedura returneaza cele mai populare produse
--
-- PARAMETRU:
-- @TopCount - cate produse sa returneze (default: 10)
--
-- TOP (@TopCount) limiteaza rezultatele la primele N
-- Similar cu LIMIT din MySQL
--
-- CE CALCULEAZA?
-- OrderCount - in cate comenzi a aparut produsul
-- TotalQuantitySold - cate bucati s-au vandut in total
-- TotalRevenue - cat a generat produsul (suma Subtotal-urilor)
-- AverageRating - media review-urilor
-- ReviewCount - cate review-uri are
--
-- GROUP BY este necesar pentru functiile de agregare (COUNT, SUM, AVG)
-- Grupeaza rezultatele pe produs
--
-- ORDER BY TotalQuantitySold DESC pune cele mai vandute primele
CREATE PROCEDURE GetPopularProducts
    @TopCount INT = 10
AS
BEGIN
    SELECT TOP (@TopCount)
        p.ProductID, p.ProductName, p.Price, c.CategoryName,
        COUNT(od.OrderDetailID) AS OrderCount,
        SUM(od.Quantity) AS TotalQuantitySold,
        SUM(od.Subtotal) AS TotalRevenue,
        AVG(CAST(r.Rating AS FLOAT)) AS AverageRating,
        COUNT(r.ReviewID) AS ReviewCount
    FROM Products p
    INNER JOIN Categories c ON p.CategoryID = c.CategoryID
    LEFT JOIN OrderDetails od ON p.ProductID = od.ProductID
    LEFT JOIN Reviews r ON p.ProductID = r.ProductID
    WHERE p.IsActive = 1
    GROUP BY p.ProductID, p.ProductName, p.Price, c.CategoryName
    ORDER BY TotalQuantitySold DESC;
END
GO

-- DATE DE TEST - UTILIZATORI
-- Acesti utilizatori sunt pentru a putea testa aplicatia imediat
-- dupa ce rulezi scriptul
--
-- PAROLA PENTRU TOTI: password123
-- Hash-ul SHA-256 al parolei "password123" este cel din HashedPassword
-- In aplicatie, cand utilizatorul introduce parola, o transformam in SHA-256
-- si o comparam cu cea din baza de date
--
-- UTILIZATORI CREATI:
-- admin (StoreOwner) - proprietarul magazinului
-- john_customer, jane_customer, mary_customer (Customer) - clienti de test
-- support1 (CustomerService) - agent de suport
INSERT INTO Users (Username, Email, HashedPassword, UserRole, FirstName, LastName, PhoneNumber, IsActive) VALUES
('admin', 'admin@ecommerce.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'StoreOwner', 'Admin', 'User', '0712345678', 1),
('john_customer', 'john@email.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Customer', 'John', 'Doe', '0723456789', 1),
('jane_customer', 'jane@email.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Customer', 'Jane', 'Smith', '0734567890', 1),
('support1', 'support@ecommerce.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'CustomerService', 'Support', 'Agent', '0745678901', 1),
('mary_customer', 'mary@email.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 'Customer', 'Mary', 'Jones', '0756789012', 1);

-- DATE DE TEST - CATEGORII
-- Categoriile principale ale magazinului
-- IconCode contine emoji-uri Unicode pentru afisare in UI
INSERT INTO Categories (CategoryName, Description, IconCode) VALUES
('Electronics', 'Electronic devices and gadgets', N'📱'),
('Clothing', 'Apparel and fashion items', N'👕'),
('Books', 'Physical and digital books', N'📚'),
('Home & Garden', 'Home decor and garden supplies', N'🏠'),
('Sports', 'Sports equipment and fitness gear', N'⚽');

-- DATE DE TEST - PRODUSE
-- Produse diverse din fiecare categorie
-- Toate sunt adaugate de admin (StoreOwnerID = 1)
-- Preturile sunt in RON
INSERT INTO Products (ProductName, Description, Price, CategoryID, StoreOwnerID, ImageURL, IsActive) VALUES
('iPhone 15 Pro', 'Latest Apple smartphone with A17 Pro chip', 1299.99, 1, 1, '/images/iphone15.jpg', 1),
('Samsung Galaxy S24', 'Flagship Android smartphone with AI', 999.99, 1, 1, '/images/galaxys24.jpg', 1),
('Sony WH-1000XM5', 'Noise canceling wireless headphones', 349.99, 1, 1, '/images/sonywh.jpg', 1),
('MacBook Pro 16"', 'Professional laptop with M3 Max chip', 2499.99, 1, 1, '/images/macbook.jpg', 1),
('Nike Air Max 270', 'Lifestyle sneakers with Max Air', 159.99, 2, 1, '/images/airmax.jpg', 1),
('Levi''s 501 Jeans', 'Classic straight fit denim jeans', 89.99, 2, 1, '/images/levis.jpg', 1),
('North Face Jacket', 'Waterproof winter jacket', 249.99, 2, 1, '/images/jacket.jpg', 1),
('Clean Code Book', 'Software craftsmanship handbook', 39.99, 3, 1, '/images/cleancode.jpg', 1),
('Design Patterns', 'Gang of Four design patterns book', 44.99, 3, 1, '/images/patterns.jpg', 1),
('Modern Desk Lamp', 'LED lamp with wireless charging', 79.99, 4, 1, '/images/lamp.jpg', 1),
('Indoor Plant Set', '3 low-maintenance plants', 45.99, 4, 1, '/images/plants.jpg', 1),
('Yoga Mat Premium', 'Extra thick non-slip mat', 34.99, 5, 1, '/images/yogamat.jpg', 1),
('Dumbbell Set 20kg', 'Adjustable weight system', 199.99, 5, 1, '/images/dumbbells.jpg', 1),
('Running Shoes', 'Lightweight marathon shoes', 129.99, 5, 1, '/images/running.jpg', 1);

-- DATE DE TEST - INVENTORY
-- Fiecare produs are un rand in Inventory
-- Relatia One-to-One cu Products
-- StockQuantity = cate bucati avem
-- MinimumStock = sub ce nivel ne avertizeaza
INSERT INTO Inventory (ProductID, StockQuantity, MinimumStock) VALUES
(1, 50, 10), (2, 75, 15), (3, 100, 20), (4, 30, 5),
(5, 200, 25), (6, 150, 30), (7, 80, 15), (8, 120, 20),
(9, 90, 15), (10, 60, 10), (11, 45, 10), (12, 90, 15),
(13, 35, 8), (14, 110, 20);

-- DATE DE TEST - TAGS
-- Tag-urile pentru produse
-- TagColor e codul HEX pentru badge-ul vizual
INSERT INTO Tags (TagName, TagColor) VALUES
('Best Seller', '#FFD700'),
('New Arrival', '#4CAF50'),
('On Sale', '#F44336'),
('Limited Stock', '#FF9800'),
('Premium', '#9C27B0'),
('Eco-Friendly', '#8BC34A');

-- DATE DE TEST - PRODUCTTAGS
-- Asocierile Many-to-Many intre Products si Tags
-- Formatul: (ProductID, TagID)
INSERT INTO ProductTags (ProductID, TagID) VALUES
(1, 1), (1, 5),
(2, 2), (2, 3),
(3, 1), (3, 5),
(4, 5),
(5, 1),
(8, 1), (8, 2),
(12, 6);

-- DATE DE TEST - ORDERS
-- Comenzi de test pentru a demonstra workflow-ul
-- DATEADD(DAY, -10, GETDATE()) = acum 10 zile in urma
-- Asta creaza comenzi cu date diferite pentru statistici realiste
INSERT INTO Orders (CustomerID, OrderDate, TotalAmount, OrderStatus, ShippingAddress, PaymentMethod) VALUES
(2, DATEADD(DAY, -10, GETDATE()), 1649.98, 'Delivered', '123 Main St, New York, NY', 'Credit Card'),
(2, DATEADD(DAY, -5, GETDATE()), 429.98, 'Delivered', '123 Main St, New York, NY', 'PayPal'),
(3, DATEADD(DAY, -2, GETDATE()), 1089.98, 'Processing', '456 Oak Ave, Los Angeles, CA', 'Credit Card'),
(5, DATEADD(DAY, -1, GETDATE()), 234.98, 'Pending', '789 Pine Rd, Chicago, IL', 'Credit Card'),
(2, GETDATE(), 349.99, 'Pending', '123 Main St, New York, NY', 'Credit Card');

-- DATE DE TEST - ORDERDETAILS
-- Liniile fiecarei comenzi
-- Fiecare comanda are unul sau mai multe produse
INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice) VALUES
(1, 1, 1, 1299.99), (1, 3, 1, 349.99),
(2, 5, 1, 159.99), (2, 6, 2, 89.99), (2, 12, 1, 34.99),
(3, 2, 1, 999.99), (3, 8, 2, 39.99),
(4, 10, 1, 79.99), (4, 11, 1, 45.99), (4, 8, 1, 39.99), (4, 9, 1, 44.99),
(5, 3, 1, 349.99);

-- DATE DE TEST - SUPPORTTICKETS
-- Ticket-uri de suport pentru a demonstra modulul de Customer Service
INSERT INTO SupportTickets (CustomerID, Subject, Description, Status, Priority, AssignedToID) VALUES
(2, 'Order Delivery Issue', 'Order #1 marked as delivered but not received', 'InProgress', 'High', 4),
(3, 'Product Return Request', 'Samsung has dead pixel, want to return', 'Open', 'Medium', NULL),
(5, 'Payment Question', 'Need clarification on charges', 'Resolved', 'Low', 4),
(2, 'Warranty Information', 'How to register iPhone warranty?', 'Open', 'Low', NULL);

-- DATE DE TEST - TICKETMESSAGES
-- Mesajele din conversatiile ticket-urilor
-- IsFromCustomer = 1 pentru mesaje de la client, 0 pentru mesaje de la agent
INSERT INTO TicketMessages (TicketID, UserID, MessageText, IsFromCustomer) VALUES
(1, 2, 'My package shows delivered but I did not receive it.', 1),
(1, 4, 'I apologize for the inconvenience. Let me track your package.', 0),
(1, 4, 'The package was delivered to your building reception. Can you check there?', 0),
(3, 5, 'Can you explain the tax calculation on my order?', 1),
(3, 4, 'Taxes are calculated based on your state rate of 8.5%. Let me break it down...', 0);

-- DATE DE TEST - REVIEWS
-- Review-urile produselor de la clienti
-- IsVerifiedPurchase = 1 daca clientul a cumparat produsul
INSERT INTO Reviews (ProductID, CustomerID, Rating, Comment, IsVerifiedPurchase) VALUES
(1, 2, 5, 'Best phone ever! Camera is incredible.', 1),
(3, 2, 5, 'Amazing noise cancellation. Very comfortable.', 1),
(5, 3, 4, 'Great sneakers but color slightly different from picture.', 0),
(8, 5, 5, 'Must-read for every developer!', 1),
(12, 3, 4, 'Good quality mat. Helpful alignment guides.', 0);

-- DATE DE TEST - STORESETTINGS
-- Configurarile magazinului
-- Acestea se pot modifica din aplicatie de catre StoreOwner
INSERT INTO StoreSettings (SettingKey, SettingValue, SettingType, Description) VALUES
('StoreName', 'TechStore Premium', 'Text', 'Name of the store'),
('StoreEmail', 'contact@techstore.com', 'Text', 'Contact email'),
('StorePhone', '1-800-TECH-STORE', 'Text', 'Contact phone number'),
('PrimaryColor', '#2196F3', 'Color', 'Primary brand color'),
('AccentColor', '#FF5722', 'Color', 'Accent color'),
('CurrencySymbol', 'RON', 'Text', 'Currency symbol'),
('TaxRate', '19', 'Number', 'VAT percentage'),
('MinimumOrderAmount', '50', 'Number', 'Minimum order amount'),
('FreeShippingThreshold', '200', 'Number', 'Free shipping above this amount'),
('AllowGuestCheckout', 'true', 'Boolean', 'Allow checkout without account');
GO

-- VERIFICARE FINALA
-- Aceste query-uri confirma ca totul s-a creat corect
-- Afiseaza numarul de randuri din fiecare tabel
SELECT 'Database created successfully!' AS Status;
SELECT 'Users' AS TableName, COUNT(*) AS Records FROM Users
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Inventory', COUNT(*) FROM Inventory
UNION ALL SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL SELECT 'ProductTags', COUNT(*) FROM ProductTags
UNION ALL SELECT 'Orders', COUNT(*) FROM Orders
UNION ALL SELECT 'OrderDetails', COUNT(*) FROM OrderDetails
UNION ALL SELECT 'SupportTickets', COUNT(*) FROM SupportTickets
UNION ALL SELECT 'TicketMessages', COUNT(*) FROM TicketMessages
UNION ALL SELECT 'Reviews', COUNT(*) FROM Reviews
UNION ALL SELECT 'StoreSettings', COUNT(*) FROM StoreSettings;

PRINT 'E-Commerce Database created successfully!';
PRINT '';
PRINT 'Demo Users (password: password123):';
PRINT '   - admin (StoreOwner)';
PRINT '   - john_customer (Customer)';
PRINT '   - jane_customer (Customer)';
PRINT '   - support1 (CustomerService)';
PRINT '   - mary_customer (Customer)';
GO
