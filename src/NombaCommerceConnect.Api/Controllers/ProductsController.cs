using Microsoft.AspNetCore.Mvc;
using NombaCommerceConnect.Application.Interfaces;
using NombaCommerceConnect.Domain.Entities;

namespace NombaCommerceConnect.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductsController(IProductRepository productRepository, IVendorRepository vendorRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
    }

    public record CreateProductRequest(Guid VendorId, string Name, string Description, decimal Price, int StockQuantity, string? ImageUrl);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var products = await _productRepository.GetActiveAsync(ct);
        return Ok(products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.StockQuantity,
            p.ImageUrl,
            VendorId = p.VendorId,
            VendorName = p.Vendor?.BusinessName
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, ct);
        if (vendor is null)
            return BadRequest(new { error = $"Vendor {request.VendorId} not found." });

        try
        {
            var product = new Product(request.VendorId, request.Name, request.Description, request.Price, request.StockQuantity, request.ImageUrl);
            await _productRepository.AddAsync(product, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetAll), new { }, new { product.Id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
