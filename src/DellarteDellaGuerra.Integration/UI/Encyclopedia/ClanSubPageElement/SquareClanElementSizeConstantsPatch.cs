using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanSubPageElement;

[PrefabExtension("EncyclopediaClanSubPageElement", "/Prefab/Constants/Constant[@Name='Encyclopedia.SubPage.Element.Height']")]
internal sealed class SquareClanElementSizeConstantsPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Append;

    [PrefabExtensionText(true)]
    public string Constants =>
        """
        <Constants>
            <Constant Name="FrameSize" BooleanCheck="*IsBig" OnFalse="63" OnTrue="89" />
            <Constant Name="BannerSize" BooleanCheck="*IsBig" OnFalse="61" OnTrue="87" />
        </Constants>
        """;
}
