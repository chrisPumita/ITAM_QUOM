namespace ITAM.Domain.Interfaces.Repositories.Assignments;

public sealed class AssetAssignmentRepositoryException : Exception
{
    public string ErrorCode { get; }

    public AssetAssignmentRepositoryException(string message, string errorCode, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
