﻿using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.Events;

namespace PeopleManagement.Domain.AggregatesModel.EmployeeAggregate
{
    public sealed class Employee : Entity, IAggregateRoot
    {
        public const int MAX_CHARS_REGISTRATION = 15;

        private IdCard? _idCard;
        private VoteId? _voteId;
        private PersonalInfo? _personalInfo;
        private Guid? _roleId;
        private Guid? _workPlaceId;
        private Guid _companyId;
        private string _registration = string.Empty;//Matricula Esocial
        private MilitaryDocument? _militaryDocument;

        public string Registration 
        { 
            get => _registration;
            private set
            {
                value = value.ToUpper().Trim();

                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeNullOrEmpty(nameof(Registration)));

                if (value.Length > MAX_CHARS_REGISTRATION)
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldCannotBeLarger(nameof(Registration), MAX_CHARS_REGISTRATION));

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
                    throw new DomainException(this.GetType().Name, DomainErrors.FieldNotBeDefaultValue(nameof(CompanyId), Guid.Empty.ToString()));
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
        public Address? Address { get; set; }
        public Contact? Contact { get; set; }
        public IEnumerable<Dependent> Dependents { get; private set; } = [];
        public Status Status { get; private set; } = null!;
        public SocialIntegrationProgram? Sip { get;  set; }
        public IEnumerable<MedicalExam> MedicalExams { get; private set; } = [];
        public IEnumerable<EmploymentContract> EmploymentContracts { get; private set; } = [];
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

            return new(id, companyId, name, Status.Pending);
        }

        public void CompleteAdmission(string registration, EmploymentContactType contractType)
        {
            var result = ThereNotPendingIssues();

            if (result.IsFailure)
                throw new DomainException(this.GetType().Name, result);

            var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
            var contract = EmploymentContract.Create(dateNow, contractType);
            EmploymentContracts = EmploymentContracts.Append(contract);
            Registration = registration;
            Status = Status.Active;
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

            if (!MedicalExams.Any(x => x.IsValid))
                result.AddError(DomainErrors.FieldIsRequired(nameof(MedicalExams)));

            return result;
        }

        public void AddDependent(Dependent dependent)
        {
            Dependents = Dependents.Append(dependent);
        }


        public void AddMedicalExam(MedicalExam exam)
        {
            MedicalExams = MedicalExams.Append(exam);
        }

        private string CreateFileStoragePath(string archiveName)
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
                AddDomainEvent(new CreateRequestMedicalExamEvent(PersonalInfo, IdCard, (Guid)WorkPlaceId!, (Guid)RoleId!));
            }
        }
    }
}