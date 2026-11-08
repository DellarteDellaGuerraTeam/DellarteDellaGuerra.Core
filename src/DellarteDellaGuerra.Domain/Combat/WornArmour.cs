namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// What a soldier wears, reduced to the material that takes a hit at each location. Where two
    /// pieces overlap, such as a sleeve and a gauntlet or tassets over hose, the stronger one counts.
    /// </summary>
    public readonly struct WornArmour
    {
        private readonly ArmourPiece _helmet;
        private readonly ArmourPiece _body;
        private readonly ArmourPiece _gloves;
        private readonly ArmourPiece _legs;

        public WornArmour(ArmourPiece helmet, ArmourPiece body, ArmourPiece gloves, ArmourPiece legs)
        {
            _helmet = helmet;
            _body = body;
            _gloves = gloves;
            _legs = legs;
        }

        public ArmourMaterial MaterialAt(HitLocation location)
        {
            return Stronger(
                Stronger(_helmet.MaterialAt(location), _body.MaterialAt(location)),
                Stronger(_gloves.MaterialAt(location), _legs.MaterialAt(location)));
        }

        private static ArmourMaterial Stronger(ArmourMaterial first, ArmourMaterial second)
        {
            return first > second ? first : second;
        }
    }
}
