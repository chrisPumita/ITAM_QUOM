namespace ITAM.Shared.Enums;

public enum OwnershipType
{
    Owned = 1,
    Rented = 2
}

public enum AssetStatus
{
    Available = 1,
    Assigned = 2,
    Maintenance = 3,
    Retired = 4
}

public enum AssetKind
{
    Equipment = 1,
    Accessory = 2
}

public enum MovementType
{
    Created = 1,
    Assigned = 2,
    Returned = 3,
    StatusChanged = 4,
    LocationChanged = 5,
    Updated = 6,
    CustodyIssued = 7
}

public enum CustodyFormStatus
{
    Draft = 1,
    Issued = 2,
    Signed = 3,
    Closed = 4
}

public enum AssetCondition
{
    New = 1,
    Used = 2
}
