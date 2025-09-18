-- Check if Quantity column exists and add it if it doesn't
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Requests') AND name = 'Quantity')
BEGIN
    ALTER TABLE dbo.Requests ADD Quantity int NOT NULL DEFAULT 1
END