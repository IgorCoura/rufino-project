using PeopleManagement.Domain.Events;
using PeopleManagement.Domain.Exceptions;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Employee : Entity, IAggregateRoot
    {
        private IdCard? _idCard;
        private VoteId? _voteId;
        private DriversLicense? _driversLicense;
        private PersonalInfo? _personalInfo;
        private Guid _roleId;
        private Guid _workPlaceId;
        private Guid _companyId;
        public Name Name { get; set; }
        public Picture Picture { get; set; }
        public Guid RoleId 
        {
            get => _roleId;
            set
            {
                if(value != Guid.Empty)
                {
                    _roleId = value;
                    SendCreateRequestMedicalExamEvent();
                }
            }
        }
        public Guid CompanyId 
        {
            get => _companyId;
            private set
            {
                if (value == Guid.Empty)
                    throw new DomainException(DomainErrors.ObjectNotBeDefaultValue(nameof(CompanyId), Guid.Empty.ToString()));
                _companyId = value;
            }
        }
        public Guid WorkPlaceId 
        {
            get => _workPlaceId;
            set
            {
                if (value != Guid.Empty)
                {
                    _workPlaceId = value;
                    SendCreateRequestMedicalExamEvent();
                }
            }
        }
        public Address? Address { get; set; }
        public Contact? Contact { get; set; }
        public List<Dependent> Dependents { get; private set; } = [];
        public Status Status { get; private set; }
        public SocialIntegrationProgram? Sip { get;  set; }
        public MedicalExam? MedicalExam { get; set; }
        public PersonalInfo? PersonalInfo 
        {
            get => _personalInfo;
            set 
            { 
                _personalInfo = value;
                SendCreateRequestMedicalExamEvent();
            }
        }
        public IdCard? IdCard 
        {
            get => _idCard;
            set
            {
                if (value != null)
                {
                    _idCard = value;
                    AddDomainEvent(new CreateArchiveDomainEvent(value.ArchiveId, CompanyId, CreateArchiveStoragePath(nameof(IdCard))));
                    SendCreateRequestMedicalExamEvent();
                }
                    
            }
        }        
        public VoteId? VoteId 
        {
            get => _voteId;
            private set
            {
                if (value != null)
                {
                    _voteId = value;
                    AddDomainEvent(new CreateArchiveDomainEvent(value.ArchiveId, CompanyId, CreateArchiveStoragePath(nameof(VoteId))));
                }
            }
        }
        public DriversLicense? DriversLicense 
        {
            get => _driversLicense;
            private set
            {
                if (value != null)
                {
                    _driversLicense = value;
                    AddDomainEvent(new CreateArchiveDomainEvent(value.ArchiveId, CompanyId, CreateArchiveStoragePath(nameof(DriversLicense))));
                }
            }
        }

        private Employee(Guid id, Guid companyId, Name name, Status status) : base(id)
        {
            Name = name;
            Picture = Guid.NewGuid();
            CompanyId = companyId;
            Status = status;
            AddDomainEvent(new CreateArchiveDomainEvent(Picture.ArchiveId, CompanyId, CreateArchiveStoragePath(nameof(Picture))));
        }

        public static Employee Create(Guid id, Guid companyId, Name name)
        {
            return new(id, companyId, name, Status.Pending);
        }

        public void AddDependent(Dependent dependent)
        {
            Dependents.Add(dependent);
        }

        private string CreateArchiveStoragePath(string archiveName)
        {
            return Path.Combine(Id.ToString(), archiveName);
        }

        private void SendCreateRequestMedicalExamEvent()
        {
            if(    IdCard != null 
                && PersonalInfo != null
                && WorkPlaceId != Guid.Empty
                && RoleId != Guid.Empty
                )
            {
                AddDomainEvent(new CreateRequestMedicalExamEvent(PersonalInfo, IdCard,  WorkPlaceId!, RoleId));
            }
        }
    }
}
