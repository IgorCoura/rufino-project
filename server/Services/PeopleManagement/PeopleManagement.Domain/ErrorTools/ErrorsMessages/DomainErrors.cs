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

        public static Error DataInvalid(string NameField, DateTime Value) => new("PMD100", $"O campo data {NameField} com o valor {Value} é invalido.", new { NameField, Value });
        public static Error DataInvalid(string NameField, DateOnly Value) => new("PMD101", $"O campo data {NameField} com o valor {Value} é invalido.", new { NameField, Value });
        public static Error DataIsGreaterThanMax(string NameField, DateOnly Value, DateOnly DateMax) => new("PMD102", $"A data do campo {NameField} com valor {Value} não pode ser maior que a data {DateMax}.", new { NameField, Value , DateMax });
        public static Error DataIsGreaterThanMax(string NameField, DateTime Value, DateTime DateMax) => new("PMD102", $"A data do campo {NameField} com valor {Value} não pode ser maior que a data {DateMax}.", new { NameField, Value , DateMax });
        public static Error DataIsGreaterThanMax(string NameField, DateTimeOffset Value, DateTimeOffset DateMax) => new("PMD102", $"A data do campo {NameField} com valor {Value} não pode ser maior que a data {DateMax}.", new { NameField, Value , DateMax });
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
            public static Error EmployeeCantSignByCellPhone(Guid Id) => new("PMD.EMP13", $"O funcionario {Id} não tem um celular registrado para poder assinar por Whatsapp.", new { Id });
            public static Error StatusInvalido() => new("PMD.EMP14", $"O funcionario está com o estado inválido para realizar está função", new { });
            public static Error InvalidDocumentDigitalSigningOptions(Guid Id) => new("PMD.EMP15", $"O funcionario {Id} não tem uma opção de assinatura digital valida.", new { Id });
            public static Error ImageNotSet(Guid Id) => new("PMD.EMP16", $"O funcionario {Id} não tem uma image.", new { Id });
            public static Error CannotMarkAsInactive(string currentStatus) => new("PMD.EMP17", $"Não é possível marcar o funcionário como inativo. O status atual '{currentStatus}' não permite esta operação. Apenas funcionários com status 'Pending' podem ser marcados como inativos.", new { currentStatus });
        }

        public static class ArchiveCategory
        {
            public static Error EventNotExist(int EventId) => new("PMD.AC10", $"O Event com id {EventId} não exist.", new { EventId });
        }

        public static class Document
        {
            public static Error TimeConflictBetweenDocuments(Guid DocIdWithConflict, Guid DocIdHasConflict, TimeSpan TimeMax) => new("PMD.DOC10", 
                $"Há um conflito de tempo gasta entre o documento sendo criado {DocIdWithConflict} e o já existente {DocIdHasConflict}." +
                $"O tempo total diaria não pode ultrapassar {TimeMax} ", new { DocIdWithConflict, DocIdHasConflict, TimeMax });

            public static Error DocumentNotHaveTemplate(Guid docId) => new("PMD.DOC11", $"O documento {docId} não tem um template associado.", new { docId });
            public static Error ErrorRecoverData(Guid docId) => new("PMD.DOC12", $"Não foi possivel recuperar todos os dados para documento {docId}. Complete os dados é tente novamente mais tarde.", new { docId });
            public static Error CantEditDocumentUnit(Guid docUnitId) => new("PMD.DOC13", $"Não é mais possivel editar a unidade de documento {docUnitId}.", new { docUnitId });
            public static Error CantGenerateDocumentUnit(Guid docUnitId) => new("PMD.DOC13", $"Não é mais possivel gerar pdfs da unidade de documento {docUnitId}.", new { docUnitId });
            public static Error DocumentAlreadySentToSignature(Guid docUnitId) => new("PMD.DOC14", $"A unidade de documento {docUnitId}, já foi enviado para assinatura.", new { docUnitId });
            public static Error IsNotPending() => new("PMD.DOC15", $"Está ação só pode ser realizada quando o documento está pendente.", new {  });
            public static Error DocumentUnitMissingNameOrExtension(Guid docUnitId) => new("PMD.DOC16", $"A unidade de documento {docUnitId} não pode ser marcada como válida porque está faltando o nome ou a extensão.", new { docUnitId });
            public static Error NotSignable(Guid docUnitId) => new("PMD.DOC17", $"A unidade de documento {docUnitId} não é assinavel.", new { docUnitId });

        }

        public static class DocumentTemplate
        {
            public static Error TemplateDoesNotAcceptSignature(Guid templateId) => new("PMD.DOCT10", $"O template {templateId} não aceita assinatura. Não é possível definir locais de assinatura.", new { templateId });
        }
    }
}
