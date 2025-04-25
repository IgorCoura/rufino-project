using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using System.Collections.Generic;

namespace PeopleManagement.Domain.ErrorTools.ErrorsMessages
{
    public static class DomainErrors
    {
        //PMD = PeopleManagementDomain
        #region FieldErros 10-99
       
        public static Error FieldInvalid(string NameField, string Value) => new("PMD10", $"O campo {NameField} com o valor {Value} é invalido.", new {NameField, Value});
        public static Error FieldCannotBeLarger(string NameField, int Length) => new("PMD11", $"O campo {NameField}, não pode ser maior que {Length}.", new {NameField, Length});
        public static Error FieldCannotBeSmaller(string NameField, int Length) => new("PMD12", $"O campo {NameField}, não pode ser menor que {Length}.", new {NameField, Length});
        public static Error FieldMustHaveLengthBetween(string NameField, int Min, int Max) => new("PMD13", $"O campo {NameField}, não pode ser menor que {Min} ou maior que {Max}.", new {NameField, Min, Max});
        public static Error FieldMustHaveLengthBetween(string NameField, double Min, double Max) => new("PMD13", $"O campo {NameField}, não pode ser menor que {Min} ou maior que {Max}.", new {NameField, Min, Max});
        public static Error FieldNotBeNull(string NameField) => new("PMD14", $"O campo {NameField}, não pode ser nulo.", new {NameField});
        public static Error FieldNotBeNullOrEmpty(string NameField) => new("PMD15", $"O campo {NameField}, não pode ser nulo ou vazio.", new {NameField});
        public static Error FieldNotBeEmpty(string NameField) => new("PMD16", $"O campo {NameField}, não pode ser  vazio.", new {NameField});
        public static Error FieldCannotHaveSpecialChar(string NameField) => new("PMD17", $"O campo {NameField}, não pode ter caracteres especiais.", new {NameField});
        public static Error FieldIsFormatInvalid(string NameField) => new("PMD18", $"O campo {NameField}, está em um formato invalido.", new {NameField});        
        public static Error FieldNotBeDefaultValue(string NameField, string ValueDefault) => new("PMD19", $"O campo {NameField}, não pode ter o valor padrão {ValueDefault}.", new {NameField, ValueDefault});
        public static Error FieldIsRequired(string NameField) => new("PMD20", $"O campo {NameField}, é obrigatório.", new { NameField });
        public static Error FieldIsNotRequired(string NameField) => new("PMD21", $"O campo {NameField}, não é requirido.", new { NameField });

        #endregion

        #region DataErrors 100-199

        public static Error DataInvalid(string NameField, DateTime Value) => new("PMD100", $"O data {NameField} com o valor {Value} é invalido.", new { NameField, Value });
        public static Error DataInvalid(string NameField, DateOnly Value) => new("PMD101", $"O data {NameField} com o valor {Value} é invalido.", new { NameField, Value });
        public static Error DataIsGreaterThanMax(string NameField, DateOnly Value, DateOnly DateMax) => new("PMD102", $"A data do campo {NameField} com valor {Value} não pode ser maior que a data {DateMax}.", new { NameField, Value , DateMax });
        public static Error DataIsGreaterThanMax(string NameField, DateTime Value, DateTime DateMax) => new("PMD102", $"A data do campo {NameField} com valor {Value} não pode ser maior que a data {DateMax}.", new { NameField, Value , DateMax });
        public static Error DataIsLessThanMin(string NameField, DateOnly Value, DateOnly DateMin) => new("PMD103", $"A data do campo {NameField} com valor {Value} não pode ser menor que a data {DateMin}.", new { NameField, Value , DateMin });
        public static Error DataIsLessThanMin(string NameField, DateTime Value, DateTime DateMin) => new("PMD103", $"A data do campo {NameField} com valor {Value} não pode ser menor que a data {DateMin}.", new { NameField, Value , DateMin });
        public static Error DataHasBeBetween(string NameField, DateOnly Value, DateOnly DateMin, DateOnly DateMax) => new("PMD104", $"A data do campo {NameField} com valor {Value} deve está entre {DateMin} e {DateMax}.", new { NameField, Value, DateMin, DateMax});
        public static Error DataHasBeBetween(string NameField, DateTime Value, DateTime DateMin, DateTime DateMax) => new("PMD104", $"A data do campo {NameField} com valor {Value} deve está entre {DateMin} e {DateMax}.", new { NameField, Value, DateMin, DateMax});
        public static Error DataNotBeNull(string NameField) => new("PMD104", $"A data do campo {NameField} não pode ser nulo.", new { NameField});

        #endregion

        #region ListErrors 200-299

        public static Error ListHasObjectDuplicate(string NameField) => new("PMD201", $"A lista {NameField} contém objetos duplicados.", new { NameField });
        public static Error ListNotBeNullOrEmpty(string NameField) => new("PMD202", $"A lista {NameField}, não pode ser nulo ou vazio.", new { NameField });
        public static Error ListItemNotFound(string NameField, string Item) => new("PMD203", $"Na lista {NameField}, não foi possivel encontrar o item {Item}.", new { NameField, Item});


        #endregion

        #region ObjectErrors 300-399

        public static Error ObjectNotFound(string NameObject, string Value) => new("PMD301", $"{NameObject} identificado por {Value}, não foi encontrar.", new { NameObject, Value});
        public static Error ErroCreateEnumeration(string NameEnumeration, string Value) => new("PMD302", $"O {NameEnumeration} com o valor {Value}, não é aceito.", new {NameEnumeration, Value});

        #endregion

        
        public static class Employee
        {
            public static Error AlreadyExistOpenContract() => new("PMD.EMP10", $"Já existe um contrato de trabalho aberto.", new { });
            public static Error NotExistOpenContract() => new("PMD.EMP11", $"Não existe um contrato de trabalho aberto.", new { });
            public static Error HasRequiresFiles() => new("PMD.EMP12", $"Há documentos requeridos.", new { });
            public static Error EmployeeCantSignByCellPhone(Guid Id) => new("PMD.EMP13", $"O funcionario {Id} não tem um celular registrado para poder assinar contratos. .", new { Id });
            public static Error StatusInvalido() => new("PMD.EMP14", $"O funcionario está com o estado inválido para realizar está função", new { });
        }

        public static class ArchiveCategory
        {
            public static Error EventNotExist(int EventId) => new("PMD.AC10", $"O Event com id {EventId} não exist.", new { EventId });
        }

        public static class Document
        {
            public static Error TimeConflictBetweenDocuments(Guid DocIdWithConflict, Guid DocIdHasConclict, TimeSpan TimeMax) => new("PMD.DOC10", 
                $"Há um conflito de tempo gasta entre o documento sendo criado {DocIdWithConflict} e o já existente {DocIdHasConclict}." +
                $"O tempo total diaria não pode ultrapassar {TimeMax} ", new { DocIdWithConflict, DocIdHasConclict, TimeMax });
        }
    }
}
