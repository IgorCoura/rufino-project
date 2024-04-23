using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Domain.Events
{
    public record CreateRequestMedicalExamEvent : INotification
    {

        public PersonalInfo PersonalInfo { get; private set; }
        public IdCard IdCard { get; private set; }
        public Guid WorkPlaceId { get; private set; }
        public Guid RoleId { get; private set; }

        public CreateRequestMedicalExamEvent(PersonalInfo personalInfo, IdCard idCard, Guid workPlaceId, Guid roleId)
        {
            PersonalInfo = personalInfo;
            IdCard = idCard;
            WorkPlaceId = workPlaceId;
            RoleId = roleId;
        }


    }
}
