namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events
{
    public class EmployeeEvent : INotification
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }

        private EmployeeEvent(int id, string name, Guid employeeId, Guid companyId)
        {
            Id = id;
            Name = name;
            EmployeeId = employeeId;
            CompanyId = companyId;
        }


        // Event IDs: from 1 to 1000, cannot be greater to avoid conflict with the RecurringEvents of the RequireDocuments
        public const int CREATED_EVENT = 1;
        public const int NAME_CHANGE_EVENT = 2;
        public const int ROLE_CHANGE_EVENT = 3;
        public const int WORKPLACE_CHANGE_EVENT = 4;
        public const int ADDRESS_CHANGE_EVENT = 5;
        public const int CONTACT_CHANGE_EVENT = 6;
        public const int MEDICAL_ADMISSION_EXAM_CHANGE_EVENT = 7;
        public const int PERSONAL_INFO_CHANGE_EVENT = 8;
        public const int ID_CARD_CHANGE_EVENT = 9;
        public const int VOTE_ID_CHANGE_EVENT = 10;
        public const int MILITAR_DOCUMENT_CHANGE_EVENT = 11;
        public const int COMPLETE_ADMISSION_EVENT = 12;
        public const int DEPENDENT_CHILD_CHANGE_EVENT = 13;
        public const int DEPENDENT_SPOUSE_CHANGE_EVENT = 14;
        public const int DEPENDENT_REMOVED_EVENT = 14;
        public const int FINISHED_CONTRACT_EVENT = 15;
        public const int DEMISSIONAL_EXAM_REQUEST_EVENT = 16;
        public const int DOCUMENT_SIGNING_OPTIONS_CHANGE_EVENT = 17;



        public static EmployeeEvent CreatedEvent(Guid employeeId, Guid companyId) => new(CREATED_EVENT, nameof(CreatedEvent), employeeId, companyId);
        public static EmployeeEvent NameChangeEvent(Guid employeeId, Guid companyId) => new(NAME_CHANGE_EVENT, nameof(NameChangeEvent), employeeId, companyId);
        public static EmployeeEvent RoleChangeEvent(Guid employeeId, Guid companyId) => new(ROLE_CHANGE_EVENT, nameof(RoleChangeEvent), employeeId, companyId);
        public static EmployeeEvent WorkPlaceChangeEvent(Guid employeeId, Guid companyId) => new(WORKPLACE_CHANGE_EVENT, nameof(WorkPlaceChangeEvent), employeeId, companyId);
        public static EmployeeEvent AddressChangeEvent(Guid employeeId, Guid companyId) => new(ADDRESS_CHANGE_EVENT, nameof(AddressChangeEvent), employeeId, companyId);
        public static EmployeeEvent ContactChangeEvent(Guid employeeId, Guid companyId) => new(CONTACT_CHANGE_EVENT, nameof(ContactChangeEvent), employeeId, companyId);
        public static EmployeeEvent MedicalAdmissionExamChangeEvent(Guid employeeId, Guid companyId) => new(MEDICAL_ADMISSION_EXAM_CHANGE_EVENT, nameof(MedicalAdmissionExamChangeEvent), employeeId, companyId);
        public static EmployeeEvent PersonalInfoChangeEvent(Guid employeeId, Guid companyId) => new(PERSONAL_INFO_CHANGE_EVENT, nameof(PersonalInfoChangeEvent), employeeId, companyId);
        public static EmployeeEvent IdCardChangeEvent(Guid employeeId, Guid companyId) => new(ID_CARD_CHANGE_EVENT, nameof(IdCardChangeEvent), employeeId, companyId);
        public static EmployeeEvent VoteIdChangeEvent(Guid employeeId, Guid companyId) => new(VOTE_ID_CHANGE_EVENT, nameof(VoteIdChangeEvent), employeeId, companyId);
        public static EmployeeEvent MilitarDocumentChangeEvent(Guid employeeId, Guid companyId) => new(VOTE_ID_CHANGE_EVENT, nameof(VoteIdChangeEvent), employeeId, companyId);
        public static EmployeeEvent CompleteAdmissionEvent(Guid employeeId, Guid companyId) => new(COMPLETE_ADMISSION_EVENT, nameof(CompleteAdmissionEvent), employeeId, companyId);
        public static EmployeeEvent DependentChildChangeEvent(Guid employeeId, Guid companyId) => new(DEPENDENT_CHILD_CHANGE_EVENT, nameof(DependentChildChangeEvent), employeeId, companyId);
        public static EmployeeEvent DependentSpouseChangeEvent(Guid employeeId, Guid companyId) => new(DEPENDENT_SPOUSE_CHANGE_EVENT, nameof(DependentSpouseChangeEvent), employeeId, companyId);
        public static EmployeeEvent DependentRemovedEvent(Guid employeeId, Guid companyId) => new(DEPENDENT_REMOVED_EVENT, nameof(DependentRemovedEvent), employeeId, companyId);
        public static EmployeeEvent FinishedContractEvent(Guid employeeId, Guid companyId) => new(FINISHED_CONTRACT_EVENT, nameof(FinishedContractEvent), employeeId, companyId);
        public static EmployeeEvent DemissionalExamRequestEvent(Guid employeeId, Guid companyId) => new(DEMISSIONAL_EXAM_REQUEST_EVENT, nameof(DemissionalExamRequestEvent), employeeId, companyId);
        public static EmployeeEvent DocumentSigningOptionsChangeEvent(Guid employeeId, Guid companyId) => new(DOCUMENT_SIGNING_OPTIONS_CHANGE_EVENT, nameof(DocumentSigningOptionsChangeEvent), employeeId, companyId);

        public static IEnumerable<EmployeeEvent?> GetAll()
        {
            var methods = GetAllMethods().Where(m => m.GetParameters().Length == 2 &&
                                             m.GetParameters()[0].ParameterType == typeof(Guid) &&
                                             m.GetParameters()[1].ParameterType == typeof(Guid));

            var result = methods.Select(x => x.Invoke(null, [Guid.Empty, Guid.Empty]) as EmployeeEvent ?? null);
            return result;
        }
        public static EmployeeEvent? FromValue(int value)
        {
            var methods = GetAllMethods();
            var objects = new List<EmployeeEvent?>();
            foreach (var method in methods)
            {
                try
                {
                    var result = method.Invoke(null, [Guid.Empty, Guid.Empty]) as EmployeeEvent ?? null;
                    objects.Add(result);
                }
                catch
                {
                    continue;
                }
            }

            var fileEvent = objects.FirstOrDefault(x => x!.Id == value);

            return fileEvent;
        }

        public static bool EventExist(int value)
        {
            var obj = EmployeeEvent.FromValue(value);
            if (obj != null)
                return true;
            return false;
        }

        private static IEnumerable<MethodInfo> GetAllMethods() =>
            typeof(EmployeeEvent).GetMethods(BindingFlags.Public | BindingFlags.Static);

    }
}
