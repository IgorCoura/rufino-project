 using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using static PeopleManagement.Domain.ErrorTools.ErrorsMessages.DomainErrors;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Employee : Entity, IAggregateRoot
    {
        private Image? _image = null;
        private Name _name = null!;
        private IdCard? _idCard;
        private VoteId? _voteId;
        private PersonalInfo? _personalInfo;
        private Guid _roleId;
        private Guid _workPlaceId;
        private Guid _companyId;
        private Registration? _registration = null;//Matricula Esocial
        private MilitaryDocument? _militaryDocument;
        private Address? _address = null;
        private Contact? _contact = null;
        private MedicalAdmissionExam? _medicalAdmissionExam = null;
        private DocumentSigningOptions _documentSigningOptions = DocumentSigningOptions.PhysicalSignature;
        
     
        public Registration? Registration 
        { 
            get => _registration;
            private set
            {
                _registration = value;
            }
        }  
        public Name Name 
        { 
            get => _name;
            set 
            {
                _name = value;
                AddDomainEvent(EmployeeEvent.NameChangeEvent(Id, CompanyId));
            } 
        }
        public Guid RoleId 
        {
            get => _roleId;
            set
            {
                if(value != Guid.Empty)
                {
                    _roleId = value;
                    AddDomainEvent(RequestDocumentsEvent.Create(Id, CompanyId, value));
                    AddDomainEvent(EmployeeEvent.RoleChangeEvent(Id, CompanyId));
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
        public Guid WorkPlaceId 
        {
            get => _workPlaceId;
            set
            {
                if (value != Guid.Empty)
                {
                    _workPlaceId = value;
                    AddDomainEvent(RequestDocumentsEvent.Create(Id, CompanyId, value));
                    AddDomainEvent(EmployeeEvent.WorkPlaceChangeEvent(Id, CompanyId));
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
                    AddDomainEvent(EmployeeEvent.AddressChangeEvent(Id, CompanyId));
                }
            }
        }
        public Contact? Contact 
        { 
            get => _contact; 
            set 
            {
                _contact = value;
                AddDomainEvent(EmployeeEvent.ContactChangeEvent(Id, CompanyId));
            } 
        }
        public List<Dependent> Dependents { get; private set; } = [];
        public Status Status { get; private set; } = null!;
        public MedicalAdmissionExam? MedicalAdmissionExam 
        {
            get => _medicalAdmissionExam;
            set
            {
                if(value != null)
                {
                    _medicalAdmissionExam = value;
                    AddDomainEvent(EmployeeEvent.MedicalAdmissionExamChangeEvent(Id, CompanyId));
                }

            } 
        }
        public List<EmploymentContract> Contracts { get; private set; } = [];
        public PersonalInfo? PersonalInfo 
        {
            get => _personalInfo;
            set 
            {
                _personalInfo = value;
                AddDomainEvent(EmployeeEvent.PersonalInfoChangeEvent(Id, CompanyId));
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
                    AddDomainEvent(EmployeeEvent.IdCardChangeEvent(Id, CompanyId));
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
                    AddDomainEvent(EmployeeEvent.VoteIdChangeEvent(Id, CompanyId));
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
                    AddDomainEvent(EmployeeEvent.MilitarDocumentChangeEvent(Id, CompanyId));
                }
            }
        }

        public DocumentSigningOptions DocumentSigningOptions
        {
            get => _documentSigningOptions;
            set
            {
                if(value == DocumentSigningOptions.DigitalSignatureAndWhatsapp && CantSignByCellPhone == false)
                {
                    throw new DomainException(this, DomainErrors.Employee.EmployeeCantSignByCellPhone(Id));
                }
                _documentSigningOptions = value;
                AddDomainEvent(EmployeeEvent.DocumentSigningOptionsChangeEvent(Id, CompanyId));
            }
        }

        public void SetImage(Extension extension)
        {
            _image = Image.Create($"employee-image-{Id}.", extension);
        }

        public Image GetImage()
        {
            if (_image == null)
                throw new DomainException(this, DomainErrors.Employee.ImageNotSet(Id));
            return _image;
        }

        private Employee() { }
        private Employee(Guid id, Guid companyId, Name name, Guid roleId, Guid workPlaceId, Status status) : base(id)
        {
            CompanyId = companyId;
            Name = name;
            RoleId = roleId;
            WorkPlaceId = workPlaceId;
            Status = status;
        }

        public static Employee Create(Guid id, Guid companyId, Name name, Guid roleId, Guid workPlaceId)
        {
            
            Employee employee = new(id, companyId, name, roleId, workPlaceId, Status.Pending);
            employee.AddDomainEvent(EmployeeEvent.CreatedEvent(id, companyId));
            return employee;
        }

        public void CompleteAdmission(Registration registration, DateOnly dateInit, EmploymentContractType contractType)
        {
            var result = ThereNotPendingIssues();

            if (result.IsFailure)
                throw new DomainException(this, result);

            if (Status != Status.Inactive && Status != Status.Pending)
                throw new DomainException(this, DomainErrors.Employee.StatusInvalido());

            NewContract(dateInit, contractType);
            Registration = registration;            
            AddDomainEvent(EmployeeEvent.CompleteAdmissionEvent(Id,CompanyId));
        }

        public Result ThereNotPendingIssues()
        {
            var result = Result.Success(this.GetType().Name);

            if (RoleId == Guid.Empty)
                result.AddError(DomainErrors.FieldIsRequired(nameof(RoleId)));

            if (WorkPlaceId == Guid.Empty)
                result.AddError(DomainErrors.FieldIsRequired(nameof(WorkPlaceId)));

            if (_image == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(Image)));

            if (Address == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(Address)));

            if (PersonalInfo == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(PersonalInfo)));

            if (IdCard == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(IdCard)));

            if (Contact == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(Contact)));

            if (VoteId == null)
                result.AddError(DomainErrors.FieldIsRequired(nameof(VoteId)));

            //if (IdCard != null && PersonalInfo != null && MilitaryDocument.IsRequired(IdCard, PersonalInfo) && MilitaryDocument == null)
            //    result.AddError(DomainErrors.FieldIsRequired(nameof(MilitaryDocument)));

            if (MedicalAdmissionExam == null || !MedicalAdmissionExam.IsValid)
                result.AddError(DomainErrors.FieldIsRequired(nameof(MedicalAdmissionExam)));

            return result;
        }

        public void AddDependent(Dependent dependent)
        {
            Dependents.Add(dependent);
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

        public void RemoveDependent(Name nameDependent)
        {
            var index = Dependents.FindIndex(x => x.Name.Equals(nameDependent));

            if (index == -1)
                throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Dependent), nameDependent.Value));

            Dependents.RemoveAt(index);
            AddDomainEvent(EmployeeEvent.DependentRemovedEvent(Id, CompanyId));
        }


        public void FinishedContract(DateOnly finishDateContract)
        {
            var index = Contracts.FindIndex(x => x.FinalDate == null);

            if (index == -1)
                throw new DomainException(this, DomainErrors.Employee.NotExistOpenContract());

            Contracts[index] = Contracts[index].FinshedContract(finishDateContract);
            Status = Status.Inactive;
            AddDomainEvent(EmployeeEvent.FinishedContractEvent(Id, CompanyId));
            if (MedicalAdmissionExam!.NeedDismissalExam)
                AddDomainEvent(EmployeeEvent.DemissionalExamRequestEvent(Id, CompanyId));
        }

        public bool CantSignByCellPhone => !(Contact?.CellPhoneIsEmpty ?? true);

        public bool IsRequiredMilitarDocument()
        {
            if (IdCard != null && PersonalInfo != null && MilitaryDocument.IsRequired(IdCard, PersonalInfo))
            {
                return true;
            }
            return false;
        }

        public bool IsAssociation(Guid associationId)
        {
            var properties = typeof(Employee).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead)
                   .ToList();

            bool hasMatchingProperty = properties.Any(property =>
            {
                var value = property.GetValue(this);
                return value is Guid guidValue && guidValue == associationId;
            });

            return hasMatchingProperty;
        }

        public Guid[] GetAllPossibleAssociationIds()
        {
            var list = new List<Guid>();
            list.Add(RoleId);
            list.Add(WorkPlaceId);
            return list.ToArray();
        }

        private void NewContract(DateOnly dateInit, EmploymentContractType contractType)
        {
            if (Contracts.Any(x => x.FinalDate == null))
                throw new DomainException(this, DomainErrors.Employee.AlreadyExistOpenContract());
            var contract = EmploymentContract.Create(dateInit, contractType);
            Status = Status.Active;
            Contracts.Add(contract);
        }

        private void RequestDependentDocuments(DependencyType type)
        {
            if (type.Equals(DependencyType.Spouse))
            {
                AddDomainEvent(EmployeeEvent.DependentSpouseChangeEvent(Id, CompanyId));
                return;
            }

            if (type.Equals(DependencyType.Child))
            {
                AddDomainEvent(EmployeeEvent.DependentChildChangeEvent(Id, CompanyId));
                return;
            }
        }


    }
}
