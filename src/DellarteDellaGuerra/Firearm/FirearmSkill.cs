using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Firearm
{
    public class FirearmSkill
    {
        private SkillObject? _skillObject;

        private static SkillObject CreateSkillObject(string id)
        {
            return Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject(id));
        }

        public void Initialise()
        {
            if (_skillObject is not null) return;

            _skillObject = CreateSkillObject("Firearm");
            _skillObject
                .Initialize(new TextObject("Firearm"),
                    new TextObject(
                        "Mastery of fighting with any firearm weapons."),
                    SkillObject.SkillTypeEnum.Personal).SetAttribute(DefaultCharacterAttributes.Vigor);
        }

        public SkillObject? GetNativeFirearmSkill()
        {
            return _skillObject;
        }
    }
}