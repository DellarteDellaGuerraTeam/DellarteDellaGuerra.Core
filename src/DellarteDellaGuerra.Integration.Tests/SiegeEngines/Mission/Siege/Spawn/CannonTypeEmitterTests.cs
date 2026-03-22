using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;

namespace DellarteDellaGuerra.Integration.Tests.SiegeEngines.Mission.Siege.Spawn;

public class CannonTypeEmitterTests
{
    [Fact]
    public void GetTypeName_ReturnsGenericCannonPrefixedWithId()
    {
        Assert.Equal("GenericCannon_falconet", CannonTypeEmitter.GetTypeName("falconet"));
    }

    [Fact]
    public void GetTypeName_PreservesUnderscoresInId()
    {
        Assert.Equal("GenericCannon_my_cannon", CannonTypeEmitter.GetTypeName("my_cannon"));
    }

    [Fact]
    public void EmitCannonType_ReturnsTypeNamedAfterCannonId()
    {
        var type = CannonTypeEmitter.EmitCannonType("emitter_test_a");

        Assert.Equal("GenericCannon_emitter_test_a", type.Name);
    }

    [Fact]
    public void EmitCannonType_ReturnedTypeIsSubclassOfGenericCannon()
    {
        var type = CannonTypeEmitter.EmitCannonType("emitter_test_b");

        Assert.True(type.IsSubclassOf(typeof(GenericCannon)));
    }
}
