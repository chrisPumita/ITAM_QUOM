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
        THROW 50003, N'Uno o más activos no están disponibles.', 1;

    IF EXISTS (
        SELECT 1
        FROM OPENJSON(@LinesJson)
             WITH (assetId UNIQUEIDENTIFIER '$.assetId') j
        INNER JOIN dbo.AssetAssignments aa
            ON aa.AssetId = j.assetId AND aa.ReturnedAt IS NULL AND aa.IsDeleted = 0
    )
        THROW 50004, N'Uno o más activos ya tienen asignación activa.', 1;

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
