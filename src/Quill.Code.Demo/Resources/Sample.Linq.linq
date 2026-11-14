// LINQ (C#) on the SQLite demo database: Db is a DbContext generated from its structure.
// F5 runs; only .Dump("Title") calls display data, one tab per call.
// The first completion takes a few seconds (Roslyn startup).

var byCity = Db.Customers
    .GroupBy(c => c.City)
    .Select(g => new { City = g.Key, Customers = g.Count() })
    .OrderByDescending(x => x.Customers);
byCity.Dump("Customers by city");

Db.Products
    .Where(p => p.Price > 50)
    .OrderBy(p => p.Name)
    .Dump("Products over 50");

Log.Information($"{Db.Orders.Count()} orders in the database.");
