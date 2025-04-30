using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate
{
    public class ListenEvent : ValueObject
    {
        
        public int EventId { get; private set; }
        public List<int> Status { get; private set; } = [];

        private ListenEvent(int eventId, List<int> status)
        {
            EventId = eventId;
            Status = status;
        }

        public static ListenEvent Create(int eventId, List<int> status)
        {
            return new(eventId, status);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return EventId;
            yield return Status;
        }
    }
}
