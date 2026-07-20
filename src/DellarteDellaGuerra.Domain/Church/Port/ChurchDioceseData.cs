using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Church.Port
{
    public class ChurchDioceseData
    {
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<ChurchSettlementData> Members { get; }

        public ChurchDioceseData(string id, string name, IReadOnlyList<ChurchSettlementData> members)
        {
            Id = id;
            Name = name;
            Members = members;
        }
    }
}
