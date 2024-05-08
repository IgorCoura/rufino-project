using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Domain.Events
{
    public record CreateRequestMedicalExamEvent : INotification
    {

        public PersonalInfo PersonalInfo { get; private set; }
        public IdCard IdCard { get; private set; }
        public Guid WorkPlaceId { get; private set; }
        public Guid RoleId { get; private set; }

        private CreateRequestMedicalExamEvent(PersonalInfo personalInfo, IdCard idCard, Guid workPlaceId, Guid roleId)
        {
            PersonalInfo = personalInfo;
            IdCard = idCard;
            WorkPlaceId = workPlaceId;
            RoleId = roleId;
        }


        public static CreateRequestMedicalExamEvent Create(PersonalInfo personalInfo, IdCard idCard, Guid workPlaceId, Guid roleId) => new(personalInfo, idCard, workPlaceId, roleId);

    }
}
