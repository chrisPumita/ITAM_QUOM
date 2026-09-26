-- sp_ReturnAsset: cierra asignación activa y deja el activo Available.

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
GO
