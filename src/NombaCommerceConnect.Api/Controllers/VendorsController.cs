using Microsoft.AspNetCore.Mvc;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Domain.Entities;

namespace NombaCommerceConnect.Api.Controllers;

[ApiController]
[Route("api/vendors")]
public class VendorsController : ControllerBase
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendorsController(IVendorRepository vendorRepository, IUnitOfWork unitOfWork)
    {
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
    }

    public record CreateVendorRequest(string BusinessName, string Email, string NombaAccountId);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var vendors = await _vendorRepository.GetAllAsync(ct);
        return Ok(vendors.Select(v => new
        {
            v.Id,
            v.BusinessName,
            v.Email,
            v.NombaAccountId,
            v.CreatedAtUtc
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVendorRequest request, CancellationToken ct)
    {
        try
        {
            var vendor = new Vendor(request.BusinessName, request.Email, request.NombaAccountId);
            await _vendorRepository.AddAsync(vendor, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetAll), new { }, new { vendor.Id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
