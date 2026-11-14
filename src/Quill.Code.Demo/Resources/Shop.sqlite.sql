-- Demo database: replayed at each startup.
DROP TABLE IF EXISTS OrderLines;
DROP TABLE IF EXISTS Orders;
DROP TABLE IF EXISTS Products;
DROP TABLE IF EXISTS Customers;

CREATE TABLE Customers (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    City TEXT NOT NULL,
    Email TEXT,
    "Group" TEXT NOT NULL
);

CREATE TABLE Products (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL,
    Price REAL NOT NULL
);

CREATE TABLE Orders (
    Id INTEGER PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers (Id),
    OrderDate TEXT NOT NULL,
    Status TEXT NOT NULL
);

CREATE TABLE OrderLines (
    Id INTEGER PRIMARY KEY,
    OrderId INTEGER NOT NULL REFERENCES Orders (Id),
    ProductId INTEGER NOT NULL REFERENCES Products (Id),
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL
);

INSERT INTO Customers (Id, Name, City, Email, "Group") VALUES
    (1, 'Alice Martin', 'Lyon', 'alice.martin@example.com', 'Individual'),
    (2, 'Bruno Lefèvre', 'Marseille', 'bruno.lefevre@example.com', 'Business'),
    (3, 'Chloé Dubois', 'Lyon', NULL, 'Individual'),
    (4, 'David Moreau', 'Nantes', 'david.moreau@example.com', 'Business'),
    (5, 'Emma Laurent', 'Paris', 'emma.laurent@example.com', 'Individual'),
    (6, 'Farid Benali', 'Marseille', 'farid.benali@example.com', 'Individual'),
    (7, 'Gaëlle Roux', 'Paris', NULL, 'Business'),
    (8, 'Hugo Petit', 'Bordeaux', 'hugo.petit@example.com', 'Individual');

INSERT INTO Products (Id, Name, Category, Price) VALUES
    (1, 'Mechanical keyboard', 'Computers', 89.90),
    (2, 'Wireless mouse', 'Computers', 34.50),
    (3, '27-inch monitor', 'Computers', 249.00),
    (4, 'Desk lamp', 'Home', 42.00),
    (5, 'A5 notebook', 'Stationery', 6.90),
    (6, 'Fountain pen', 'Stationery', 24.00),
    (7, 'Headphones', 'Audio', 129.00),
    (8, 'Portable speaker', 'Audio', 59.90);

INSERT INTO Orders (Id, CustomerId, OrderDate, Status) VALUES
    (1, 1, '2026-01-12', 'Delivered'),
    (2, 2, '2026-01-20', 'Delivered'),
    (3, 1, '2026-02-03', 'Delivered'),
    (4, 4, '2026-02-18', 'Cancelled'),
    (5, 5, '2026-03-02', 'Delivered'),
    (6, 6, '2026-03-15', 'Shipped'),
    (7, 2, '2026-03-28', 'Delivered'),
    (8, 7, '2026-04-05', 'Shipped'),
    (9, 8, '2026-04-11', 'Processing'),
    (10, 5, '2026-04-19', 'Processing'),
    (11, 3, '2026-04-22', 'Shipped'),
    (12, 1, '2026-04-30', 'Processing');

INSERT INTO OrderLines (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES
    (1, 1, 1, 1, 89.90), (2, 1, 2, 1, 34.50),
    (3, 2, 3, 2, 249.00), (4, 2, 4, 2, 42.00),
    (5, 3, 5, 5, 6.90), (6, 3, 6, 1, 24.00),
    (7, 4, 7, 1, 129.00),
    (8, 5, 8, 1, 59.90), (9, 5, 5, 3, 6.90),
    (10, 6, 2, 2, 34.50),
    (11, 7, 1, 3, 89.90), (12, 7, 3, 1, 249.00),
    (13, 8, 7, 2, 129.00), (14, 8, 4, 1, 42.00),
    (15, 9, 6, 2, 24.00),
    (16, 10, 3, 1, 249.00), (17, 10, 8, 1, 59.90),
    (18, 11, 5, 10, 6.90),
    (19, 12, 1, 1, 89.90), (20, 12, 7, 1, 129.00);
