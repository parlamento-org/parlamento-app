using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using backend.Models;

using Newtonsoft.Json;

using Xunit;
using Xunit.Extensions.Ordering;

namespace integration_tests;

public class AsnFactory : TestingWebAppFactory
{
    protected override void SeedDbForTests(DatabaseContext db)
    {
        Brand brand3 = new Brand("Maersk");
        brand3.SalesChannels[0].isActive = true;
        brand3.SalesChannels[0].fullOrWait = true;


        db.brands!.AddRange(new List<Brand>
            {
                new Brand("Levis"),
                new Brand("Nike"),
                brand3,
            });
        db.SaveChanges();
    }
}
