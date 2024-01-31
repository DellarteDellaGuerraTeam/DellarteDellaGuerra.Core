using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace DellarteDellaGuerra.DadgCampaign;

public class CampaignBehaviourDisabler
{
    /**
     * <summary>
     *     Disables campaign behaviours that should not be used in the mod.
     *     This method is idempotent. 
     * </summary>
     * <param name="campaignBehaviorManager">The campaign behavior manager.</param>
     * <exception cref="ArgumentNullException">Thrown when campaignBehaviorManager is null.</exception>
     */
    public void Disable(ICampaignBehaviorManager campaignBehaviorManager)
    {
        if (campaignBehaviorManager == null)
        {
            throw new ArgumentNullException(nameof(campaignBehaviorManager));
        }

        // Introduces a grudge between two sandbox kings using hardcoded lord ids.
        // We do not need this in our mod.
        campaignBehaviorManager.RemoveBehavior<BackstoryCampaignBehavior>();
    }
}