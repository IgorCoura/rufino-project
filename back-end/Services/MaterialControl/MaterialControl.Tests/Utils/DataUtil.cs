using Commom.Domain.BaseEntities;
using MaterialControl.Domain.Entities;
using MaterialControl.Infra.Context;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Commom.Tests.Utils
{
    public static class DataUtil
    {
        public static void InsertDataForTests(this MaterialControlContext context)
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(context.Users.AddRangeAsync(
                new User()
                {
                    Id = Guid.Parse("5fa3f749-fe32-4065-8fdf-32068dbad788"),
                    Username = "admin",
                    Role = "admin"
                },
                new User()
                {
                    Id = Guid.Parse("71b36eec-2db3-4cb1-8c98-5c894f7cc264"),
                    Username = "client",
                    Role = "client"
                }
                ));

            tasks.Add(context.Brands.AddRangeAsync(
                new Brand()
                {
                    Id = Guid.Parse("6e59a809-88c7-4e75-a684-4e5b0948ab20"),
                    Name = "Amanco",
                    Description = "Produtor de tubos"
                }
                ));

            var ids = new string[]
            {
                "2001","2002","2003","2004","2005","2006","2007","2008","2009","2010","2011","2012",
                "2013","2014","2015",
                "2999"
            };

            var functionsIdsAdmin = ids.Select(x => new FunctionId()
            {
                Id = Guid.NewGuid(),
                Name = x
            }).ToList();

            var functionsIdsClient = functionsIdsAdmin.Select(x => x).ToList();

            tasks.Add(context.Roles.AddRangeAsync(
                new Role()
                {
                    Name = "admin",
                    FunctionsIds = functionsIdsAdmin
                },
                new Role()
                {
                    Name = "client",
                    FunctionsIds = functionsIdsClient
                }
            ));

            Task.WaitAll(tasks.ToArray());
            context.SaveChanges();
        }
    }
}








