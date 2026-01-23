using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Util
{
    public static class DocumentStatusUtil
    {
        public static EnumerationDto GetDocumentGroupStatus(List<DocumentStatus> documentStatus)
        {
            if (documentStatus.Any(x => x == DocumentStatus.Warning))
            {
                return new EnumerationDto
                {
                    Id = 2,
                    Name = "Warning"
                };
            }

            if (documentStatus.Any(x => x != DocumentStatus.OK && x != DocumentStatus.AwaitingSignature))
            {
                return new EnumerationDto
                {
                    Id = 3,
                    Name = "RequiresAttention"
                };
            }

            return new EnumerationDto
            {
                Id = 1,
                Name = "Okay"
            };

        }
    }
}
