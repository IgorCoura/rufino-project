using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class RecoverDataType : Enumeration
    {
        public static readonly RecoverDataType Company = new(1, nameof(Company), "Company", typeof(IRecoverCompanyInfoToDocumentTemplateService));
        public static readonly RecoverDataType Departament = new(2, nameof(Departament), "Departament", typeof(IRecoverDepartamentInfoToDocumentTemplateService));
        public static readonly RecoverDataType Employee = new(3, nameof(Employee), "Employee", typeof(IRecoverEmployeeInfoToDocumentTemplateService));
        public static readonly RecoverDataType PGR = new(4, nameof(PGR), "PGR", typeof(IRecoverPGRInfoToDocumentTemplateService));
        public static readonly RecoverDataType Position = new(5, nameof(Position), "Position", typeof(IRecoverPositionInfoToDocumentTemplateService));
        public static readonly RecoverDataType Role = new(6, nameof(Role), "Role", typeof(IRecoverRoleInfoToDocumentTemplateService));
        public static readonly RecoverDataType Workplace = new(7, nameof(Workplace), "Role", typeof(IRecoverWorkplaceInfoToDocumentTemplateService));

        public string TemplateName { get; private set; } = null!;
        public Type Type { get; private set; } = null!;
        private RecoverDataType(int id, string name, string templateName, Type type) : base(id, name)
        {
            TemplateName = templateName;
            Type = type;
        }

        public static implicit operator RecoverDataType(int id) => Enumeration.FromValue<RecoverDataType>(id);
        public static implicit operator RecoverDataType(string name) => Enumeration.FromDisplayName<RecoverDataType>(name);

  
    }
}
