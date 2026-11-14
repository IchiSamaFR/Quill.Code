-- SQLite demo database (fake data), recreated at startup: F5 runs.
-- Ctrl+Space suggests tables, columns and SQLite words; hovering a name gives its type.
-- Each SELECT has its own result tab; select a statement to run only that one.

SELECT c.Name, c.City, c."Group"
FROM Customers c
ORDER BY c.Name
LIMIT 5;

SELECT c.City, COUNT(*) AS OrderCount, ROUND(SUM(l.Quantity * l.UnitPrice), 2) AS Total
FROM Orders o
JOIN Customers c ON c.Id = o.CustomerId
JOIN OrderLines l ON l.OrderId = o.Id
WHERE o.Status <> 'Cancelled'
GROUP BY c.City
ORDER BY Total DESC;
