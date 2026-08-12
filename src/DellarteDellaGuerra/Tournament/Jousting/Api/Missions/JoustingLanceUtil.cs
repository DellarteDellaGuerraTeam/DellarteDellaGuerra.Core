using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using Force.DeepCloner;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Missions
{
    internal class JoustingLanceUtil
    {
        private const string JoustingCouchItemUsage = "jousting_polearm_couch";

        private readonly ILogger _logger;
        private readonly MethodInfo? _itemUsageSetter;
        private readonly FieldInfo? _missionWeaponUsagesField;

        internal JoustingLanceUtil(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<JoustingLanceUtil>();

            _itemUsageSetter = typeof(WeaponComponentData).GetMethod("set_ItemUsage",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (_itemUsageSetter is null)
                _logger.Error($"Could not find {nameof(WeaponComponentData)}.set_ItemUsage through reflection.");

            _missionWeaponUsagesField = typeof(MissionWeapon).GetField("_weapons",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (_missionWeaponUsagesField is null)
                _logger.Error($"Could not find {nameof(MissionWeapon)}._weapons through reflection.");
        }

        internal void RestrictToCouchUsage(Agent agent)
        {
            if (_missionWeaponUsagesField is null || _itemUsageSetter is null) return;

            MissionWeapon lance = agent.Equipment[EquipmentIndex.Weapon0];
            if (_missionWeaponUsagesField.GetValue(lance) is not List<WeaponComponentData> usages)
            {
                _logger.Error($"Could not read the weapon usages of jousting lance '{lance.Item?.StringId}'.");
                return;
            }

            var couchUsage = usages.FirstOrDefault(IsMountedPassivePolearmUsage);
            if (couchUsage is null)
            {
                _logger.Error($"Jousting lance '{lance.Item?.StringId}' has no couch usage.");
                return;
            }

            if (MBItem.GetItemUsageIndex(JoustingCouchItemUsage) < 0)
            {
                _logger.Error($"Jousting item usage '{JoustingCouchItemUsage}' is not loaded.");
                return;
            }

            // The list is mission-local, but its entries are shared with the ItemObject. Clone the couch usage before
            // selecting the jousting animation set so the normal lance remains unchanged outside this mission.
            var joustingCouchUsage = couchUsage.DeepClone();
            if (!TrySetItemUsage(joustingCouchUsage, JoustingCouchItemUsage)) return;

            usages.Clear();
            usages.Add(joustingCouchUsage);
            lance.CurrentUsageIndex = 0;
            agent.EquipWeaponWithNewEntity(EquipmentIndex.Weapon0, ref lance);
        }

        internal bool TrySetItemUsage(WeaponComponentData weapon, string itemUsage)
        {
            if (_itemUsageSetter is null) return false;

            _itemUsageSetter.Invoke(weapon, new object[] { itemUsage });
            return true;
        }

        private static bool IsMountedPassivePolearmUsage(WeaponComponentData usage)
        {
            if (usage.WeaponClass != WeaponClass.TwoHandedPolearm || string.IsNullOrEmpty(usage.ItemUsage))
                return false;

            const ItemObject.ItemUsageSetFlags couchFlags =
                ItemObject.ItemUsageSetFlags.RequiresMount | ItemObject.ItemUsageSetFlags.PassiveUsage;
            return (MBItem.GetItemUsageSetFlags(usage.ItemUsage) & couchFlags) == couchFlags;
        }
    }
}
