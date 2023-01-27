using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Infra.Context;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MaterialPurchase.Tests
{
    public static class Util
    {
        public static void InsertDataForTests(this MaterialPurchaseContext context)
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(context.Constructions.AddRangeAsync(
                new Construction()
                {
                    NickName = "Build",
                    CorporateName = "Build LTDA",
                    Address = new Address("Dom Pedro", "Piracicaba", "Sao Paulo", "Brasil", "99999-000"),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Construction()
                {
                    NickName = "Ticem",
                    CorporateName = "Ticem LTDA",
                    Address = new Address("Dom Pedro", "Piracicaba", "Sao Paulo", "Brasil", "99999-000"),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    PurchasingAuthorizationUserGroups = new List<ConstructionAuthUserGroup>()
                    {
                        new ConstructionAuthUserGroup()
                        {
                            Priority= 1,
                            UserAuthorizations = new List<ConstructionUserAuthorization>()
                            {
                                new ConstructionUserAuthorization()
                                {
                                    UserId = Guid.Parse("FDEC4D71-4300-4F5D-8146-9C3E8D62528B"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        },
                        new ConstructionAuthUserGroup()
                        {
                            Priority= 2,
                            UserAuthorizations = new List<ConstructionUserAuthorization>()
                            {
                                new ConstructionUserAuthorization()
                                {
                                    UserId = Guid.Parse("59C7F554-38E6-4C13-BB11-FE47BA08F97E"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        },
                    }
                }
             ));

            tasks.Add(context.Users.AddRangeAsync(
                new User()
                {

                    Id = Guid.Parse("4922766E-D3BA-4D4C-99B0-093D5977D41F"),
                    Username = "admin",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "11"

                },
                new User()
                {

                    Id = Guid.Parse("F363DA96-1EBB-419D-B178-3F7F3B54B863"),
                    Username = "Creator",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "22"

                },
                new User()
                {

                    Id = Guid.Parse("FDEC4D71-4300-4F5D-8146-9C3E8D62528B"),
                    Username = "user1",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "33"

                },
                new User()
                {

                    Id = Guid.Parse("59C7F554-38E6-4C13-BB11-FE47BA08F97E"),
                    Username = "user2",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "44"
                }
                ));

            tasks.Add(context.Materials.AddRangeAsync(
                new Material
                {
                    Name = "Tubo de PVC",
                    Description = "description",
                    Unity = "Metro",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Material
                {
                    Name = "Cabo de cobre 2,5mm²",
                    Description = "description",
                    Unity = "Metro",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
                ));

            tasks.Add(context.Brands.AddRangeAsync(
                new Brand()
                {
                    Name = "Tigre",
                    Description = "description",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Brand()
                {
                    Name = "CobreFlex",
                    Description = "description",
                }
                ));

            tasks.Add(context.Providers.AddRangeAsync(
                new Provider()
                {
                    Id = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                    Name = "Ponto do Encanador",
                    Description = "description",
                    Cnpj = "02.624.999/0001-23",
                    Email = "ponto@email.com",
                    Site = "Site.com",
                    Phone = "Phone",
                    Address = new Address("Dom Pedro", "Piracicaba", "Sao Paulo", "Brasil", "99999-000")
                }
                ));
            Task.WaitAll(tasks.ToArray());
            context.SaveChanges();
        }
    }
}








