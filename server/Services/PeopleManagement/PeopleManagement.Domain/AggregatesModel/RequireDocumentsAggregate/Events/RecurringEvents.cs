namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events
{
    public class RecurringEvents : INotification
    {
        public int Id { get; private set; } = 0;
        public string Name { get; private set; } = string.Empty;

        public RecurringEvents()
        {

        }

        private RecurringEvents(int id, string name)
        {
            Id = id;
            Name = name;
        }



        // Event IDs: from 1000 to 2000, cannot be lesser or greater to avoid conflict.
        public const int JANUARY = 1001;
        public const int FEBRUARY = 1002;
        public const int MARCH = 1003;
        public const int APRIL = 1004;
        public const int MAY = 1005;
        public const int JUNE = 1006;
        public const int JULY = 1007;
        public const int AUGUST = 1008;
        public const int SEPTEMBER = 1009;
        public const int OCTOBER = 1010;
        public const int NOVEMBER = 1011;
        public const int DECEMBER = 1012;
        public const int DAILY = 1013;
        public const int WEEKLY = 1014;
        public const int MONTHLY = 1015;
        public const int YEARLY = 1016;

        public static RecurringEvents JanuaryEvent => new(JANUARY, nameof(JanuaryEvent));
        public static RecurringEvents FebruaryEvent => new(FEBRUARY, nameof(FebruaryEvent));
        public static RecurringEvents MarchEvent => new(MARCH, nameof(MarchEvent));
        public static RecurringEvents AprilEvent => new(APRIL, nameof(AprilEvent));
        public static RecurringEvents MayEvent => new(MAY, nameof(MayEvent));
        public static RecurringEvents JuneEvent => new(JUNE, nameof(JuneEvent));
        public static RecurringEvents JulyEvent => new(JULY, nameof(JulyEvent));
        public static RecurringEvents AugustEvent => new(AUGUST, nameof(AugustEvent));
        public static RecurringEvents SeptemberEvent => new(SEPTEMBER, nameof(SeptemberEvent));
        public static RecurringEvents OctoberEvent => new(OCTOBER, nameof(OctoberEvent));
        public static RecurringEvents NovemberEvent => new(NOVEMBER, nameof(NovemberEvent));
        public static RecurringEvents DecemberEvent => new(DECEMBER, nameof(DecemberEvent));
        public static RecurringEvents DailyEvent => new(DAILY, nameof(DailyEvent));
        public static RecurringEvents WeeklyEvent => new(WEEKLY, nameof(WeeklyEvent));
        public static RecurringEvents MonthlyEvent => new(MONTHLY, nameof(MonthlyEvent));
        public static RecurringEvents YearlyEvent => new(YEARLY, nameof(YearlyEvent));

        public static IEnumerable<RecurringEvents?> GetAll()
        {
            var methods = GetAllMethods();

            var result = methods.Select(m => m.Invoke(null, null) as RecurringEvents);
            return result;
        }
        public static RecurringEvents? FromValue(int value)
        {
            var methods = GetAllMethods();
            var objects = new List<RecurringEvents?>();
            foreach (var method in methods)
            {
                try
                {
                    var result = method.Invoke(null, null) as RecurringEvents ?? null;
                    objects.Add(result);
                }
                catch
                {
                    continue;
                }
            }

            var fileEvent = objects.FirstOrDefault(x => x!.Id == value);

            return fileEvent;
        }

        public static bool EventExist(int value)
        {
            var obj = RecurringEvents.FromValue(value);
            if (obj != null)
                return true;
            return false;
        }

        private static IEnumerable<MethodInfo> GetAllMethods() =>
            typeof(RecurringEvents).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.ReturnType == typeof(RecurringEvents) && x.GetParameters().Length == 0);

    }
    

}
