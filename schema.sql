IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [DisplayName] nvarchar(150) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [IX_AspNetUsers_Email] ON [AspNetUsers] ([Email]) WHERE [Email] IS NOT NULL;

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260925025745_InitialIdentity', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Brands] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
);
DECLARE @defaultSchema AS sysname;
SET @defaultSchema = SCHEMA_NAME();
DECLARE @description AS sql_variant;
SET @description = N'Catálogo de marcas.';
EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'Brands';

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [ParentCategoryId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema1 AS sysname;
SET @defaultSchema1 = SCHEMA_NAME();
DECLARE @description1 AS sql_variant;
SET @description1 = N'Catálogo de categorías de activos TI.';
EXEC sp_addextendedproperty 'MS_Description', @description1, 'SCHEMA', @defaultSchema1, 'TABLE', N'Categories';
SET @description1 = N'Nombre visible de la categoría.';
EXEC sp_addextendedproperty 'MS_Description', @description1, 'SCHEMA', @defaultSchema1, 'TABLE', N'Categories', 'COLUMN', N'Name';
SET @description1 = N'Orden de visualización.';
EXEC sp_addextendedproperty 'MS_Description', @description1, 'SCHEMA', @defaultSchema1, 'TABLE', N'Categories', 'COLUMN', N'SortOrder';

CREATE TABLE [Employees] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeNumber] nvarchar(30) NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [Department] nvarchar(120) NULL,
    [IsActive] bit NOT NULL,
    [IdentityUserId] uniqueidentifier NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_AspNetUsers_IdentityUserId] FOREIGN KEY ([IdentityUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
DECLARE @defaultSchema2 AS sysname;
SET @defaultSchema2 = SCHEMA_NAME();
DECLARE @description2 AS sql_variant;
SET @description2 = N'Colaboradores. IdentityUserId opcional (pueden no tener cuenta).';
EXEC sp_addextendedproperty 'MS_Description', @description2, 'SCHEMA', @defaultSchema2, 'TABLE', N'Employees';
SET @description2 = N'Número de empleado único.';
EXEC sp_addextendedproperty 'MS_Description', @description2, 'SCHEMA', @defaultSchema2, 'TABLE', N'Employees', 'COLUMN', N'EmployeeNumber';

CREATE TABLE [FolioCounters] (
    [Id] int NOT NULL IDENTITY,
    [Prefix] nvarchar(20) NOT NULL,
    [Year] int NULL,
    [LastNumber] int NOT NULL,
    [PadLength] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FolioCounters] PRIMARY KEY ([Id])
);
DECLARE @defaultSchema3 AS sysname;
SET @defaultSchema3 = SCHEMA_NAME();
DECLARE @description3 AS sql_variant;
SET @description3 = N'Contadores de folio (responsivas y documentos).';
EXEC sp_addextendedproperty 'MS_Description', @description3, 'SCHEMA', @defaultSchema3, 'TABLE', N'FolioCounters';
SET @description3 = N'Prefijo del documento, ej. RES.';
EXEC sp_addextendedproperty 'MS_Description', @description3, 'SCHEMA', @defaultSchema3, 'TABLE', N'FolioCounters', 'COLUMN', N'Prefix';

CREATE TABLE [Locations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [IsWarehouse] bit NOT NULL,
    [ParentLocationId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Locations_Locations_ParentLocationId] FOREIGN KEY ([ParentLocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema4 AS sysname;
SET @defaultSchema4 = SCHEMA_NAME();
DECLARE @description4 AS sql_variant;
SET @description4 = N'Ubicaciones físicas / bodegas.';
EXEC sp_addextendedproperty 'MS_Description', @description4, 'SCHEMA', @defaultSchema4, 'TABLE', N'Locations';

CREATE TABLE [Suppliers] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Contact] nvarchar(150) NULL,
    [Email] nvarchar(256) NULL,
    [Phone] nvarchar(40) NULL,
    [OffersPurchase] bit NOT NULL,
    [OffersMaintenance] bit NOT NULL,
    [OffersRental] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
DECLARE @defaultSchema5 AS sysname;
SET @defaultSchema5 = SCHEMA_NAME();
DECLARE @description5 AS sql_variant;
SET @description5 = N'Proveedores: compra, mantenimiento y/o arrendamiento.';
EXEC sp_addextendedproperty 'MS_Description', @description5, 'SCHEMA', @defaultSchema5, 'TABLE', N'Suppliers';

CREATE TABLE [Models] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Specs] nvarchar(2000) NULL,
    [CategoryId] int NOT NULL,
    [BrandId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Models] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Models_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Models_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema6 AS sysname;
SET @defaultSchema6 = SCHEMA_NAME();
DECLARE @description6 AS sql_variant;
SET @description6 = N'Modelos comerciales (categoría + marca).';
EXEC sp_addextendedproperty 'MS_Description', @description6, 'SCHEMA', @defaultSchema6, 'TABLE', N'Models';

CREATE TABLE [CustodyForms] (
    [Id] uniqueidentifier NOT NULL,
    [Folio] nvarchar(40) NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [IssuedAt] datetime2 NULL,
    [SignedAt] datetime2 NULL,
    [Notes] nvarchar(2000) NULL,
    [IssuedByUserId] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CustodyForms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustodyForms_AspNetUsers_IssuedByUserId] FOREIGN KEY ([IssuedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustodyForms_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema7 AS sysname;
SET @defaultSchema7 = SCHEMA_NAME();
DECLARE @description7 AS sql_variant;
SET @description7 = N'Responsivas (resguardo). Folio RES-yyyy-####. Multi-renglón.';
EXEC sp_addextendedproperty 'MS_Description', @description7, 'SCHEMA', @defaultSchema7, 'TABLE', N'CustodyForms';
SET @description7 = N'Folio único de responsiva.';
EXEC sp_addextendedproperty 'MS_Description', @description7, 'SCHEMA', @defaultSchema7, 'TABLE', N'CustodyForms', 'COLUMN', N'Folio';

CREATE TABLE [Assets] (
    [Id] uniqueidentifier NOT NULL,
    [AssetCode] nvarchar(50) NOT NULL,
    [SerialNumber] nvarchar(100) NULL,
    [Kind] nvarchar(30) NOT NULL,
    [ModelId] int NOT NULL,
    [OwnershipType] nvarchar(30) NOT NULL,
    [SupplierId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [LocationId] int NULL,
    [PurchaseDate] date NULL,
    [RentalEndDate] date NULL,
    [Imei] nvarchar(20) NULL,
    [WarrantyEndDate] date NULL,
    [ContractNumber] nvarchar(80) NULL,
    [CurrentEmployeeId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Assets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assets_Employees_CurrentEmployeeId] FOREIGN KEY ([CurrentEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assets_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assets_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assets_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema8 AS sysname;
SET @defaultSchema8 = SCHEMA_NAME();
DECLARE @description8 AS sql_variant;
SET @description8 = N'Activos TI: equipos y accesorios de inventario.';
EXEC sp_addextendedproperty 'MS_Description', @description8, 'SCHEMA', @defaultSchema8, 'TABLE', N'Assets';
SET @description8 = N'Código de inventario / etiqueta.';
EXEC sp_addextendedproperty 'MS_Description', @description8, 'SCHEMA', @defaultSchema8, 'TABLE', N'Assets', 'COLUMN', N'AssetCode';

CREATE TABLE [AssetAssignments] (
    [Id] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    [ReturnedAt] datetime2 NULL,
    [ReturnCondition] nvarchar(30) NULL,
    [Notes] nvarchar(1000) NULL,
    [AssignedByUserId] uniqueidentifier NOT NULL,
    [ReturnedByUserId] uniqueidentifier NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AssetAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetAssignments_AspNetUsers_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetAssignments_AspNetUsers_ReturnedByUserId] FOREIGN KEY ([ReturnedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetAssignments_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema9 AS sysname;
SET @defaultSchema9 = SCHEMA_NAME();
DECLARE @description9 AS sql_variant;
SET @description9 = N'Asignaciones activo-colaborador. Una activa por activo.';
EXEC sp_addextendedproperty 'MS_Description', @description9, 'SCHEMA', @defaultSchema9, 'TABLE', N'AssetAssignments';
SET @description9 = N'Usuario Identity que ejecutó la asignación.';
EXEC sp_addextendedproperty 'MS_Description', @description9, 'SCHEMA', @defaultSchema9, 'TABLE', N'AssetAssignments', 'COLUMN', N'AssignedByUserId';

CREATE TABLE [AssetMovements] (
    [Id] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [MovementType] nvarchar(40) NOT NULL,
    [FromStatus] nvarchar(30) NULL,
    [ToStatus] nvarchar(30) NULL,
    [FromLocationId] int NULL,
    [ToLocationId] int NULL,
    [EmployeeId] uniqueidentifier NULL,
    [PerformedByUserId] uniqueidentifier NOT NULL,
    [CustodyFormId] uniqueidentifier NULL,
    [Notes] nvarchar(2000) NULL,
    [OccurredAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AssetMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetMovements_AspNetUsers_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetMovements_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetMovements_CustodyForms_CustodyFormId] FOREIGN KEY ([CustodyFormId]) REFERENCES [CustodyForms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetMovements_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetMovements_Locations_FromLocationId] FOREIGN KEY ([FromLocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetMovements_Locations_ToLocationId] FOREIGN KEY ([ToLocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION
);
DECLARE @defaultSchema10 AS sysname;
SET @defaultSchema10 = SCHEMA_NAME();
DECLARE @description10 AS sql_variant;
SET @description10 = N'Historial y trazabilidad de movimientos del activo.';
EXEC sp_addextendedproperty 'MS_Description', @description10, 'SCHEMA', @defaultSchema10, 'TABLE', N'AssetMovements';
SET @description10 = N'Usuario Identity que ejecutó el movimiento.';
EXEC sp_addextendedproperty 'MS_Description', @description10, 'SCHEMA', @defaultSchema10, 'TABLE', N'AssetMovements', 'COLUMN', N'PerformedByUserId';

CREATE TABLE [CustodyFormLines] (
    [Id] uniqueidentifier NOT NULL,
    [CustodyFormId] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [Quantity] int NOT NULL DEFAULT 1,
    [ConditionOnDelivery] nvarchar(30) NOT NULL,
    [DeliveryNotes] nvarchar(1000) NULL,
    [ReturnNotes] nvarchar(1000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CustodyFormLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustodyFormLines_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustodyFormLines_CustodyForms_CustodyFormId] FOREIGN KEY ([CustodyFormId]) REFERENCES [CustodyForms] ([Id]) ON DELETE CASCADE
);
DECLARE @defaultSchema11 AS sysname;
SET @defaultSchema11 = SCHEMA_NAME();
DECLARE @description11 AS sql_variant;
SET @description11 = N'Renglones de responsiva: equipo o accesorio.';
EXEC sp_addextendedproperty 'MS_Description', @description11, 'SCHEMA', @defaultSchema11, 'TABLE', N'CustodyFormLines';

CREATE INDEX [IX_AssetAssignments_AssignedByUserId] ON [AssetAssignments] ([AssignedByUserId]);

CREATE INDEX [IX_AssetAssignments_EmployeeId] ON [AssetAssignments] ([EmployeeId]);

CREATE INDEX [IX_AssetAssignments_ReturnedByUserId] ON [AssetAssignments] ([ReturnedByUserId]);

CREATE UNIQUE INDEX [UX_AssetAssignments_ActiveAsset] ON [AssetAssignments] ([AssetId]) WHERE [ReturnedAt] IS NULL AND [IsDeleted] = 0;

CREATE INDEX [IX_AssetMovements_AssetId_OccurredAt] ON [AssetMovements] ([AssetId], [OccurredAt]);

CREATE INDEX [IX_AssetMovements_CustodyFormId] ON [AssetMovements] ([CustodyFormId]);

CREATE INDEX [IX_AssetMovements_EmployeeId] ON [AssetMovements] ([EmployeeId]);

CREATE INDEX [IX_AssetMovements_FromLocationId] ON [AssetMovements] ([FromLocationId]);

CREATE INDEX [IX_AssetMovements_PerformedByUserId] ON [AssetMovements] ([PerformedByUserId]);

CREATE INDEX [IX_AssetMovements_ToLocationId] ON [AssetMovements] ([ToLocationId]);

CREATE UNIQUE INDEX [IX_Assets_AssetCode] ON [Assets] ([AssetCode]);

CREATE INDEX [IX_Assets_CurrentEmployeeId] ON [Assets] ([CurrentEmployeeId]);

CREATE INDEX [IX_Assets_Kind] ON [Assets] ([Kind]);

CREATE INDEX [IX_Assets_LocationId] ON [Assets] ([LocationId]);

CREATE INDEX [IX_Assets_ModelId] ON [Assets] ([ModelId]);

CREATE UNIQUE INDEX [IX_Assets_SerialNumber] ON [Assets] ([SerialNumber]) WHERE [SerialNumber] IS NOT NULL;

CREATE INDEX [IX_Assets_Status] ON [Assets] ([Status]);

CREATE INDEX [IX_Assets_SupplierId] ON [Assets] ([SupplierId]);

CREATE UNIQUE INDEX [IX_Brands_Name] ON [Brands] ([Name]);

CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);

CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);

CREATE INDEX [IX_CustodyFormLines_AssetId] ON [CustodyFormLines] ([AssetId]);

CREATE INDEX [IX_CustodyFormLines_CustodyFormId] ON [CustodyFormLines] ([CustodyFormId]);

CREATE INDEX [IX_CustodyForms_EmployeeId] ON [CustodyForms] ([EmployeeId]);

CREATE UNIQUE INDEX [IX_CustodyForms_Folio] ON [CustodyForms] ([Folio]);

CREATE INDEX [IX_CustodyForms_IssuedByUserId] ON [CustodyForms] ([IssuedByUserId]);

CREATE INDEX [IX_Employees_Email] ON [Employees] ([Email]);

CREATE UNIQUE INDEX [IX_Employees_EmployeeNumber] ON [Employees] ([EmployeeNumber]);

CREATE INDEX [IX_Employees_IdentityUserId] ON [Employees] ([IdentityUserId]);

CREATE UNIQUE INDEX [IX_FolioCounters_Prefix_Year] ON [FolioCounters] ([Prefix], [Year]) WHERE [Year] IS NOT NULL;

CREATE INDEX [IX_Locations_ParentLocationId] ON [Locations] ([ParentLocationId]);

CREATE INDEX [IX_Models_BrandId] ON [Models] ([BrandId]);

CREATE UNIQUE INDEX [IX_Models_CategoryId_BrandId_Name] ON [Models] ([CategoryId], [BrandId], [Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260925044709_AddBusinessSchema', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_Employees_Email] ON [Employees];

CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260925055825_EmployeeEmailUnique', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE UNIQUE INDEX [IX_Suppliers_Name] ON [Suppliers] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260926051432_SupplierNameUnique', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;

-- sp_AssignAssets: asigna 1..N activos a un empleado, emite responsiva RES-yyyy-####.
-- @LinesJson: [{"assetId":"...","quantity":1,"conditionOnDelivery":"New","deliveryNotes":null}]

CREATE OR ALTER PROCEDURE dbo.sp_AssignAssets
    @EmployeeId          UNIQUEIDENTIFIER,
    @AssignedByUserId    UNIQUEIDENTIFIER,
    @Notes               NVARCHAR(1000) = NULL,
    @LinesJson           NVARCHAR(MAX),
    @CustodyFormId       UNIQUEIDENTIFIER OUTPUT,
    @Folio               NVARCHAR(40) OUTPUT,
    @AssignedCount       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Year INT = YEAR(SYSUTCDATETIME());
    DECLARE @Next INT;
    DECLARE @Pad INT = 4;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @EmployeeId AND IsDeleted = 0 AND IsActive = 1)
        THROW 50001, N'Empleado no encontrado o inactivo.', 1;

    IF NOT EXISTS (SELECT 1 FROM OPENJSON(@LinesJson))
        THROW 50002, N'Debe indicar al menos un activo.', 1;

    BEGIN TRAN;

    IF EXISTS (
        SELECT 1
        FROM OPENJSON(@LinesJson)
             WITH (assetId UNIQUEIDENTIFIER '$.assetId') j
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.Assets a
            WHERE a.Id = j.assetId AND a.IsDeleted = 0 AND a.Status = N'Available'
        )
    )
        THROW 50003, N'Uno o mÃ¡s activos no estÃ¡n disponibles.', 1;

    IF EXISTS (
        SELECT 1
        FROM OPENJSON(@LinesJson)
             WITH (assetId UNIQUEIDENTIFIER '$.assetId') j
        INNER JOIN dbo.AssetAssignments aa
            ON aa.AssetId = j.assetId AND aa.ReturnedAt IS NULL AND aa.IsDeleted = 0
    )
        THROW 50004, N'Uno o mÃ¡s activos ya tienen asignaciÃ³n activa.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.FolioCounters WITH (UPDLOCK, HOLDLOCK) WHERE Prefix = N'RES' AND [Year] = @Year)
    BEGIN
        INSERT INTO dbo.FolioCounters (Prefix, [Year], LastNumber, PadLength, CreatedAt, UpdatedAt)
        VALUES (N'RES', @Year, 0, 4, @Now, @Now);
    END

    UPDATE dbo.FolioCounters WITH (UPDLOCK, HOLDLOCK)
    SET LastNumber = LastNumber + 1,
        UpdatedAt = @Now
    WHERE Prefix = N'RES' AND [Year] = @Year;

    SELECT @Next = LastNumber, @Pad = PadLength
    FROM dbo.FolioCounters
    WHERE Prefix = N'RES' AND [Year] = @Year;

    SET @Folio = N'RES-' + CAST(@Year AS NVARCHAR(4)) + N'-'
        + RIGHT(REPLICATE(N'0', @Pad) + CAST(@Next AS NVARCHAR(20)), @Pad);
    SET @CustodyFormId = NEWID();

    INSERT INTO dbo.CustodyForms
        (Id, Folio, EmployeeId, Status, IssuedAt, Notes, IssuedByUserId, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (@CustodyFormId, @Folio, @EmployeeId, N'Issued', @Now, @Notes, @AssignedByUserId, 0, @Now, @Now);

    DECLARE @AssetId UNIQUEIDENTIFIER;
    DECLARE @Qty INT;
    DECLARE @Cond NVARCHAR(30);
    DECLARE @DelNotes NVARCHAR(1000);
    DECLARE @AssignmentId UNIQUEIDENTIFIER;
    DECLARE @LineId UNIQUEIDENTIFIER;
    DECLARE @MovementId UNIQUEIDENTIFIER;
    DECLARE @FromLoc INT;

    DECLARE line_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT assetId, ISNULL(quantity, 1), ISNULL(conditionOnDelivery, N'New'), deliveryNotes
        FROM OPENJSON(@LinesJson)
        WITH (
            assetId UNIQUEIDENTIFIER '$.assetId',
            quantity INT '$.quantity',
            conditionOnDelivery NVARCHAR(30) '$.conditionOnDelivery',
            deliveryNotes NVARCHAR(1000) '$.deliveryNotes'
        );

    OPEN line_cursor;
    FETCH NEXT FROM line_cursor INTO @AssetId, @Qty, @Cond, @DelNotes;

    SET @AssignedCount = 0;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @FromLoc = LocationId FROM dbo.Assets WHERE Id = @AssetId;

        SET @LineId = NEWID();
        INSERT INTO dbo.CustodyFormLines
            (Id, CustodyFormId, AssetId, Quantity, ConditionOnDelivery, DeliveryNotes, IsDeleted, CreatedAt, UpdatedAt)
        VALUES
            (@LineId, @CustodyFormId, @AssetId, @Qty, @Cond, @DelNotes, 0, @Now, @Now);

        SET @AssignmentId = NEWID();
        INSERT INTO dbo.AssetAssignments
            (Id, AssetId, EmployeeId, AssignedAt, Notes, AssignedByUserId, IsDeleted, CreatedAt, UpdatedAt)
        VALUES
            (@AssignmentId, @AssetId, @EmployeeId, @Now, @Notes, @AssignedByUserId, 0, @Now, @Now);

        UPDATE dbo.Assets
        SET Status = N'Assigned',
            CurrentEmployeeId = @EmployeeId,
            UpdatedAt = @Now
        WHERE Id = @AssetId;

        SET @MovementId = NEWID();
        INSERT INTO dbo.AssetMovements
            (Id, AssetId, MovementType, FromStatus, ToStatus, FromLocationId, ToLocationId,
             EmployeeId, PerformedByUserId, CustodyFormId, Notes, OccurredAt, IsDeleted, CreatedAt, UpdatedAt)
        VALUES
            (@MovementId, @AssetId, N'Assigned', N'Available', N'Assigned', @FromLoc, @FromLoc,
             @EmployeeId, @AssignedByUserId, @CustodyFormId, @Notes, @Now, 0, @Now, @Now);

        SET @AssignedCount = @AssignedCount + 1;
        FETCH NEXT FROM line_cursor INTO @AssetId, @Qty, @Cond, @DelNotes;
    END

    CLOSE line_cursor;
    DEALLOCATE line_cursor;

    COMMIT TRAN;
END



-- sp_ReturnAsset: cierra asignaciÃ³n activa y deja el activo Available.

CREATE OR ALTER PROCEDURE dbo.sp_ReturnAsset
    @AssetId            UNIQUEIDENTIFIER,
    @ReturnedByUserId   UNIQUEIDENTIFIER,
    @ReturnCondition    NVARCHAR(30) = N'Used',
    @Notes              NVARCHAR(1000) = NULL,
    @ToLocationId       INT = NULL,
    @AssignmentId       UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @EmployeeId UNIQUEIDENTIFIER;
    DECLARE @FromStatus NVARCHAR(30);
    DECLARE @FromLoc INT;
    DECLARE @MovementId UNIQUEIDENTIFIER;

    BEGIN TRAN;

    SELECT TOP (1)
        @AssignmentId = Id,
        @EmployeeId = EmployeeId
    FROM dbo.AssetAssignments WITH (UPDLOCK, ROWLOCK)
    WHERE AssetId = @AssetId AND ReturnedAt IS NULL AND IsDeleted = 0;

    IF @AssignmentId IS NULL
        THROW 50011, N'El activo no tiene asignaciÃ³n activa.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Assets
        WHERE Id = @AssetId AND IsDeleted = 0 AND Status = N'Assigned'
    )
        THROW 50012, N'El activo no estÃ¡ en estado Assigned.', 1;

    SELECT @FromStatus = Status, @FromLoc = LocationId FROM dbo.Assets WHERE Id = @AssetId;

    UPDATE dbo.AssetAssignments
    SET ReturnedAt = @Now,
        ReturnCondition = @ReturnCondition,
        Notes = COALESCE(@Notes, Notes),
        ReturnedByUserId = @ReturnedByUserId,
        UpdatedAt = @Now
    WHERE Id = @AssignmentId;

    UPDATE dbo.Assets
    SET Status = N'Available',
        CurrentEmployeeId = NULL,
        LocationId = COALESCE(@ToLocationId, LocationId),
        UpdatedAt = @Now
    WHERE Id = @AssetId;

    SET @MovementId = NEWID();
    INSERT INTO dbo.AssetMovements
        (Id, AssetId, MovementType, FromStatus, ToStatus, FromLocationId, ToLocationId,
         EmployeeId, PerformedByUserId, Notes, OccurredAt, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (@MovementId, @AssetId, N'Returned', @FromStatus, N'Available', @FromLoc, COALESCE(@ToLocationId, @FromLoc),
         @EmployeeId, @ReturnedByUserId, @Notes, @Now, 0, @Now, @Now);

    COMMIT TRAN;
END


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260926055710_AddAssignReturnStoredProcedures', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Assets] ADD [Condition] nvarchar(30) NOT NULL DEFAULT N'New';

CREATE INDEX [IX_Assets_Condition] ON [Assets] ([Condition]);


CREATE OR ALTER PROCEDURE dbo.sp_ReturnAsset
    @AssetId            UNIQUEIDENTIFIER,
    @ReturnedByUserId   UNIQUEIDENTIFIER,
    @ReturnCondition    NVARCHAR(30) = N'Used',
    @Notes              NVARCHAR(1000) = NULL,
    @ToLocationId       INT = NULL,
    @AssignmentId       UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @EmployeeId UNIQUEIDENTIFIER;
    DECLARE @FromStatus NVARCHAR(30);
    DECLARE @FromLoc INT;
    DECLARE @MovementId UNIQUEIDENTIFIER;

    BEGIN TRAN;

    SELECT TOP (1)
        @AssignmentId = Id,
        @EmployeeId = EmployeeId
    FROM dbo.AssetAssignments WITH (UPDLOCK, ROWLOCK)
    WHERE AssetId = @AssetId AND ReturnedAt IS NULL AND IsDeleted = 0;

    IF @AssignmentId IS NULL
        THROW 50011, N'El activo no tiene asignación activa.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Assets
        WHERE Id = @AssetId AND IsDeleted = 0 AND Status = N'Assigned'
    )
        THROW 50012, N'El activo no está en estado Assigned.', 1;

    SELECT @FromStatus = Status, @FromLoc = LocationId FROM dbo.Assets WHERE Id = @AssetId;

    UPDATE dbo.AssetAssignments
    SET ReturnedAt = @Now,
        ReturnCondition = @ReturnCondition,
        Notes = COALESCE(@Notes, Notes),
        ReturnedByUserId = @ReturnedByUserId,
        UpdatedAt = @Now
    WHERE Id = @AssignmentId;

    UPDATE dbo.Assets
    SET Status = N'Available',
        [Condition] = N'Used',
        CurrentEmployeeId = NULL,
        LocationId = COALESCE(@ToLocationId, LocationId),
        UpdatedAt = @Now
    WHERE Id = @AssetId;

    SET @MovementId = NEWID();
    INSERT INTO dbo.AssetMovements
        (Id, AssetId, MovementType, FromStatus, ToStatus, FromLocationId, ToLocationId,
         EmployeeId, PerformedByUserId, Notes, OccurredAt, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (@MovementId, @AssetId, N'Returned', @FromStatus, N'Available', @FromLoc, COALESCE(@ToLocationId, @FromLoc),
         @EmployeeId, @ReturnedByUserId, @Notes, @Now, 0, @Now, @Now);

    COMMIT TRAN;
END


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260926170930_AddAssetConditionAndReturnUsed', N'10.0.12');

COMMIT;
GO

