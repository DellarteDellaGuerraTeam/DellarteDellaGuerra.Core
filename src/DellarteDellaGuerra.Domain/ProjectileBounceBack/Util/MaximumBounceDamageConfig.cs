namespace DellarteDellaGuerra.Domain.ProjectileBounceBack
{
    public record MaximumBounceDamageConfig(
        float NakedCutStick = 10f,
        float NakedPierceStick = 10f,
        float NakedBluntStick = 10f,
        float ClothCutStick = 15f,
        float ClothPierceStick = 12f,
        float ClothBluntStick = 20f,
        float LeatherCutStick = 20f,
        float LeatherPierceStick = 15f,
        float LeatherBluntStick = 20f,
        float MailCutStick = 30f,
        float MailPierceStick = 17f,
        float MailBluntStick = 30f,
        float PlateCutStick = 33f,
        float PlatePierceStick = 20f,
        float PlateBluntStick = 33f
    )
    {
    }
}