using DellarteDellaGuerra.Domain.PrivateWars;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarNameplateColorUseCaseTests
    {
        [Theory]
        [InlineData(SettlementNameplateRelation.Enemy, 0xFF870707u)]
        [InlineData(SettlementNameplateRelation.Ally, 0xFF245E05u)]
        [InlineData(SettlementNameplateRelation.Neutral, 0xFF000000u)]
        public void GetSettlementCapsuleArgbColor_WithoutPrivateWar_UsesVanillaRelationColor(
            SettlementNameplateRelation relation,
            uint expected)
        {
            var useCase = new PrivateWarNameplateColorUseCase(new FakeNameplateColorProvider());

            uint color = useCase.GetSettlementCapsuleArgbColor(
                relation,
                isPrivateWarEnemy: false,
                isPrivateWarAlly: false);

            Assert.Equal(expected, color);
        }

        [Fact]
        public void GetSettlementCapsuleArgbColor_PrivateWarEnemy_OverridesVanillaRelationColor()
        {
            var useCase = new PrivateWarNameplateColorUseCase(new FakeNameplateColorProvider(
                enemy: "FF112233",
                ally: "FF445566"));

            uint color = useCase.GetSettlementCapsuleArgbColor(
                SettlementNameplateRelation.Ally,
                isPrivateWarEnemy: true,
                isPrivateWarAlly: false);

            Assert.Equal(0xFF112233u, color);
        }

        [Fact]
        public void GetSettlementCapsuleArgbColor_PrivateWarAlly_OverridesVanillaRelationColor()
        {
            var useCase = new PrivateWarNameplateColorUseCase(new FakeNameplateColorProvider(
                enemy: "FF112233",
                ally: "FF445566"));

            uint color = useCase.GetSettlementCapsuleArgbColor(
                SettlementNameplateRelation.Enemy,
                isPrivateWarEnemy: false,
                isPrivateWarAlly: true);

            Assert.Equal(0xFF445566u, color);
        }

        private sealed class FakeNameplateColorProvider : IPrivateWarNameplateColorProvider
        {
            private readonly string? _enemy;
            private readonly string? _ally;

            public FakeNameplateColorProvider(string? enemy = null, string? ally = null)
            {
                _enemy = enemy;
                _ally = ally;
            }

            public string? GetConfiguredPrivateWarEnemyColorArgb()
                => _enemy;

            public string? GetConfiguredPrivateWarAllyColorArgb()
                => _ally;
        }
    }
}
