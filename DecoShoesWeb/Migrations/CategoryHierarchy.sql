IF COL_LENGTH('Categories', 'ParentCategoryID') IS NULL
BEGIN
    ALTER TABLE Categories ADD ParentCategoryID int NULL;
END;

IF COL_LENGTH('Categories', 'Slug') IS NULL
BEGIN
    ALTER TABLE Categories ADD Slug nvarchar(450) NULL;
END;

IF COL_LENGTH('Categories', 'DisplayOrder') IS NULL
BEGIN
    ALTER TABLE Categories ADD DisplayOrder int NOT NULL CONSTRAINT DF_Categories_DisplayOrder DEFAULT 0;
END;

IF COL_LENGTH('Categories', 'IsActive') IS NULL
BEGIN
    ALTER TABLE Categories ADD IsActive bit NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1;
END;

IF COL_LENGTH('Products', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE Products ADD CreatedAt datetime2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME();
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_ParentCategoryID' AND object_id = OBJECT_ID('Categories'))
BEGIN
    CREATE INDEX IX_Categories_ParentCategoryID ON Categories(ParentCategoryID);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_Slug' AND object_id = OBJECT_ID('Categories'))
BEGIN
    CREATE UNIQUE INDEX IX_Categories_Slug ON Categories(Slug) WHERE Slug IS NOT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Categories_Categories_ParentCategoryID')
BEGIN
    ALTER TABLE Categories WITH CHECK ADD CONSTRAINT FK_Categories_Categories_ParentCategoryID
    FOREIGN KEY (ParentCategoryID) REFERENCES Categories(CategoryID);
END;

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske', N'Glavna ženska kategorija', NULL, 'zenske', 10, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muski')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muški', N'Glavna muška kategorija', NULL, 'muski', 20, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'djeca')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Djeca', N'Dječija kategorija', NULL, 'djeca', 30, 1);

DECLARE @ZenskeId int = (SELECT TOP 1 CategoryID FROM Categories WHERE Slug = 'zenske');
DECLARE @MuskiId int = (SELECT TOP 1 CategoryID FROM Categories WHERE Slug = 'muski');
DECLARE @DjecaId int = (SELECT TOP 1 CategoryID FROM Categories WHERE Slug = 'djeca');

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-patike')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske patike', NULL, @ZenskeId, 'zenske-patike', 10, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-cipele')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske cipele', NULL, @ZenskeId, 'zenske-cipele', 20, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-cizme')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske čizme', NULL, @ZenskeId, 'zenske-cizme', 30, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-sandale')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske sandale', NULL, @ZenskeId, 'zenske-sandale', 40, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-torbe')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske torbe', NULL, @ZenskeId, 'zenske-torbe', 50, 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-patike')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške patike', NULL, @MuskiId, 'muske-patike', 10, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-cipele')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške cipele', NULL, @MuskiId, 'muske-cipele', 20, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-sandale')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške sandale', NULL, @MuskiId, 'muske-sandale', 40, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-torbe')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške torbe', NULL, @MuskiId, 'muske-torbe', 50, 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'djecije-patike')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Dječije patike', NULL, @DjecaId, 'djecije-patike', 10, 1);

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260518120000_CategoryHierarchy')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260518120000_CategoryHierarchy', N'8.0.23');
END;