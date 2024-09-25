using System.Linq;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events
{
    public record RequestFilesEvent : INotification
    {

        public int Id { get; private set; }
        public string Name { get; private set; }
        public Guid OwnerId { get; private set; }
        public Guid CompanyId { get; private set; }

        private RequestFilesEvent(int id, string name, Guid ownerId, Guid companyId)
        {
            Id = id;
            Name = name;
            OwnerId = ownerId;
            CompanyId = companyId;
        }

        public const int ADMISSION_FILES = 1;
        public const int COMPLETE_ADMISSION_FILES = 2;
        public const int CHILD_DOCUMENT = 3;
        public const int MILITAR_DOCUMENT = 4;
        public const int SPOUSE_DOCUMENT = 5;
        public const int MEDICAL_DISMISSAL_EXAM = 6;

        public static RequestFilesEvent AdmissionFiles(Guid ownerId, Guid companyId) => new(ADMISSION_FILES, nameof(AdmissionFiles), ownerId, companyId);
        public static RequestFilesEvent CompleteAdmissionFiles(Guid ownerId, Guid companyId) => new(COMPLETE_ADMISSION_FILES, nameof(CompleteAdmissionFiles), ownerId, companyId);
        public static RequestFilesEvent ChildDocument(Guid ownerId, Guid companyId) => new(CHILD_DOCUMENT, nameof(ChildDocument), ownerId, companyId);
        public static RequestFilesEvent MilitarDocument(Guid ownerId, Guid companyId) => new(MILITAR_DOCUMENT, nameof(MilitarDocument), ownerId, companyId);
        public static RequestFilesEvent SpouseDocument(Guid ownerId, Guid companyId) => new(SPOUSE_DOCUMENT, nameof(SpouseDocument), ownerId, companyId);
        public static RequestFilesEvent MedicalDismissalExam(Guid ownerId, Guid companyId) => new(MEDICAL_DISMISSAL_EXAM, nameof(MedicalDismissalExam), ownerId, companyId);

        public static IEnumerable<MethodInfo> GetAll() =>
            typeof(RequestFilesEvent).GetMethods(BindingFlags.Public | BindingFlags.Static);
        public static RequestFilesEvent? FromValue(int value)
        {
            var methods = GetAll();
            var objects = new List<RequestFilesEvent?>();
            foreach(var method in methods)
            {
                try
                {
                    var result = method.Invoke(null, new object[] { Guid.Empty, Guid.Empty }) as RequestFilesEvent ?? null;
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
            var obj = RequestFilesEvent.FromValue(value);
            if (obj != null)
                return true;
            return false;
        }


    }
}


