using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/brand")]
public class BrandController : ControllerBase
{
    private readonly DbSet<Brand> _dbSet;
    private readonly DatabaseContext _context;

    private readonly DbSet<SalesChannel> _dbSetSalesChannels;

    private readonly DbSet<ContactInfo> _dbSetContactInfos;

    public BrandController(DatabaseContext context)
    {
        this._context = context;
        this._dbSet = _context.Set<Brand>();
        this._dbSetSalesChannels = _context.Set<SalesChannel>();
        this._dbSetContactInfos = _context.Set<ContactInfo>();


    }

    [HttpGet(Name = "GetBrands")]
    public Dictionary<string, List<Brand>> Get(string? searchString)
    {

        if (!String.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            return new Dictionary<string, List<Brand>>
            {

                ["brand"] = _dbSet.Include(x => x.SalesChannels).Include(x => x.contactInfos).Where(brand => brand.Name!.ToLower().Contains(searchString)).ToList()
            };
        }
        else
            return new Dictionary<string, List<Brand>>
            {

                ["brand"] = _dbSet.Include(x => x.SalesChannels).Include(x => x.contactInfos).ToList()
            };
    }

    [HttpPost(Name = "CreateBrand")]
    public async Task<IActionResult> Create(Brand dto)
    {

        Brand brand = new Brand(dto);

        var brandName = _context.brands?.FirstOrDefault(x => x.Name == brand.Name);
        if (brandName != null)
        {
            return NotFound("There is already a brand with this name!");
        }
        _dbSet.Add(brand);
        await _context.SaveChangesAsync();
        return Ok(brand);
    }

    [HttpGet("{id}", Name = "GetBrand")]
    public IActionResult Get(int id)
    {
        var brandQuery = _dbSet.Include(x => x.SalesChannels).Include(x => x.contactInfos).Where(brand => brand.Id == id);

        if (!brandQuery.Any())
        {
            return NotFound("No Brand found with the given ID.");
        }

        return Ok(brandQuery.First());
    }

    [HttpGet("{id}/channels", Name = "GetBrandChannels")]
    public IActionResult GetChannelTypes(int id)
    {
        var brandQuery = _dbSet.Include(x => x.SalesChannels).Where(brand => brand.Id == id);

        if (!brandQuery.Any())
        {
            return NotFound("No Brand found with the given ID.");
        }

        return Ok(brandQuery.First().SalesChannels);
    }

    [HttpDelete("{id}", Name = "DeleteBrand")]
    public IActionResult Delete(int id)
    {
        var brandQuery = _dbSet.Include(x => x.SalesChannels).Include(x => x.contactInfos).Where(brand => brand.Id == id);

        if (!brandQuery.Any())
        {
            return NotFound("No Brand found with the given ID.");
        }

        var brand = brandQuery.First();
        _dbSet.Remove(brand);
        foreach (SalesChannel saleschannel in brand.SalesChannels)
        {
            _dbSetSalesChannels.Remove(saleschannel);
        }
        foreach (ContactInfo contactInfo in brand.contactInfos)
        {
            _dbSetContactInfos.Remove(contactInfo);
        }
        _context.SaveChanges();

        return Ok(brand);
    }




}