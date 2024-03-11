using System;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.DadgCampaign.Behaviours
{
    /**
     * <summary>
     * Adds a parent to noble orphaned children especially to John Egremont, Mary Woodville and Catherine Woodville.
     * <br/>
     * The family tree of those 3 orphan children has been reworked so that these heroes are no longer orphans.
     * <br/>
     * But it requires a new game to take effect.
     * <br/>
     * So in order to fix this on existing saves, we need to temporarily add a parent to these heroes.
     * </summary>
     */
    [Obsolete("This class will be removed in version e2.0.0")]
    public class NobleOrphanChildrenCampaignBehaviour : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, AddParentToNobleOrphanChildren);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void AddParentToNobleOrphanChildren()
        {
            Hero.AllAliveHeroes.ForEach(hero =>
            {
                if (hero.Template is null && hero.IsChild && hero.Father is null && hero.Mother is null)
                {
                    SetClanLeaderAsParent(hero);
                }
            });
        }

        private void SetClanLeaderAsParent(Hero? hero)
        {
            var leader = hero?.Clan?.Leader;
            if (hero is null || leader is null)
            {
                return;
            }
            if (leader.IsFemale)
            {
                hero.Mother = leader;
                return;
            }
            hero.Father = leader;
        }
    }
}
