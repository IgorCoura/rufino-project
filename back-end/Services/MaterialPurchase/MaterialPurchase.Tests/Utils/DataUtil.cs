using Commom.Domain.BaseEntities;
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
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
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
                    Role = "admin"

                },
                new User()
                {

                    Id = Guid.Parse("F363DA96-1EBB-419D-B178-3F7F3B54B863"),
                    Username = "Creator",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "client"

                },
                new User()
                {

                    Id = Guid.Parse("FDEC4D71-4300-4F5D-8146-9C3E8D62528B"),
                    Username = "user1",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "client"

                },
                new User()
                {

                    Id = Guid.Parse("59C7F554-38E6-4C13-BB11-FE47BA08F97E"),
                    Username = "user2",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "client"
                },
                new User()
                {

                    Id = Guid.Parse("ddf5281b-cdf7-4781-b4ad-8391f743d35c"),
                    Username = "sup1",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "client"
                }
                ));

            tasks.Add(context.Materials.AddRangeAsync(
                new Material
                {
                    Id = Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                    Name = "Tubo de PVC",
                    Unity = "Metro",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Material
                {
                    Id = Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"),
                    Name = "Cabo de cobre 2,5mm²",
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
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new Brand()
                {
                    Id = Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"),
                    Name = "CobreFlex",
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
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Approved,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
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
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
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
                                    UserId = Guid.Parse("ddf5281b-cdf7-4781-b4ad-8391f743d35c"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Supervisor,
                                }
                            }
                        },
                        new PurchaseAuthUserGroup()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Priority= 3,
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
                    Id = Guid.Parse("ae1d0df7-deed-4e3e-85ab-82bf2453c541"),
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
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
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
                            Id = Guid.Parse("6852fc38-9763-448f-a898-c5d908e8906d"),
                            MaterialId=Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                            BrandId= Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                            UnitPrice=99,
                            Quantity=11

                        },
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("7dc01270-8e70-4094-bb1c-e7d42e1c570a"),
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
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Approved,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
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
                                    UserId = Guid.Parse("ddf5281b-cdf7-4781-b4ad-8391f743d35c"),
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Supervisor,
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

            tasks.Add(context.Purchases.AddRangeAsync(
                new Purchase()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Id = Guid.Parse("3887e6ff-13a4-4665-a8e3-14632d7dd2ce"),
                    ProviderId = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                    ConstructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                    Freight = 11,
                    Status = Domain.Enum.PurchaseStatus.Approved,
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
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
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
                                    AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Approved,
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Client,
                                }
                            }
                        }
                    },
                    Materials = new List<ItemMaterialPurchase>()
                    {
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("5f13d67a-19e8-41a9-9ea7-56eb86f1ca6c"),
                            MaterialId=Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                            BrandId= Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                            UnitPrice=99,
                            Quantity=11

                        },

                    }
                }
                ));

            tasks.Add(context.Purchases.AddRangeAsync(
                new Purchase()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Id = Guid.Parse("29475890-4638-4a8b-a866-30a4b1ae2ac5"),
                    ProviderId = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                    ConstructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                    Freight = 11,
                    Status = Domain.Enum.PurchaseStatus.DeliveryProblem,
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
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
                                }
                            }
                        }
                    },
                    Materials = new List<ItemMaterialPurchase>()
                    {
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("ad4504a9-581b-4b85-99c1-3df851ec3db3"),
                            MaterialId=Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                            BrandId= Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                            UnitPrice=99,
                            Quantity=11

                        },

                    }
                }
                ));

            tasks.Add(context.Purchases.AddRangeAsync(
                new Purchase()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Id = Guid.Parse("7a694ea5-a2aa-4f38-aed3-b2fbf09cc208"),
                    ProviderId = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                    ConstructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                    Freight = 11,
                    Status = Domain.Enum.PurchaseStatus.WaitingDelivery,
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
                                    Permissions = Domain.Enum.UserAuthorizationPermissions.Creator,
                                }
                            }
                        }
                    },
                    Materials = new List<ItemMaterialPurchase>()
                    {
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("adcdf482-8def-492f-ae2a-6bcaa2a141e4"),
                            MaterialId=Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"),
                            BrandId= Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"),
                            UnitPrice=99,
                            Quantity=11

                        },
                        new ItemMaterialPurchase()
                        {
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Id = Guid.Parse("a1144b14-6005-4764-875b-3b097e6ca41c"),
                            MaterialId=Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"),
                            BrandId= Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"),
                            UnitPrice=66,
                            Quantity=44

                        },
                    }
                }
                ));

            var ids = new string[]
            {
                "1001", "1002", "1003", "1004", "1005", "1006",
                "1007", "1008", "1009", "1010", "1011", "1012",
                "1013", "1014", "1015", "1016", "1017", "1018",
            };

            var functionsIdsAdmin = ids.Select(x => new FunctionId()
            {
                Id = Guid.NewGuid(),
                Name = x
            }).ToList();

            var functionsIdsClient = functionsIdsAdmin.Select(x => x).ToList();
            functionsIdsClient.RemoveAll(x => x.Name == "1018");


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








