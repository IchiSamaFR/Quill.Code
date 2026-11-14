-- MySQL on a fictitious catalog: completion and hover, no execution.
-- Identifiers in `backticks`, @name parameters, first rows through LIMIT, # comments.
-- Try Ctrl+Space after "c.", after FROM, or type L right after a table name.

SELECT c.name, c.city, c.`group`
FROM customers c
WHERE c.city = 'Lyon'  # MySQL comment
ORDER BY c.name
LIMIT 10;

CALL get_customer_orders(@MaxRows);
