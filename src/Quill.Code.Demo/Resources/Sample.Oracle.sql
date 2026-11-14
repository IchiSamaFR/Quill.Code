-- Oracle on a fictitious catalog: completion and hover, no execution.
-- Identifiers in "QUOTES" (upper case otherwise), :name parameters, first rows through FETCH FIRST.
-- Try Ctrl+Space after "c.", type F right after a table name, or ":" then M for :MaxRows.

SELECT c.NAME, c.CITY, c."GROUP", NVL(c.EMAIL, 'unknown') AS EMAIL
FROM SHOP.CUSTOMERS c
WHERE c.CITY = 'Lyon'
ORDER BY c.NAME
FETCH FIRST :MaxRows ROWS ONLY;

CALL SHOP.GET_CUSTOMER_ORDERS(:MaxRows);
