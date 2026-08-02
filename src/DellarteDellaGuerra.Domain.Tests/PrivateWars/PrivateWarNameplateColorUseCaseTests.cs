using Bannerlord.PrivateWars.Domain.Interaction;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarNameplateColorUseCaseTests
    {
        [Theory]
        [InlineData(SettlementNameplateRelation.Enemy, 0xFF870707u)]
        [InlineData(SettlementNameplateRelation.SameFaction, 0xFF245E05u)]
        [InlineData(SettlementNameplateRelation.Alliance, 0xFF2986CCu)]
        [InlineData(SettlementNameplateRelation.Neutral, 0xFF000000u)]
        public void GetSettlementCapsuleArgbColor_WithoutPrivateWar_UsesVanillaRelationColor(
            SettlementNameplateRelation relation,
            uint expected)
        {
            var useCase = new NameplateColorUseCase(new FakeNameplateColorProvider());

            uint color = useCase.GetSettlementCapsuleArgbColor(
                relation,
                isPrivateWarEnemy: false,
                isPrivateWarAlly: false);

            Assert.Equal(expected, color);
        }

        [Fact]
        public void GetSettlementCapsuleArgbColor_PrivateWarEnemy_OverridesVanillaRelationColor()
        {
            var useCase = new NameplateColorUseCase(new FakeNameplateColorProvider(
                enemy: "FF112233",
                ally: "FF445566"));

            uint color = useCase.GetSettlementCapsuleArgbColor(
                SettlementNameplateRelation.SameFaction,
                isPrivateWarEnemy: true,
                isPrivateWarAlly: false);

            Assert.Equal(0xFF112233u, color);
        }

        [Fact]
        public void GetSettlementCapsuleArgbColor_PrivateWarAlly_OverridesVanillaRelationColor()
        {
            var useCase = new NameplateColorUseCase(new FakeNameplateColorProvider(
                enemy: "FF112233",
                ally: "FF445566"));

            uint color = useCase.GetSettlementCapsuleArgbColor(
                SettlementNameplateRelation.Enemy,
                isPrivateWarEnemy: false,
                isPrivateWarAlly: true);

            Assert.Equal(0xFF445566u, color);
        }

        [Fact]
        public void GetSettlementCapsuleArgbColor_WhenBothPrivateFlagsAreSet_EnemyTakesPrecedence()
        {
            var useCase = new NameplateColorUseCase(new FakeNameplateColorProvider(
                enemy: "FF112233",
                ally: "FF445566"));

            uint color = useCase.GetSettlementCapsuleArgbColor(
                SettlementNameplateRelation.Alliance,
                isPrivateWarEnemy: true,
                isPrivateWarAlly: true);

            Assert.Equal(0xFF112233u, color);
        }

        [Theory]
        [InlineData(null, NameplateColorUseCase.DefaultPrivateWarEnemyArgb)]
        [InlineData("", NameplateColorUseCase.DefaultPrivateWarEnemyArgb)]
        [InlineData("not-a-color", NameplateColorUseCase.DefaultPrivateWarEnemyArgb)]
        [InlineData("00112233", NameplateColorUseCase.DefaultPrivateWarEnemyArgb)]
        [InlineData("112233", 0xFF112233u)]
        [InlineData("#112233", 0xFF112233u)]
        [InlineData("80112233", 0x80112233u)]
        [InlineData("#80112233", 0x80112233u)]
        public void GetPrivateWarEnemyArgbColor_ValidatesConfiguredColor(string? configured, uint expected)
        {
            var useCase = new NameplateColorUseCase(
                new FakeNameplateColorProvider(enemy: configured));

            Assert.Equal(expected, useCase.GetPrivateWarEnemyArgbColor());
        }

        [Theory]
        [InlineData(null, NameplateColorUseCase.DefaultPrivateWarAllyArgb)]
        [InlineData("", NameplateColorUseCase.DefaultPrivateWarAllyArgb)]
        [InlineData("not-a-color", NameplateColorUseCase.DefaultPrivateWarAllyArgb)]
        [InlineData("00112233", NameplateColorUseCase.DefaultPrivateWarAllyArgb)]
        [InlineData("445566", 0xFF445566u)]
        [InlineData("#445566", 0xFF445566u)]
        [InlineData("80445566", 0x80445566u)]
        [InlineData("#80445566", 0x80445566u)]
        public void GetPrivateWarAllyArgbColor_ValidatesConfiguredColor(string? configured, uint expected)
        {
            var useCase = new NameplateColorUseCase(
                new FakeNameplateColorProvider(ally: configured));

            Assert.Equal(expected, useCase.GetPrivateWarAllyArgbColor());
        }

        private sealed class FakeNameplateColorProvider : INameplateColorProvider
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
