﻿using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.Text.Json.Nodes;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;

namespace PeopleManagement.Services.Services.RecoverInfoToDocument
{
    public class RecoverDepartamentInfoToDocumentTemplateService(IDepartmentRepository departmentRepository) : IRecoverDepartamentInfoToDocumentTemplateService
    {
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;
        public async Task<JsonObject> RecoverInfo(Guid employeeId, Guid companyId, JsonObject[]? jsonObjects = null, CancellationToken cancellation = default)
        {
            var department = await _departmentRepository.GetDepartmentFromEmployeeId(employeeId, companyId, cancellation)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Department), employeeId.ToString()));

            var departmentJson = new JsonObject
            {
                ["Department"] = new JsonObject
                {
                    ["Id"] = department.Id.ToString(),
                    ["Name"] = department.Name.ToString(),
                    ["Description"] = department.Description.ToString()
                }
            };

            return departmentJson;
        }

        public static JsonObject GetModel()
        {
            var json = new JsonObject
            {
                ["Department"] = new JsonObject
                {
                    ["Id"] = Guid.Empty.ToString(),
                    ["Name"] = "department.Name",
                    ["Description"] = "department.Description"
                }
            };

            return json;
        }
    }
}
