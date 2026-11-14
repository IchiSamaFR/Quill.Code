-- SQL Server on a fictitious catalog: completion, hover and syntax checking, no execution.
-- Identifiers in [brackets], @name parameters, first rows through TOP, batches separated by GO.
-- Try Ctrl+Space after "c.", after FROM, or type @ for the @MaxRows parameter.

SELECT TOP (@MaxRows) c.Name, c.City, c.[Group]
FROM dbo.Customers c
WHERE c.City = 'Lyon'
ORDER BY c.Name;
GO

EXEC dbo.GetCustomerOrders @MaxRows;
