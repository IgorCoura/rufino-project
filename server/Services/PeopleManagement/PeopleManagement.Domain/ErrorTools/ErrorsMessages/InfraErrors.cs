namespace PeopleManagement.Domain.ErrorTools.ErrorsMessages
{
    public static class InfraErrors
    {
        public static class Db
        {
            public static Error UniqueConstraint(string mensage) => new("DB101", mensage, new { });
            public static Error CannotInsertNull(string mensage) => new("DB102", mensage, new { });
            public static Error MaxLengthExceeded(string mensage) => new("DB103", mensage, new { });
            public static Error NumericOverflow(string mensage) => new("DB104", mensage, new { });
            public static Error ReferenceConstraint(string mensage) => new("DB105", mensage, new { });
        }
    }
}
