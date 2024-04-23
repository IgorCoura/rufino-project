namespace PeopleManagement.Domain.Exceptions
{
    public static class DomainErrors
    {
        //PMD = PeopleManagementDomain
        #region FieldErros 10-99
        public static ErrorModel FieldInvalid(string nameField, string value) => new ("PMD10", $"O campo {nameField} com o valor {value} é invalido.");
        public static ErrorModel FieldCannotBeLarger(string nameField, int length) => new ("PMD11", $"O campo {nameField}, não pode ser maior que {length}.");
        public static ErrorModel FieldCannotBeSmaller(string nameField, int length) => new ("PMD12", $"O campo {nameField}, não pode ser menor que {length}.");
        public static ErrorModel FieldMustHaveLengthBetween(string nameField, int length) => new ("PMD13", $"O campo {nameField}, não pode ser menor que {length}.");
        public static ErrorModel FieldNotBeNull(string nameField) => new ("PMD14", $"O campo {nameField}, não pode ser nulo.");
        public static ErrorModel FieldNotBeNullOrEmpty(string nameField) => new ("PMD15", $"O campo {nameField}, não pode ser nulo ou vazio.");
        public static ErrorModel FieldNotBeEmpty(string nameField) => new ("PMD16", $"O campo {nameField}, não pode ser  vazio.");
        public static ErrorModel FieldCannotHaveSpecialChar(string nameField) => new ("PMD17", $"O campo {nameField}, não pode ter caracteres especiais.");
        public static ErrorModel FieldIsFormatInvalid(string nameField) => new("PMD18", $"O campo {nameField}, está em um formato invalido.");
        public static ErrorModel FieldNotBeDefaultValue(string nameField, string valueDefault) => new("PMD19", $"O campo {nameField}, não pode ter o valor padrão {valueDefault}.");
        #endregion

        #region DataErrors 100-199

        public static ErrorModel DataInvalid(string nameField, DateTime value) => new("PMD100", $"O data {nameField} com o valor {value} é invalido.");
        public static ErrorModel DataInvalid(string nameField, DateOnly value) => new("PMD101", $"O data {nameField} com o valor {value} é invalido.");
        public static ErrorModel DataIsGreaterThanMax(string nameField, DateOnly value,  DateOnly dateMax) => new("PMD102", $"A data do campo {nameField} com valor {value} não pode ser maior que a data {dateMax}.");
        public static ErrorModel DataIsLessThanMin(string nameField, DateOnly value,  DateOnly dateMin) => new("PMD103", $"A data do campo {nameField} com valor {value} não pode ser menor que a data {dateMin}.");
        public static ErrorModel DataHasBeBetween(string nameField, DateOnly value,  DateOnly dateMin, DateOnly dateMax) => new("PMD104", $"A data do campo {nameField} com valor {value} deve está entre {dateMin} e {dateMax}.");
        public static ErrorModel DataNotBeNull(string nameField) => new("PMD104", $"A data do campo {nameField} não pode ser nulo.");

        #endregion

        #region DataErrors 200-299
        public static ErrorModel ListHasObjectDuplicate(string nameList) => new("PMD201", $"A lista {nameList} contém objetos duplicados.");
        public static ErrorModel ListNotBeNullOrEmpty(string nameList) => new("PMD202", $"A lista {nameList}, não pode ser nulo ou vazio.");
        public static ErrorModel ListItemNotFound(string nameList, string item) => new("PMD203", $"Na lista {nameList}, não foi possivel encontrar o item {item}.");


        #endregion
    }
}
