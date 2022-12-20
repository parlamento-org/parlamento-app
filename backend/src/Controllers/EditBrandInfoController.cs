using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("/edit")]
public class EditBrandInfoController : ControllerBase
{
    private readonly DbSet<Brand> _dbSet;
    private readonly DbSet<ContactInfo> _dbSetContactInfos;
    private readonly DatabaseContext _context;

    public EditBrandInfoController(DatabaseContext context)
    {
        this._context = context;
        this._dbSet = _context.Set<Brand>();
        this._dbSetContactInfos = _context.Set<ContactInfo>();

    }

    [HttpPut("contact-info", Name = "EditBrandContactInfo")]
    public IActionResult editContactInfo(ContactInfoDTO contactInfoDTO)
    {

        var brandQuery = _dbSet.Include(x => x.contactInfos).Where(brand => brand.Id == contactInfoDTO.brandID);

        if (!brandQuery.Any())
        {
            return NotFound("No Brand found with that ID");
        }
        var brand = brandQuery.First();

        brand.Name = contactInfoDTO.newBrandName;

        foreach (ContactInfo contactInfo in brand.contactInfos)
        {
            _dbSetContactInfos.Remove(contactInfo);
        }

        brand.contactInfos = contactInfoDTO.contactInfos;

        _context.SaveChanges();

        return Ok(brand);
    }

    [HttpPut("sales-channel", Name = "EditSalesChannel")]
    public IActionResult editSalesChannel(FullfilmentDTO fullfilmentDTO)
    {

        var brandQuery = _dbSet.Include(x => x.SalesChannels).Where(brand => brand.Id == fullfilmentDTO.brandID);

        if (!brandQuery.Any())
        {
            return NotFound("No Brand found with that ID");
        }
        var brand = brandQuery.First();

        var salesChannel = brand.SalesChannels?.Find(salesChannel => salesChannel.Name == fullfilmentDTO.salesChannelName);

        if (salesChannel == null)
        {
            return NotFound("No SalesChannel found with that name");
        }

        salesChannel.isActive = fullfilmentDTO.isActive;
        salesChannel.fullOrWait = salesChannel.isActive && fullfilmentDTO.fullOrWait;
        salesChannel.partialAndCancel = salesChannel.isActive && fullfilmentDTO.partialAndCancel;

        _context.SaveChanges();

        return Ok(salesChannel);
    }

    [HttpPut("return-policy", Name = "EditReturnPolicy")]
    public IActionResult editReturnPolicy(ReturnDTO returnDTO)
    {

        var brandQuery = _dbSet.Include(x => x.SalesChannels).Where(brand => brand.Id == returnDTO.brandID);

        if (!brandQuery.Any())
        {
            return NotFound("No Brand found with that ID");
        }
        var brand = brandQuery.First();

        var salesChannel = brand.SalesChannels?.Find(salesChannel => salesChannel.Name == returnDTO.salesChannelName);

        if (salesChannel == null)
        {
            return NotFound("No SalesChannel found with that name");
        }

        if (!salesChannel.isActive)
        {
            var result = new ObjectResult("Can't edit an inactive sales channel's return policies!");
            result.StatusCode = 500;
            return result;
        }

        salesChannel.prePrintedReturnLabel = returnDTO.prePrintedReturnLabel;
        salesChannel.digitalReturnLabel = returnDTO.digitalReturnLabel;


        _context.SaveChanges();

        return Ok(salesChannel);
    }
}