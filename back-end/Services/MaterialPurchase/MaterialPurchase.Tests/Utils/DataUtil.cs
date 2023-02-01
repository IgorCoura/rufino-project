using Commom.Domain.SeedWork;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Infra.Context;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MaterialPurchase.Tests.Utils
{
    public static class DataUtil
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
                    Id = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
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
                    Id = Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                    Name = "Tubo de PVC",
                    Description = "description",
                    Unity = "Metro",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Material
                {
                    Id = Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"),
                    Name = "Cabo de cobre 2,5mm²",
                    Description = "description",
                    Unity = "Metro",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
                )); ;

            tasks.Add(context.Brands.AddRangeAsync(
                new Brand()
                {
                    Id = Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                    Name = "Tigre",
                    Description = "description",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Brand()
                {
                    Id = Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"),
                    Name = "CobreFlex",
                    Description = "description",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
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

            tasks.Add(context.Purchases.AddRangeAsync(
                new Purchase()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Id = Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A"),
                    ProviderId = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                    ConstructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                    Freight = 11,
                    Status = Domain.Enum.PurchaseStatus.Open,
                    AuthorizationUserGroups = new List<PurchaseAuthUserGroup>()
                    {
                        new PurchaseAuthUserGroup()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Priority= 1,
                            UserAuthorizations = new List<PurchaseUserAuthorization>()
                            {
                                new PurchaseUserAuthorization()
                                {
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    UserId = Guid.Parse("FDEC4D71-4300-4F5D-8146-9C3E8D62528B"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        },
                        new PurchaseAuthUserGroup()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Priority= 2,
                            UserAuthorizations = new List<PurchaseUserAuthorization>()
                            {
                                new PurchaseUserAuthorization()
                                {
                                    CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now,
                                    UserId = Guid.Parse("59C7F554-38E6-4C13-BB11-FE47BA08F97E"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        },
                    },
                    Materials = new List<ItemMaterialPurchase>()
                    {
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("005D1FEF-3308-4B27-8CB2-2CE610C1E231"),
                            MaterialId=Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                            BrandId= Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                            UnitPrice=99,
                            Quantity=11

                        },
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("C2355567-D8C1-4341-A66A-E0FD974CC295"),
                            MaterialId=Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"),
                            BrandId= Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"),
                            UnitPrice=66,
                            Quantity=44

                        },
                    }
                }
                ));

            tasks.Add(context.Purchases.AddRangeAsync(
                new Purchase()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Id = Guid.Parse("da9752e8-0cd6-4127-8364-c6fa7e1d8c8a"),
                    ProviderId = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                    ConstructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                    Freight = 11,
                    Status = Domain.Enum.PurchaseStatus.Authorizing,
                    AuthorizationUserGroups = new List<PurchaseAuthUserGroup>()
                    {
                        new PurchaseAuthUserGroup()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Priority= 1,
                            UserAuthorizations = new List<PurchaseUserAuthorization>()
                            {
                                new PurchaseUserAuthorization()
                                {
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    UserId = Guid.Parse("FDEC4D71-4300-4F5D-8146-9C3E8D62528B"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Approved,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        },
                        new PurchaseAuthUserGroup()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Priority= 2,
                            UserAuthorizations = new List<PurchaseUserAuthorization>()
                            {
                                new PurchaseUserAuthorization()
                                {
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    UserId = Guid.Parse("59C7F554-38E6-4C13-BB11-FE47BA08F97E"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        },
                    },
                    Materials = new List<ItemMaterialPurchase>()
                    {
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("b8d80162-84ab-4e95-9d1b-459be818a673"),
                            MaterialId=Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                            BrandId= Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                            UnitPrice=99,
                            Quantity=11

                        },
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("b52aafee-30fc-48e4-83c1-131322bfc79e"),
                            MaterialId=Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"),
                            BrandId= Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"),
                            UnitPrice=66,
                            Quantity=44

                        },
                    }
                }
                ));

            tasks.Add(context.Purchases.AddRangeAsync(
                new Purchase()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Id = Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce"),
                    ProviderId = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                    ConstructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                    Freight = 11,
                    Status = Domain.Enum.PurchaseStatus.Blocked,
                    AuthorizationUserGroups = new List<PurchaseAuthUserGroup>()
                    {
                        new PurchaseAuthUserGroup()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Priority= 1,
                            UserAuthorizations = new List<PurchaseUserAuthorization>()
                            {
                                new PurchaseUserAuthorization()
                                {
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    UserId = Guid.Parse("FDEC4D71-4300-4F5D-8146-9C3E8D62528B"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        },
                    },
                    Materials = new List<ItemMaterialPurchase>()
                    {
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("7ea45f31-a728-463a-b59f-a972f8750d38"),
                            MaterialId=Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                            BrandId= Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                            UnitPrice=99,
                            Quantity=11

                        },

                    }
                }
                ));

            Task.WaitAll(tasks.ToArray());
            context.SaveChanges();
        }
    }
}








