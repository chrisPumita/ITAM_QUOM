using ITAM.Domain.Entities.Company;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Domain.Interfaces.Services.Company;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Company;

namespace ITAM.Application.Services.Company;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repo;

    public SupplierService(ISupplierRepository repo) => _repo = repo;

    public async Task<Result<List<SupplierListDto>>> ListAsync(bool? onlyActive)
    {
        var items = await _repo.ListAsync(onlyActive);
        return Ok(items.Select(Map).ToList(), "Proveedores obtenidos.");
    }

    public async Task<Result<SupplierListDto>> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<SupplierListDto>("Proveedor no encontrado.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<Guid>> CreateAsync(SupplierUpsertDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<Guid>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, null))
            return Fail<Guid>("Ya existe un proveedor con ese nombre.", "Duplicate");

        var entity = new Supplier
        {
            Name = name,
            Contact = NullIfWhite(dto.Contact),
            Email = NullIfWhite(dto.Email),
            Phone = NullIfWhite(dto.Phone),
            OffersPurchase = dto.OffersPurchase,
            OffersMaintenance = dto.OffersMaintenance,
            OffersRental = dto.OffersRental,
            IsActive = dto.IsActive
        };
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Proveedor creado.");
    }

    public async Task<Result<bool>> UpdateAsync(Guid id, SupplierUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Proveedor no encontrado.", "NotFound");

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<bool>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, id))
            return Fail<bool>("Ya existe un proveedor con ese nombre.", "Duplicate");

        entity.Name = name;
        entity.Contact = NullIfWhite(dto.Contact);
        entity.Email = NullIfWhite(dto.Email);
        entity.Phone = NullIfWhite(dto.Phone);
        entity.OffersPurchase = dto.OffersPurchase;
        entity.OffersMaintenance = dto.OffersMaintenance;
        entity.OffersRental = dto.OffersRental;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Proveedor actualizado.");
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SupplierListDto Map(Supplier x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Contact = x.Contact,
        Email = x.Email,
        Phone = x.Phone,
        OffersPurchase = x.OffersPurchase,
        OffersMaintenance = x.OffersMaintenance,
        OffersRental = x.OffersRental,
        IsActive = x.IsActive
    };

    private static Result<T> Ok<T>(T data, string message) => new()
    {
        IsSuccess = true,
        Message = message,
        Data = data
    };

    private static Result<T> Fail<T>(string message, string error) => new()
    {
        IsSuccess = false,
        Message = message,
        Error = error
    };
}
