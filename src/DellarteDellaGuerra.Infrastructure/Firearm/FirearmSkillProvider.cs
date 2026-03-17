using DellarteDellaGuerra.Infrastructure.MbObjects;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Infrastructure.Firearm;

public class FirearmSkillProvider : IMBObjectProvider<SkillObject>
{
    private SkillObject? _skillObject;

    public SkillObject GetMbObject()
    {
        if (_skillObject is null)
        {
            _skillObject = new SkillObject("Firearm");
            _skillObject
                .Initialize(
                    new TextObject("Firearm"),
                    new TextObject("Mastery of fighting with any firearm weapons."),
                    SkillObject.SkillTypeEnum.Personal)
                .SetAttribute(DefaultCharacterAttributes.Vigor);
        }

        return _skillObject;
    }
}