namespace PeopleManagement.Domain.ErrorTools.ErrorsMessages
{
    public static class InfraErrors
    {
        //PMI = PeopleManagementInfra
        public static class Db
        {
            public static Error UniqueConstraint(string mensage) => new("PMI.DB101", mensage, new { });
            public static Error CannotInsertNull(string mensage) => new("PMI.DB102", mensage, new { });
            public static Error MaxLengthExceeded(string mensage) => new("PMI.DB103", mensage, new { });
            public static Error NumericOverflow(string mensage) => new("PMI.DB104", mensage, new { });
            public static Error ReferenceConstraint(string mensage) => new("PMI.DB105", mensage, new { });

        }

        public static class File
        {
            public static Error ExtesionFileInvalid(string NameObject, string Extesion) => new("PMI.FILE401", $"A extesão {Extesion} para {NameObject} é invalida", new { NameObject, Extesion });
            public static Error InvalidFile() => new("PMI.FILE402", $"O arquivo enviado está em um formato Invalido.", new { });
            public static Error FileNotFound() => new("PMI.FILE403", $"Não foi possivel encotrar o arquivo.", new { });

        }
    }
}
