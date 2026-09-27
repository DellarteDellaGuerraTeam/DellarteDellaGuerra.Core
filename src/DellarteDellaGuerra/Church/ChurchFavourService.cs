using DellarteDellaGuerra.Domain.Church.Favour;
using DellarteDellaGuerra.Domain.Church.Port;

namespace DellarteDellaGuerra.Church
{
    public class ChurchFavourService
    {
        private readonly IChurchLedgerWorld _world;

        public ChurchFavourService(IChurchLedgerWorld world)
        {
            _world = world;
        }

        public ChurchFavourProgress GetStatus()
        {
            return ChurchFavourPolicy.GetProgress(_world.GetLivingClergyRelations());
        }
    }
}
