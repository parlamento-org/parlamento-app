using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SalesChannelName
{
    Webshop,
    Wholesale,
    OwnStore,
    Marketing,
    StockShared
}

public class ContactInfo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public String? Name { get; set; }

    [Required]
    public String? PhoneNumber { get; set; }

}

public class SalesChannel
{
    public SalesChannel() { }
    public SalesChannel(SalesChannelName name)
    {
        this.Name = name;
        this.fullOrWait = false;
        this.digitalReturnLabel = false;
        this.partialAndCancel = false;
        this.prePrintedReturnLabel = false;

        this.isActive = false;

    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [EnumDataType(typeof(SalesChannelName), ErrorMessage = "Invalid Sales Channel Name")]
    public SalesChannelName? Name { get; set; }

    [Required]
    public Boolean isActive { get; set; }
    public Boolean fullOrWait { get; set; }

    public Boolean partialAndCancel { get; set; }

    public Boolean digitalReturnLabel { get; set; }
    public Boolean prePrintedReturnLabel { get; set; }



}

public class FullfilmentDTO
{
    [Required]
    public int brandID { get; set; }

    [Required]
    [EnumDataType(typeof(SalesChannelName), ErrorMessage = "Invalid Sales Channel Name")]
    public SalesChannelName salesChannelName { get; set; }

    [Required]
    public Boolean fullOrWait { get; set; }
    [Required]
    public Boolean partialAndCancel { get; set; }

    [Required]
    public Boolean isActive { get; set; }

}

public class ContactInfoDTO
{
    [Required]
    public int brandID { get; set; }

    [Required]
    [MaxLength(256)]
    public String? newBrandName { get; set; }

    [Required]
    public List<ContactInfo> contactInfos { get; set; } = new List<ContactInfo>();

}

public class ReturnDTO
{
    [Required]
    public int brandID { get; set; }

    [Required]
    [EnumDataType(typeof(SalesChannelName), ErrorMessage = "Invalid Sales Channel Name")]
    public SalesChannelName salesChannelName { get; set; }

    [Required]
    public Boolean digitalReturnLabel { get; set; }
    [Required]
    public Boolean prePrintedReturnLabel { get; set; }


}

public class Brand
{

    public Brand()
    {

    }

    public override string ToString()
    {
        return "name: " + Name + " contactInfos: " + contactInfos?.Count + " salesChannel: " + SalesChannels?.Count;
    }
    public Brand(Brand dto)
    {
        Name = dto.Name;
        if (dto.contactInfos != null)
        {
            contactInfos = new List<ContactInfo>(dto.contactInfos);
        }
        if (dto.SalesChannels != null)
        {
            SalesChannels = new List<SalesChannel>(dto.SalesChannels);
        }
    }
    public Brand(String name)
    {
        Name = name;
        contactInfos = new List<ContactInfo>();
        SalesChannels = new List<SalesChannel>();
        SalesChannels.Add(new SalesChannel(SalesChannelName.Webshop));
        SalesChannels.Add(new SalesChannel(SalesChannelName.Wholesale));
        SalesChannels.Add(new SalesChannel(SalesChannelName.OwnStore));
        SalesChannels.Add(new SalesChannel(SalesChannelName.Marketing));
        SalesChannels.Add(new SalesChannel(SalesChannelName.StockShared));
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public String? Name { get; set; }
    [Required]
    [ForeignKey("BrandId")]
    public List<SalesChannel> SalesChannels { get; set; } = new List<SalesChannel>();
    [Required]
    [ForeignKey("BrandId")]
    public List<ContactInfo> contactInfos { get; set; } = new List<ContactInfo>();


}