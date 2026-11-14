-- PostgreSQL on a fictitious catalog: completion and hover, no execution.
-- Identifiers in "quotes" (lower case otherwise), @name parameters, LIMIT or FETCH FIRST, :: casts.
-- Try Ctrl+Space after "c.", after FROM, or type F right after a table name.

SELECT c.name, c.city, c."group", o.order_date::date
FROM customers c
JOIN orders o ON o.customer_id = c.id
WHERE c.city ILIKE 'lyon'
ORDER BY c.name
LIMIT 10;

CALL get_customer_orders(@MaxRows);
