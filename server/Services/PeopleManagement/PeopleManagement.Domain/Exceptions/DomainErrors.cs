namespace PeopleManagement.Domain.Exceptions
{
    public static class DomainErrors
    {
        #region StringsErros 10-99
        public static ErrorModel FieldInvalid(string nameField, string value) => new ("10", $"O campo {nameField} com o valor {value} é invalido.");
        public static ErrorModel FieldCannotBeLarger(string nameField, int length) => new ("11", $"O campo {nameField}, não pode ser maior que {length}.");
        public static ErrorModel FieldCannotBeSmaller(string nameField, int length) => new ("12", $"O campo {nameField}, não pode ser menor que {length}.");
        public static ErrorModel FieldMustHaveLengthBetween(string nameField, int length) => new ("13", $"O campo {nameField}, não pode ser menor que {length}.");
        public static ErrorModel FieldNotBeNull(string nameField) => new ("14", $"O campo {nameField}, não pode ser nulo.");
        public static ErrorModel FieldNotBeNullOrEmpty(string nameField) => new ("15", $"O campo {nameField}, não pode ser nulo ou vazio.");
        public static ErrorModel FieldNotBeEmpty(string nameField) => new ("16", $"O campo {nameField}, não pode ser  vazio.");
        public static ErrorModel FieldCannotHaveSpecialChar(string nameField) => new ("17", $"O campo {nameField}, não pode ter caracteres especiais.");
        public static ErrorModel FieldIsFormatInvalid(string nameField) => new("18", $"O campo {nameField}, está em um formato invalido.");
        #endregion

        #region DataErrors 100-199

        public static ErrorModel DataInvalid(string nameField, DateTime value) => new("100", $"O data {nameField} com o valor {value} é invalido.");
        public static ErrorModel DataInvalid(string nameField, DateOnly value) => new("101", $"O data {nameField} com o valor {value} é invalido.");
        public static ErrorModel DataIsGreaterThanMax(string nameField, DateOnly value,  DateOnly dateMax) => new("102", $"A data do campo {nameField} com valor {value} não pode ser maior que a data {dateMax}.");
        public static ErrorModel DataIsLessThanMin(string nameField, DateOnly value,  DateOnly dateMin) => new("103", $"A data do campo {nameField} com valor {value} não pode ser menor que a data {dateMin}.");
        public static ErrorModel DataHasBeBetween(string nameField, DateOnly value,  DateOnly dateMin, DateOnly dateMax) => new("104", $"A data do campo {nameField} com valor {value} deve está entre {dateMin} e {dateMax}.");

        #endregion

        #region DataErrors 200-299
        public static ErrorModel ListHasObjectDuplicate(string nameList) => new("201", $"A lista {nameList} contém objetos duplicados.");
        public static ErrorModel ListNotBeNullOrEmpty(string nameField) => new("202", $"A lista {nameField}, não pode ser nulo ou vazio.");

        #endregion

        #region ObjectesErrors 300-399

        public static ErrorModel ObjectNotBeDefaultValue(string nameField, string valueDefault) => new("301", $"O campo {nameField}, não pode ter o valor padrão {valueDefault}.");

        #endregion
    }
}
