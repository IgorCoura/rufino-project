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
            public static Error ExtesionFileInvalid(string NameObject, string Extesion) => new("PMI.FILE101", $"A extesão {Extesion} para {NameObject} é invalida", new { NameObject, Extesion });
            public static Error InvalidFile() => new("PMI.FILE102", $"O arquivo enviado está em um formato Invalido.", new { });
            public static Error FileNotFound() => new("PMI.FILE103", $"Não foi possivel encotrar o arquivo.", new { });
           
        }

        public static class SignDoc
        {
            public static Error ErrorSendDocToSign(Guid DocumentUnitId) => new("PMI.SD101", $"Um erro ocorreu ao tentar enviar o documento {DocumentUnitId} para assinar.", new { DocumentUnitId });
            public static Error ErrorInRecoverDocSigned(string DocumentUnitId) => new("PMI.SD102", $"Um erro ocorreu ao tentar recuperar o documento {DocumentUnitId} assinado.", new { DocumentUnitId });
        }
    }
}
