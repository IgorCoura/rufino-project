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
        private Guid? _roleId;
        private Guid? _workPlaceId;
        private Guid _companyId;

        public string Registration { get; private set; } = string.Empty; //Matricula Esocial
        public Name Name { get; set; } = string.Empty;
        public Picture Picture { get; set; } = null!;
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
                    throw new DomainException(DomainErrors.FieldNotBeDefaultValue(nameof(CompanyId), Guid.Empty.ToString()));
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
        public List<Dependent> Dependents { get; private set; } = [];
        public Status Status { get; private set; } = null!;
        public SocialIntegrationProgram? Sip { get;  set; }
        public MedicalExam? MedicalExam { get; set; }
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
            private set
            {
                if (value != null)
                {
                    _voteId = value;
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
                }
            }
        }
        private Employee() { }
        private Employee(Guid id, Guid companyId, Name name, Status status) : base(id)
        {
            Name = name;
            Picture = Picture.Create();
            CompanyId = companyId;
            Status = status;
        }

        public static Employee Create(Guid id, Guid companyId, Name name)
        {
            return new(id, companyId, name, Status.Pending);
        }

        public void AddDependent(Dependent dependent)
        {
            Dependents.Add(dependent);
        }

        public File AddIdCardFile(FileExtesion extesionFile)
        {
            if (IdCard == null)
                throw new DomainException(DomainErrors.FieldNotBeNull(nameof(IdCard)));
            File file = CreateFile(extesionFile, nameof(IdCard));
            IdCard.AddFile(file);
            return file;
        }
        public File AddVoteIdFile(FileExtesion extesionFile)
        {
            if (VoteId == null)
                throw new DomainException(DomainErrors.FieldNotBeNull(nameof(VoteId)));
            File file = CreateFile(extesionFile, nameof(VoteId));
            VoteId.AddFile(file);
            return file;
        }

        public File AddDriversLicense(FileExtesion extesionFile)
        {
            if (DriversLicense == null)
                throw new DomainException(DomainErrors.FieldNotBeNull(nameof(DriversLicense)));
            File file = CreateFile(extesionFile, nameof(DriversLicense));
            DriversLicense.AddFile(file);
            return file;
        }

        public Image AddPictureImage(ImageExtesion extesionFile)
        {
            var imageName = Guid.NewGuid().ToString();
            var path = CreateFileStoragePath(nameof(Picture));
            Image image = Image.Create(path, imageName, extesionFile);
            Picture.AddImage(image);
            return image;
        }

        public File AddDepedentIdCardFile(Guid depedentId, FileExtesion extesionFile)
        {
            var dependent = Dependents.FirstOrDefault(x => x.Id == depedentId) 
                ?? throw new DomainException(DomainErrors.ListItemNotFound(nameof(Dependents), depedentId.ToString()));
            
            if (dependent.IdCard == null)
                throw new DomainException(DomainErrors.FieldNotBeNull(nameof(IdCard)));

            File file = CreateFile(extesionFile, nameof(dependent.IdCard));
            dependent.IdCard.AddFile(file);
            return file;
        }

        public File AddDepedentTestimonialFile(Guid depedentId, FileExtesion extesionFile)
        {
            var dependent = Dependents.FirstOrDefault(x => x.Id == depedentId)
                ?? throw new DomainException(DomainErrors.ListItemNotFound(nameof(Dependents), depedentId.ToString()));                

            File file = CreateFile(extesionFile, nameof(dependent.Testimonial));
            dependent.Testimonial.AddFile(file);
            return file;
        }



        private File CreateFile(FileExtesion extesionFile, string valueObjectName)
        {
            var fileName = Guid.NewGuid().ToString();
            var path = CreateFileStoragePath(valueObjectName);
            File file = File.Create(path, fileName, extesionFile);
            return file;
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
