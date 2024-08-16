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

        public static RequestFilesEvent AdmissionFiles(Guid ownerId, Guid companyId) => new(1, nameof(AdmissionFiles), ownerId, companyId);
        public static RequestFilesEvent CompleteAdmissionFiles(Guid ownerId, Guid companyId) => new(2, nameof(CompleteAdmissionFiles), ownerId, companyId);
        public static RequestFilesEvent ChildDocument(Guid ownerId, Guid companyId) => new(3, nameof(ChildDocument), ownerId, companyId);
        public static RequestFilesEvent MilitarDocument(Guid ownerId, Guid companyId) => new(4, nameof(MilitarDocument), ownerId, companyId);
        public static RequestFilesEvent SpouseDocument(Guid ownerId, Guid companyId) => new(5, nameof(SpouseDocument), ownerId, companyId);
        public static RequestFilesEvent MedicalDismissalExam(Guid ownerId, Guid companyId) => new(6, nameof(MedicalDismissalExam), ownerId, companyId);

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


