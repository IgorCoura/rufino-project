using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.Events;
using System.Collections.ObjectModel;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Employee : Entity, IAggregateRoot
    {       
        private IdCard? _idCard;
        private VoteId? _voteId;
        private PersonalInfo? _personalInfo;
        private Guid? _roleId;
        private Guid? _workPlaceId;
        private Guid _companyId;
        private Registration? _registration = null!;//Matricula Esocial
        private MilitaryDocument? _militaryDocument;
        private Address? _address = null!;
        private MedicalAdmissionExam? _medicalAdmissionExam = null!;

        public Registration? Registration 
        { 
            get => _registration;
            private set
            {
                _registration = value;
            }
        }  
        public Name Name { get; set; } = null!;
        public Guid? RoleId 
        {
            get => _roleId;
            set
            {
                if(value != null && value != Guid.Empty)
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
                    throw new DomainException(this, DomainErrors.FieldNotBeDefaultValue(nameof(CompanyId), Guid.Empty.ToString()));
                _companyId = value;
            }
        }
        public Guid? WorkPlaceId 
        {
            get => _workPlaceId;
            set
            {
                if (value != null && value != Guid.Empty)
                {
                    _workPlaceId = value;
                    SendCreateRequestMedicalExamEvent();
                }
            }
        }
        public Address? Address 
        {
            get => _address;
            set
            {
                if(value != null)
                {
                    _address = value;
                    AddDomainEvent(RequestDocumentsEvent.AddressProof(Id, CompanyId));
                }
            }
        }
        public Contact? Contact { get; set; }
        public List<Dependent> Dependents { get; private set; } = [];
        public Status Status { get; private set; } = null!;
        public SocialIntegrationProgram? Sip { get;  set; }
        public MedicalAdmissionExam? MedicalAdmissionExam 
        {
            get => _medicalAdmissionExam;
            set
            {
                if(value != null)
                {
                    _medicalAdmissionExam = value;
                    AddDomainEvent(RequestDocumentsEvent.MedicalAdmissionExam(Id, CompanyId));
                }

            } 
        }
        public List<EmploymentContract> Contracts { get; private set; } = new();
        public PersonalInfo? PersonalInfo 
        {
            get => _personalInfo;
            set 
            { 
                _personalInfo = value;
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
                    SendCreateRequestMedicalExamEvent();
                    AddDomainEvent(RequestDocumentsEvent.IdCard(Id, CompanyId));
                }
                    
            }
        }        
        public VoteId? VoteId 
        {
            get => _voteId;
            set
            {
                if (value != null)
                {
                    _voteId = value;
                    AddDomainEvent(RequestDocumentsEvent.VoteId(Id, CompanyId));
                }
            }
        }

        public MilitaryDocument? MilitaryDocument 
        {
            get => _militaryDocument;
            set
            {
                if (value != null)
                {
                    _militaryDocument = value;
                    AddDomainEvent(RequestDocumentsEvent.MilitarDocument(Id, CompanyId));
                }
            }
        }
        private Employee() { }
        private Employee(Guid id, Guid companyId, Name name, Status status) : base(id)
        {
            Name = name;
            CompanyId = companyId;
            Status = status;
        }

        public static Employee Create(Guid id, Guid companyId, Name name)
        {
            Employee employee = new(id, companyId, name, Status.Pending);

            return employee;
        }

        public void CompleteAdmission(Registration registration, EmploymentContactType contractType)
        {
            var result = ThereNotPendingIssues();

            if (result.IsFailure)
                throw new DomainException(this, result);
            CreateContract(contractType);
            Registration = registration;
            Status = Status.Active;
            AddDomainEvent(RequestDocumentsEvent.Contract(Id, CompanyId));
        }

        public Result ThereNotPendingIssues()
        {
            var result = Result.Success(this.GetType().Name);

            if (RoleId == null || RoleId == Guid.Empty)
                result.AddError(DomainErrors.FieldIsRequired(nameof(RoleId)));

            if (WorkPlaceId == null || WorkPlaceId == Guid.Empty)
                result.AddError(DomainErrors.FieldIsRequired(nameof(WorkPlaceId)));

            if (Address == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(Address)));

            if (Sip == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(Sip)));

            if (PersonalInfo == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(PersonalInfo)));

            if (IdCard == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(IdCard)));           

            if (VoteId == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(VoteId)));

            if (IdCard != null && PersonalInfo != null && MilitaryDocument.IsRequired(IdCard, PersonalInfo) && MilitaryDocument == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(MilitaryDocument)));

            if (MedicalAdmissionExam == null || !MedicalAdmissionExam.IsValid)
                result.AddError(DomainErrors.FieldIsRequired(nameof(MedicalAdmissionExam)));

            return result;
        }

        public void AddDependent(Dependent dependent)
        {

            Dependents = [.. Dependents, dependent];

            RequestDependentDocuments(dependent.DependencyType);
        }

        public void AlterDependet(Name nameDependent, Dependent currentDependent)
        {
            var index = Dependents.FindIndex(x => x.Name.Equals(nameDependent));

            if (index == -1)
                throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Dependent), nameDependent.Value));

            Dependents[index] = currentDependent;

            RequestDependentDocuments(currentDependent.DependencyType);
        }

        
        public void FinishedContract(DateOnly finishDateContract)
        {
            var contract = Contracts.Find(x => x.FinalDate == null) ??
                throw new DomainException(this, DomainErrors.Employee.NotExistOpenContract());

            contract = contract.FinshedContract(finishDateContract);
            Status = Status.Inactive;
            if (MedicalAdmissionExam!.NeedDismissalExam)
                AddDomainEvent(RequestDocumentsEvent.MedicalDismissalExam(Id, CompanyId)); 

        }

        private void CreateContract(EmploymentContactType contractType)
        {
            if (Contracts.Any(x => x.FinalDate == null))
                throw new DomainException(this, DomainErrors.Employee.AlreadyExistOpenContract());
            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
            var contract = EmploymentContract.Create(dateNow, contractType);
            Contracts.Add(contract);
        }

        private void RequestDependentDocuments(DependencyType type)
        {
            if (type.Equals(DependencyType.Spouse))
            {
                AddDomainEvent(RequestDocumentsEvent.SpouseDocument(Id, CompanyId));
                AddDomainEvent(RequestDocumentsEvent.MarriageCertificate(Id, CompanyId));
            }

            if (type.Equals(DependencyType.Child))
            {
                AddDomainEvent(RequestDocumentsEvent.ChildDocument(Id, CompanyId));
            }
        }

        private void SendCreateRequestMedicalExamEvent()
        {
            if(    IdCard != null 
                && PersonalInfo != null
                && WorkPlaceId != Guid.Empty
                && RoleId != Guid.Empty
                )
            {
                AddDomainEvent(CreateRequestMedicalExamEvent.Create(PersonalInfo, IdCard, (Guid)WorkPlaceId!, (Guid)RoleId!));
            }
        }
    }
}
