-- This script creates SYNONYMs in the dbo schema that point to objects in the purojit2_idmcbp schema.
-- Non-destructive: it only creates synonyms if the dbo object doesn't already exist and the source object exists.
-- Run this on the target database where the objects currently live under schema purojit2_idmcbp.
-- Example: CREATE SYNONYM dbo.Users FOR purojit2_idmcbp.Users;

SET NOCOUNT ON;

DECLARE @sql NVARCHAR(MAX) = N'';

;WITH Objects AS (
    SELECT s.name AS SchemaName, o.name AS ObjectName, o.type
    FROM sys.objects o
    JOIN sys.schemas s ON o.schema_id = s.schema_id
    WHERE s.name = 'purojit2_idmcbp'
)
SELECT @sql = STRING_AGG('IF OBJECT_ID(N''dbo.' + QUOTENAME(ObjectName) + ''') IS NULL AND OBJECT_ID(N''purojit2_idmcbp.' + QUOTENAME(ObjectName) + ''') IS NOT NULL\nBEGIN\n    PRINT ''Creating synonym dbo.' + ObjectName + ' -> purojit2_idmcbp.' + ObjectName + '''\n    EXEC(N''CREATE SYNONYM dbo.' + ObjectName + ' FOR purojit2_idmcbp.' + ObjectName + ''')\nEND\n', CHAR(13) + CHAR(10))
FROM Objects;

-- In case STRING_AGG returns NULL (no objects), handle gracefully
IF @sql IS NULL
BEGIN
    PRINT 'No objects found in schema purojit2_idmcbp. Nothing to do.';
END
ELSE
BEGIN
    PRINT '--- Generated synonym creation script ---';
    PRINT @sql;
    EXEC sp_executesql @sql;
    PRINT '--- Completed creating synonyms ---';
END

-- Notes:
-- 1) This script will create synonyms for tables, views, stored procedures, functions, etc. found under the pureojit2_idmcbp schema.
-- 2) Synonyms are non-destructive and can be dropped later using: DROP SYNONYM dbo.ObjectName;
-- 3) You must run this as a user with CREATE SYNONYM permission in the target database.
-- 4) Review the generated PRINT output before running in production.

GO
