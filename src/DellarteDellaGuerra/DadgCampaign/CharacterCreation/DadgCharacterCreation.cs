using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.DadgCampaign.CharacterCreation
{
    public class DadgCharacterCreation : SandboxCharacterCreationContent
    {
        public override void OnCharacterCreationFinalized()
        {
            MobileParty.MainParty.Position2D = new Vec2(750f, 300f);;
            GameState? gameState = GameStateManager.Current?.ActiveState;
            if (gameState is MapState mapState)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
        }

        public override TextObject ReviewPageDescription => new TextObject("{=W6pKpEoT}You prepare to set off for a grand adventure in England! Here is your character. Continue if you are ready, or go back to make changes.");

        protected override void OnInitialized(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            AddParentsMenu(characterCreation);
            AddChildhoodMenu(characterCreation);
            AddEducationMenu(characterCreation);
            AddYouthMenu(characterCreation);
            AddAdulthoodMenu(characterCreation);
            AddAgeSelectionMenu(characterCreation);
        }

        protected new void AddParentsMenu(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            CharacterCreationMenu characterCreationMenu = new CharacterCreationMenu(new TextObject("{=b4lDDcli}Family"), new TextObject("{=XgFU1pCx}You were born into a family of..."), ParentsOnInit);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(EmpireParentsOnCondition);
            characterCreationCategory.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, effectedAttribute: DefaultCharacterAttributes.Vigor, text: new TextObject("{=InN5ZZt3}A landlord's retainers"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: EmpireLandlordsRetainerOnConsequence, onApply: EmpireLandlordsRetainerOnApply, descriptionText: new TextObject("{=ivKl4mV2}Your father was a trusted lieutenant of the local landowning aristocrat. He rode with the lord's cavalry, fighting as an armored lancer."));
            characterCreationCategory.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            }, effectedAttribute: DefaultCharacterAttributes.Social, text: new TextObject("{=651FhzdR}Urban merchants"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: EmpireMerchantOnConsequence, onApply: EmpireMerchantOnApply, descriptionText: new TextObject("{=FQntPChs}Your family were merchants in one of the main cities of the Empire. They sometimes organized caravans to nearby towns, and discussed issues in the town council."));
            characterCreationCategory.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Polearm
            }, effectedAttribute: DefaultCharacterAttributes.Endurance, text: new TextObject("{=sb4gg8Ak}Freeholders"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: EmpireFreeholderOnConsequence, onApply: EmpireFreeholderOnApply, descriptionText: new TextObject("{=09z8Q08f}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the imperial rural economy, as well as the backbone of the levy."));
            characterCreationCategory.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Crafting,
                DefaultSkills.Crossbow
            }, effectedAttribute: DefaultCharacterAttributes.Intelligence, text: new TextObject("{=v48N6h1t}Urban artisans"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: EmpireArtisanOnConsequence, onApply: EmpireArtisanOnApply, descriptionText: new TextObject("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."));
            characterCreationCategory.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Scouting,
                DefaultSkills.Bow
            }, effectedAttribute: DefaultCharacterAttributes.Control, text: new TextObject("{=7eWmU2mF}Foresters"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: EmpireWoodsmanOnConsequence, onApply: EmpireWoodsmanOnApply, descriptionText: new TextObject("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."));
            characterCreationCategory.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, effectedAttribute: DefaultCharacterAttributes.Cunning, text: new TextObject("{=aEke8dSb}Urban vagabonds"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: EmpireVagabondOnConsequence, onApply: EmpireVagabondOnApply, descriptionText: new TextObject("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of imperial cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the Empire's many criminal gangs, and you had an early look at the dark side of life."));
            CharacterCreationCategory characterCreationCategory2 = characterCreationMenu.AddMenuCategory(VlandianParentsOnCondition);
            characterCreationCategory2.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, effectedAttribute: DefaultCharacterAttributes.Social, text: new TextObject("{=2TptWc4m}A baron's retainers"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: VlandiaBaronsRetainerOnConsequence, onApply: VlandiaBaronsRetainerOnApply, descriptionText: new TextObject("{=0Suu1Q9q}Your father was a bailiff for a local feudal magnate. He looked after his liege's estates, resolved disputes in the village, and helped train the village levy. He rode with the lord's cavalry, fighting as an armored knight."));
            characterCreationCategory2.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            }, effectedAttribute: DefaultCharacterAttributes.Intelligence, text: new TextObject("{=651FhzdR}Urban merchants"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: VlandiaMerchantOnConsequence, onApply: VlandiaMerchantOnApply, descriptionText: new TextObject("{=qNZFkxJb}Your family were merchants in one of the main cities of the kingdom. They organized caravans to nearby towns and were active in the local merchant's guild."));
            characterCreationCategory2.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Polearm,
                DefaultSkills.Crossbow
            }, effectedAttribute: DefaultCharacterAttributes.Endurance, text: new TextObject("{=RDfXuVxT}Yeomen"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: VlandiaYeomanOnConsequence, onApply: VlandiaYeomanOnApply, descriptionText: new TextObject("{=BLZ4mdhb}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."));
            characterCreationCategory2.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Crafting,
                DefaultSkills.TwoHanded
            }, effectedAttribute: DefaultCharacterAttributes.Vigor, text: new TextObject("{=p2KIhGbE}Urban blacksmith"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: VlandiaBlacksmithOnConsequence, onApply: VlandiaBlacksmithOnApply, descriptionText: new TextObject("{=btsMpRcA}Your family owned a smithy in a city. Your father played an active if minor role in the town council, and also served in the militia."));
            characterCreationCategory2.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Scouting,
                DefaultSkills.Crossbow
            }, effectedAttribute: DefaultCharacterAttributes.Control, text: new TextObject("{=YcnK0Thk}Hunters"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: VlandiaHunterOnConsequence, onApply: VlandiaHunterOnApply, descriptionText: new TextObject("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."));
            characterCreationCategory2.AddCategoryOption(effectedSkills: new MBList<SkillObject>
            {
                DefaultSkills.Roguery,
                DefaultSkills.Crossbow
            }, effectedAttribute: DefaultCharacterAttributes.Cunning, text: new TextObject("{=ipQP6aVi}Mercenaries"), focusToAdd: FocusToAdd, skillLevelToAdd: SkillLevelToAdd, attributeLevelToAdd: AttributeLevelToAdd, optionCondition: null, onSelect: VlandiaMercenaryOnConsequence, onApply: VlandiaMercenaryOnApply, descriptionText: new TextObject("{=yYhX6JQC}Your father joined one of Vlandia's many mercenary companies, composed of men who got such a taste for war in their lord's service that they never took well to peace. Their crossbowmen were much valued across Calradia. Your mother was a camp follower, taking you along in the wake of bloody campaigns."));
            CharacterCreationCategory characterCreationCategory3 = characterCreationMenu.AddMenuCategory(SturgianParentsOnCondition);
            characterCreationCategory3.AddCategoryOption(new TextObject("{=mc78FEbA}A boyar's companions"), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.TwoHanded
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, SturgiaBoyarsCompanionOnConsequence, SturgiaBoyarsCompanionOnApply, new TextObject("{=hob3WVkU}Your father was a member of a boyar's druzhina, the 'companions' that make up his retinue. He sat at his lord's table in the great hall, oversaw the boyar's estates, and stood by his side in the center of the shield wall in battle."));
            characterCreationCategory3.AddCategoryOption(new TextObject("{=HqzVBfpl}Urban traders"), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, SturgiaTraderOnConsequence, SturgiaTraderOnApply, new TextObject("{=bjVMtW3W}Your family were merchants who lived in one of Sturgia's great river ports, organizing the shipment of the north's bounty of furs, honey and other goods to faraway lands."));
            characterCreationCategory3.AddCategoryOption(new TextObject("{=zrpqSWSh}Free farmers"), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, SturgiaFreemanOnConsequence, SturgiaFreemanOnApply, new TextObject("{=Mcd3ZyKq}Your family had just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."));
            characterCreationCategory3.AddCategoryOption(new TextObject("{=v48N6h1t}Urban artisans"), new MBList<SkillObject>
            {
                DefaultSkills.Crafting,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, SturgiaArtisanOnConsequence, SturgiaArtisanOnApply, new TextObject("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."));
            characterCreationCategory3.AddCategoryOption(new TextObject("{=YcnK0Thk}Hunters"), new MBList<SkillObject>
            {
                DefaultSkills.Scouting,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, SturgiaHunterOnConsequence, SturgiaHunterOnApply, new TextObject("{=WyZ2UtFF}Your family had no taste for the authority of the boyars. They made their living deep in the woods, slashing and burning fields which they tended for a year or two before moving on. They hunted and trapped fox, hare, ermine, and other fur-bearing animals."));
            characterCreationCategory3.AddCategoryOption(new TextObject("{=TPoK3GSj}Vagabonds"), new MBList<SkillObject>
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, SturgiaVagabondOnConsequence, SturgiaVagabondOnApply, new TextObject("{=2SDWhGmQ}Your family numbered among the poor migrants living in the slums that grow up outside the walls of the river cities, making whatever money they could from a variety of odd jobs. Sometimes they did services for one of the region's many criminal gangs."));
            CharacterCreationCategory characterCreationCategory4 = characterCreationMenu.AddMenuCategory(AseraiParentsOnCondition);
            characterCreationCategory4.AddCategoryOption(new TextObject("{=Sw8OxnNr}Kinsfolk of an emir"), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AseraiTribesmanOnConsequence, AseraiTribesmanOnApply, new TextObject("{=MFrIHJZM}Your family was from a smaller offshoot of an emir's tribe. Your father's land gave him enough income to afford a horse but he was not quite wealthy enough to buy the armor needed to join the heavier cavalry. He fought as one of the light horsemen for which the desert is famous."));
            characterCreationCategory4.AddCategoryOption(new TextObject("{=ngFVgwDD}Warrior-slaves"), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AseraiWariorSlaveOnConsequence, AseraiWariorSlaveOnApply, new TextObject("{=GsPC2MgU}Your father was part of one of the slave-bodyguards maintained by the Aserai emirs. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother."));
            characterCreationCategory4.AddCategoryOption(new TextObject("{=651FhzdR}Urban merchants"), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AseraiMerchantOnConsequence, AseraiMerchantOnApply, new TextObject("{=1zXrlaav}Your family were respected traders in an oasis town. They ran caravans across the desert, and were experts in the finer points of negotiating passage through the desert tribes' territories."));
            characterCreationCategory4.AddCategoryOption(new TextObject("{=g31pXuqi}Oasis farmers"), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AseraiOasisFarmerOnConsequence, AseraiOasisFarmerOnApply, new TextObject("{=5P0KqBAw}Your family tilled the soil in one of the oases of the Nahasa and tended the palm orchards that produced the desert's famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under the emir's banner."));
            characterCreationCategory4.AddCategoryOption(new TextObject("{=EEedqolz}Bedouin"), new MBList<SkillObject>
            {
                DefaultSkills.Scouting,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AseraiBedouinOnConsequence, AseraiBedouinOnApply, new TextObject("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Nahasa."));
            characterCreationCategory4.AddCategoryOption(new TextObject("{=tRIrbTvv}Urban back-alley thugs"), new MBList<SkillObject>
            {
                DefaultSkills.Roguery,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AseraiBackAlleyThugOnConsequence, AseraiBackAlleyThugOnApply, new TextObject("{=6bUSbsKC}Your father worked for a fitiwi, one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff."));
            CharacterCreationCategory characterCreationCategory5 = characterCreationMenu.AddMenuCategory(BattanianParentsOnCondition);
            characterCreationCategory5.AddCategoryOption(new TextObject("{=GeNKQlHR}Members of the chieftain's hearthguard"), new MBList<SkillObject>
            {
                DefaultSkills.TwoHanded,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, BattaniaChieftainsHearthguardOnConsequence, BattaniaChieftainsHearthguardOnApply, new TextObject("{=LpH8SYFL}Your family were the trusted kinfolk of a Battanian chieftain, and sat at his table in his great hall. Your father assisted his chief in running the affairs of the clan and trained with the traditional weapons of the Battanian elite, the two-handed sword or falx and the bow."));
            characterCreationCategory5.AddCategoryOption(new TextObject("{=AeBzTj6w}Healers"), new MBList<SkillObject>
            {
                DefaultSkills.Medicine,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, BattaniaHealerOnConsequence, BattaniaHealerOnApply, new TextObject("{=j6py5Rv5}Your parents were healers who gathered herbs and treated the sick. As a living reservoir of Battanian tradition, they were also asked to adjudicate many disputes between the clans."));
            characterCreationCategory5.AddCategoryOption(new TextObject("{=tGEStbxb}Tribespeople"), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, BattaniaTribesmanOnConsequence, BattaniaTribesmanOnApply, new TextObject("{=WchH8bS2}Your family were middle-ranking members of a Battanian clan, who tilled their own land. Your father fought with the kern, the main body of his people's warriors, joining in the screaming charges for which the Battanians were famous."));
            characterCreationCategory5.AddCategoryOption(new TextObject("{=BCU6RezA}Smiths"), new MBList<SkillObject>
            {
                DefaultSkills.Crafting,
                DefaultSkills.TwoHanded
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, BattaniaSmithOnConsequence, BattaniaSmithOnApply, new TextObject("{=kg9YtrOg}Your family were smiths, a revered profession among the Battanians. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Battanian aristocracy."));
            characterCreationCategory5.AddCategoryOption(new TextObject("{=7eWmU2mF}Foresters"), new MBList<SkillObject>
            {
                DefaultSkills.Scouting,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, BattaniaWoodsmanOnConsequence, BattaniaWoodsmanOnApply, new TextObject("{=7jBroUUQ}Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation."));
            characterCreationCategory5.AddCategoryOption(new TextObject("{=SpJqhEEh}Bards"), new MBList<SkillObject>
            {
                DefaultSkills.Roguery,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, BattaniaBardOnConsequence, BattaniaBardOnApply, new TextObject("{=aVzcyhhy}Your father was a bard, drifting from chieftain's hall to chieftain's hall making his living singing the praises of one Battanian aristocrat and mocking his enemies, then going to his enemy's hall and doing the reverse. You learned from him that a clever tongue could spare you  from a life toiling in the fields, if you kept your wits about you."));
            CharacterCreationCategory characterCreationCategory6 = characterCreationMenu.AddMenuCategory(KhuzaitParentsOnCondition);
            characterCreationCategory6.AddCategoryOption(new TextObject("{=FVaRDe2a}A noyan's kinsfolk"), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, KhuzaitNoyansKinsmanOnConsequence, KhuzaitNoyansKinsmanOnApply, new TextObject("{=jAs3kDXh}Your family were the trusted kinsfolk of a Khuzait noyan, and shared his meals in the chieftain's yurt. Your father assisted his chief in running the affairs of the clan and fought in the core of armored lancers in the center of the Khuzait battle line."));
            characterCreationCategory6.AddCategoryOption(new TextObject("{=TkgLEDRM}Merchants"), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, KhuzaitMerchantOnConsequence, KhuzaitMerchantOnApply, new TextObject("{=qPg3IDiq}Your family came from one of the merchant clans that dominated the cities in eastern Calradia before the Khuzait conquest. They adjusted quickly to their new masters, keeping the caravan routes running and ensuring that the tariff revenues that once went into imperial coffers now flowed to the khanate."));
            characterCreationCategory6.AddCategoryOption(new TextObject("{=tGEStbxb}Tribespeople"), new MBList<SkillObject>
            {
                DefaultSkills.Bow,
                DefaultSkills.Riding
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, KhuzaitTribesmanOnConsequence, KhuzaitTribesmanOnApply, new TextObject("{=URgZ4ai4}Your family were middle-ranking members of one of the Khuzait clans. He had some  herds of his own, but was not rich. When the Khuzait horde was summoned to battle, he fought with the horse archers, shooting and wheeling and wearing down the enemy before the lancers delivered the final punch."));
            characterCreationCategory6.AddCategoryOption(new TextObject("{=gQ2tAvCz}Farmers"), new MBList<SkillObject>
            {
                DefaultSkills.Polearm,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, KhuzaitFarmerOnConsequence, KhuzaitFarmerOnApply, new TextObject("{=5QSGoRFj}Your family tilled one of the small patches of arable land in the steppes for generations. When the Khuzaits came, they ceased paying taxes to the emperor and providing conscripts for his army, and served the khan instead."));
            characterCreationCategory6.AddCategoryOption(new TextObject("{=vfhVveLW}Shamans"), new MBList<SkillObject>
            {
                DefaultSkills.Medicine,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, KhuzaitShamanOnConsequence, KhuzaitShamanOnApply, new TextObject("{=WOKNhaG2}Your family were guardians of the sacred traditions of the Khuzaits, channelling the spirits of the wilderness and of the ancestors. They tended the sick and dispensed wisdom, resolving disputes and providing practical advice."));
            characterCreationCategory6.AddCategoryOption(new TextObject("{=Xqba1Obq}Nomads"), new MBList<SkillObject>
            {
                DefaultSkills.Scouting,
                DefaultSkills.Riding
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, KhuzaitNomadOnConsequence, KhuzaitNomadOnApply, new TextObject("{=9aoQYpZs}Your family's clan never pledged its loyalty to the khan and never settled down, preferring to live out in the deep steppe away from his authority. They remain some of the finest trackers and scouts in the grasslands, as the ability to spot an enemy coming and move quickly is often all that protects their herds from their neighbors' predations."));
            characterCreation.AddNewMenu(characterCreationMenu);
        }

        protected new void AddChildhoodMenu(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            CharacterCreationMenu characterCreationMenu = new CharacterCreationMenu(new TextObject("{=8Yiwt1z6}Early Childhood"), new TextObject("{=character_creation_content_16}As a child you were noted for..."), ChildhoodOnInit);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory();
            characterCreationCategory.AddCategoryOption(new TextObject("{=kmM68Qx4}your leadership skills."), new MBList<SkillObject>
            {
                DefaultSkills.Leadership,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, ChildhoodYourLeadershipSkillsOnConsequence, ChildhoodGoodLeadingOnApply, new TextObject("{=FfNwXtii}If the wolf pup gang of your early childhood had an alpha, it was definitely you. All the other kids followed your lead as you decided what to play and where to play, and led them in games and mischief."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=5HXS8HEY}your brawn."), new MBList<SkillObject>
            {
                DefaultSkills.TwoHanded,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, ChildhoodYourBrawnOnConsequence, ChildhoodGoodAthleticsOnApply, new TextObject("{=YKzuGc54}You were big, and other children looked to have you around in any scrap with children from a neighboring village. You pushed a plough and threw an axe like an adult."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=QrYjPUEf}your attention to detail."), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, ChildhoodAttentionToDetailOnConsequence, ChildhoodGoodMemoryOnApply, new TextObject("{=JUSHAPnu}You were quick on your feet and attentive to what was going on around you. Usually you could run away from trouble, though you could give a good account of yourself in a fight with other children if cornered."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=Y3UcaX74}your aptitude for numbers."), new MBList<SkillObject>
            {
                DefaultSkills.Engineering,
                DefaultSkills.Trade
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, ChildhoodAptitudeForNumbersOnConsequence, ChildhoodGoodMathOnApply, new TextObject("{=DFidSjIf}Most children around you had only the most rudimentary education, but you lingered after class to study letters and mathematics. You were fascinated by the marketplace - weights and measures, tallies and accounts, the chatter about profits and losses."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=GEYzLuwb}your way with people."), new MBList<SkillObject>
            {
                DefaultSkills.Charm,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, ChildhoodWayWithPeopleOnConsequence, ChildhoodGoodMannersOnApply, new TextObject("{=w2TEQq26}You were always attentive to other people, good at guessing their motivations. You studied how individuals were swayed, and tried out what you learned from adults on your friends."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=MEgLE2kj}your skill with horses."), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Medicine
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, ChildhoodSkillsWithHorsesOnConsequence, ChildhoodAffinityWithAnimalsOnApply, new TextObject("{=ngazFofr}You were always drawn to animals, and spent as much time as possible hanging out in the village stables. You could calm horses, and were sometimes called upon to break in new colts. You learned the basics of veterinary arts, much of which is applicable to humans as well."));
            characterCreation.AddNewMenu(characterCreationMenu);
        }

        protected new void AddEducationMenu(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            CharacterCreationMenu characterCreationMenu = new CharacterCreationMenu(new TextObject("{=rcoueCmk}Adolescence"), _educationIntroductoryText, EducationOnInit);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory();
            characterCreationCategory.AddCategoryOption(new TextObject("{=RKVNvimC}herded the sheep."), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, RuralAdolescenceOnCondition, RuralAdolescenceHerderOnConsequence, RuralAdolescenceHerderOnApply, new TextObject("{=KfaqPpbK}You went with other fleet-footed youths to take the villages' sheep, goats or cattle to graze in pastures near the village. You were in charge of chasing down stray beasts, and always kept a big stone on hand to be hurled at lurking predators if necessary."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=bTKiN0hr}worked in the village smithy."), new MBList<SkillObject>
            {
                DefaultSkills.TwoHanded,
                DefaultSkills.Crafting
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, RuralAdolescenceOnCondition, RuralAdolescenceSmithyOnConsequence, RuralAdolescenceSmithyOnApply, new TextObject("{=y6j1bJTH}You were apprenticed to the local smith. You learned how to heat and forge metal, hammering for hours at a time until your muscles ached."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=tI8ZLtoA}repaired projects."), new MBList<SkillObject>
            {
                DefaultSkills.Crafting,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, RuralAdolescenceOnCondition, RuralAdolescenceRepairmanOnConsequence, RuralAdolescenceRepairmanOnApply, new TextObject("{=6LFj919J}You helped dig wells, rethatch houses, and fix broken plows. You learned about the basics of construction, as well as what it takes to keep a farming community prosperous."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=TRwgSLD2}gathered herbs in the wild."), new MBList<SkillObject>
            {
                DefaultSkills.Medicine,
                DefaultSkills.Scouting
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, RuralAdolescenceOnCondition, RuralAdolescenceGathererOnConsequence, RuralAdolescenceGathererOnApply, new TextObject("{=9ks4u5cH}You were sent by the village healer up into the hills to look for useful medicinal plants. You learned which herbs healed wounds or brought down a fever, and how to find them."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=T7m7ReTq}hunted small game."), new MBList<SkillObject>
            {
                DefaultSkills.Bow,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, RuralAdolescenceOnCondition, RuralAdolescenceHunterOnConsequence, RuralAdolescenceHunterOnApply, new TextObject("{=RuvSk3QT}You accompanied a local hunter as he went into the wilderness, helping him set up traps and catch small animals."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=qAbMagWq}sold product at the market."), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, RuralAdolescenceOnCondition, RuralAdolescenceHelperOnConsequence, RuralAdolescenceHelperOnApply, new TextObject("{=DIgsfYfz}You took your family's goods to the nearest town to sell your produce and buy supplies. It was hard work, but you enjoyed the hubbub of the marketplace."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=nOfSqRnI}at the town watch's training ground."), new MBList<SkillObject>
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanAdolescenceOnCondition, UrbanAdolescenceWatcherOnConsequence, UrbanAdolescenceWatcherOnApply, new TextObject("{=qnqdEJOv}You watched the town's watch practice shooting and perfect their plans to defend the walls in case of a siege."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=8a6dnLd2}with the alley gangs."), new MBList<SkillObject>
            {
                DefaultSkills.Roguery,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanAdolescenceOnCondition, UrbanAdolescenceGangerOnConsequence, UrbanAdolescenceGangerOnApply, new TextObject("{=1SUTcF0J}The gang leaders who kept watch over the slums of Calradian cities were always in need of poor youth to run messages and back them up in turf wars, while thrill-seeking merchants' sons and daughters sometimes slummed it in their company as well."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=7Hv984Sf}at docks and building sites."), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Crafting
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanAdolescenceOnCondition, UrbanAdolescenceDockerOnConsequence, UrbanAdolescenceDockerOnApply, new TextObject("{=bhdkegZ4}All towns had their share of projects that were constantly in need of both skilled and unskilled labor. You learned how hoists and scaffolds were constructed, how planks and stones were hewn and fitted, and other skills."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=kbcwb5TH}in the markets and caravanserais."), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanPoorAdolescenceOnCondition, UrbanAdolescenceMarketerOnConsequence, UrbanAdolescenceMarketerOnApply, new TextObject("{=lLJh7WAT}You worked in the marketplace, selling trinkets and drinks to busy shoppers."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=kbcwb5TH}in the markets and caravanserais."), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanRichAdolescenceOnCondition, UrbanAdolescenceMarketerOnConsequence, UrbanAdolescenceMarketerOnApply, new TextObject("{=rmMcwSn8}You helped your family handle their business affairs, going down to the marketplace to make purchases and oversee the arrival of caravans."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=mfRbx5KE}reading and studying."), new MBList<SkillObject>
            {
                DefaultSkills.Engineering,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanPoorAdolescenceOnCondition, UrbanAdolescenceTutorOnConsequence, UrbanAdolescenceDockerOnApply, new TextObject("{=elQnygal}Your family scraped up the money for a rudimentary schooling and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=etG87fB7}with your tutor."), new MBList<SkillObject>
            {
                DefaultSkills.Engineering,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanRichAdolescenceOnCondition, UrbanAdolescenceTutorOnConsequence, UrbanAdolescenceDockerOnApply, new TextObject("{=hXl25avg}Your family arranged for a private tutor and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=FKpLEamz}caring for horses."), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Steward
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanRichAdolescenceOnCondition, UrbanAdolescenceHorserOnConsequence, UrbanAdolescenceDockerOnApply, new TextObject("{=Ghz90npw}Your family owned a few horses at the town stables and you took charge of their care. Many evenings you would take them out beyond the walls and gallup through the fields, racing other youth."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=vH7GtuuK}working at the stables."), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Steward
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, UrbanPoorAdolescenceOnCondition, UrbanAdolescenceHorserOnConsequence, UrbanAdolescenceDockerOnApply, new TextObject("{=csUq1RCC}You were employed as a hired hand at the town's stables. The overseers recognized that you had a knack for horses, and you were allowed to exercise them and sometimes even break in new steeds."));
            characterCreation.AddNewMenu(characterCreationMenu);
        }

        protected void AddYouthMenu(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            CharacterCreationMenu characterCreationMenu = new CharacterCreationMenu(new TextObject("{=ok8lSW6M}Youth"), _youthIntroductoryText, YouthOnInit);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory();
            characterCreationCategory.AddCategoryOption(new TextObject("{=CITG915d}joined a commander's staff."), new MBList<SkillObject>
            {
                DefaultSkills.Steward,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthCommanderOnCondition, YouthCommanderOnConsequence, YouthCommanderOnApply, new TextObject("{=Ay0G3f7I}Your family arranged for you to be part of the staff of an imperial strategos. You were not given major responsibilities - mostly carrying messages and tending to his horse -- but it did give you a chance to see how campaigns were planned and men were deployed in battle."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=bhE2i6OU}served as a baron's groom."), new MBList<SkillObject>
            {
                DefaultSkills.Steward,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthGroomOnCondition, YouthGroomOnConsequence, YouthGroomOnApply, new TextObject("{=iZKtGI6Y}Your family arranged for you to accompany a minor baron of the Vlandian kingdom. You were not given major responsibilities - mostly carrying messages and tending to his horse -- but it did give you a chance to see how campaigns were planned and men were deployed in battle."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=F2bgujPo}were a chieftain's servant."), new MBList<SkillObject>
            {
                DefaultSkills.Steward,
                DefaultSkills.Tactics
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthChieftainOnCondition, YouthChieftainOnConsequence, YouthChieftainOnApply, new TextObject("{=7AYJ3SjK}Your family arranged for you to accompany a chieftain of your people. You were not given major responsibilities - mostly carrying messages and tending to his horse -- but it did give you a chance to see how campaigns were planned and men were deployed in battle."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=h2KnarLL}trained with the cavalry."), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthCavalryOnCondition, YouthCavalryOnConsequence, YouthCavalryOnApply, new TextObject("{=7cHsIMLP}You could never have bought the equipment on your own, but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=zsC2t5Hb}trained with the hearth guard."), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthHearthGuardOnCondition, YouthHearthGuardOnConsequence, YouthHearthGuardOnApply, new TextObject("{=RmbWW6Bm}You were a big and imposing enough youth that the chief's guard allowed you to train alongside them, in preparation to join them some day."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=aTncHUfL}stood guard with the garrisons."), new MBList<SkillObject>
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthGarrisonOnCondition, YouthGarrisonOnConsequence, YouthGarrisonOnApply, new TextObject("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=aTncHUfL}stood guard with the garrisons."), new MBList<SkillObject>
            {
                DefaultSkills.Bow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthOtherGarrisonOnCondition, YouthOtherGarrisonOnConsequence, YouthOtherGarrisonOnApply, new TextObject("{=1EkEElZd}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=VlXOgIX6}rode with the scouts."), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthOutridersOnCondition, YouthOutridersOnConsequence, YouthOutridersOnApply, new TextObject("{=888lmJqs}All of Calradia's kingdoms recognize the value of good light cavalry and horse archers, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a good bow so that you could fulfill their levy obligations."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=VlXOgIX6}rode with the scouts."), new MBList<SkillObject>
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthOtherOutridersOnCondition, YouthOtherOutridersOnConsequence, YouthOtherOutridersOnApply, new TextObject("{=sYuN6hPD}All of Calradia's kingdoms recognize the value of good light cavalry, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a sheaf of javelins so that you could fulfill their levy obligations."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=a8arFSra}trained with the infantry."), new MBList<SkillObject>
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, YouthInfantryOnConsequence, YouthInfantryOnApply, new TextObject("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Calradia."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=oMbOIPc9}joined the skirmishers."), new MBList<SkillObject>
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthSkirmisherOnCondition, YouthSkirmisherOnConsequence, YouthSkirmisherOnApply, new TextObject("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=cDWbwBwI}joined the kern."), new MBList<SkillObject>
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthKernOnCondition, YouthKernOnConsequence, YouthKernOnApply, new TextObject("{=tTb28jyU}Many Battanians fight as kern, versatile troops who could both harass the enemy line with their javelins or join in the final screaming charge once it weakened."));
            characterCreationCategory.AddCategoryOption(new TextObject("{=GFUggps8}marched with the camp followers."), new MBList<SkillObject>
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, YouthCamperOnCondition, YouthCamperOnConsequence, YouthCamperOnApply, new TextObject("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery."));
            characterCreation.AddNewMenu(characterCreationMenu);
        }

        protected void AddAdulthoodMenu(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            MBTextManager.SetTextVariable("EXP_VALUE", SkillLevelToAdd);
            CharacterCreationMenu characterCreationMenu = new CharacterCreationMenu(new TextObject("{=MafIe9yI}Young Adulthood"), new TextObject("{=4WYY0X59}Before you set out for a life of adventure, your biggest achievement was..."), AccomplishmentOnInit);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory();
            characterCreationCategory.AddCategoryOption(new TextObject("{=8bwpVpgy}you defeated an enemy in battle."), new MBList<SkillObject>
            {
                DefaultSkills.OneHanded,
                DefaultSkills.TwoHanded
            }, DefaultCharacterAttributes.Vigor, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AccomplishmentDefeatedEnemyOnConsequence, AccomplishmentDefeatedEnemyOnApply, new TextObject("{=1IEroJKs}Not everyone who musters for the levy marches to war, and not everyone who goes on campaign sees action. You did both, and you also took down an enemy warrior in direct one-to-one combat, in the full view of your comrades."), new MBList<TraitObject> { DefaultTraits.Valor }, 1, 20);
            characterCreationCategory.AddCategoryOption(new TextObject("{=mP3uFbcq}you led a successful manhunt."), new MBList<SkillObject>
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentPosseOnConditions, AccomplishmentExpeditionOnConsequence, AccomplishmentExpeditionOnApply, new TextObject("{=4f5xwzX0}When your community needed to organize a posse to pursue horse thieves, you were the obvious choice. You hunted down the raiders, surrounded them and forced their surrender, and took back your stolen property."), new MBList<TraitObject> { DefaultTraits.Calculating }, 1, 10);
            characterCreationCategory.AddCategoryOption(new TextObject("{=wfbtS71d}you led a caravan."), new MBList<SkillObject>
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentMerchantOnCondition, AccomplishmentMerchantOnConsequence, AccomplishmentExpeditionOnApply, new TextObject("{=joRHKCkm}Your family needed someone trustworthy to take a caravan to a neighboring town. You organized supplies, ensured a constant watch to keep away bandits, and brought it safely to its destination."), new MBList<TraitObject> { DefaultTraits.Calculating }, 1, 10);
            characterCreationCategory.AddCategoryOption(new TextObject("{=x1HTX5hq}you saved your village from a flood."), new MBList<SkillObject>
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentSavedVillageOnCondition, AccomplishmentSavedVillageOnConsequence, AccomplishmentExpeditionOnApply, new TextObject("{=bWlmGDf3}When a sudden storm caused the local stream to rise suddenly, your neighbors needed quick-thinking leadership. You provided it, directing them to build levees to save their homes."), new MBList<TraitObject> { DefaultTraits.Calculating }, 1, 10);
            characterCreationCategory.AddCategoryOption(new TextObject("{=s8PNllPN}you saved your city quarter from a fire."), new MBList<SkillObject>
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentSavedStreetOnCondition, AccomplishmentSavedStreetOnConsequence, AccomplishmentExpeditionOnApply, new TextObject("{=ZAGR6PYc}When a sudden blaze broke out in a back alley, your neighbors needed quick-thinking leadership and you provided it. You organized a bucket line to the nearest well, putting the fire out before any homes were lost."), new MBList<TraitObject> { DefaultTraits.Calculating }, 1, 10);
            characterCreationCategory.AddCategoryOption(new TextObject("{=xORjDTal}you invested some money in a workshop."), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Crafting
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentUrbanOnCondition, AccomplishmentWorkshopOnConsequence, AccomplishmentWorkshopOnApply, new TextObject("{=PyVqDLBu}Your parents didn't give you much money, but they did leave just enough for you to secure a loan against a larger amount to build a small workshop. You paid back what you borrowed, and sold your enterprise for a profit."), new MBList<TraitObject> { DefaultTraits.Calculating }, 1, 10);
            characterCreationCategory.AddCategoryOption(new TextObject("{=xKXcqRJI}you invested some money in land."), new MBList<SkillObject>
            {
                DefaultSkills.Trade,
                DefaultSkills.Crafting
            }, DefaultCharacterAttributes.Intelligence, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentRuralOnCondition, AccomplishmentWorkshopOnConsequence, AccomplishmentWorkshopOnApply, new TextObject("{=cbF9jdQo}Your parents didn't give you much money, but they did leave just enough for you to purchase a plot of unused land at the edge of the village. You cleared away rocks and dug an irrigation ditch, raised a few seasons of crops, than sold it for a considerable profit."), new MBList<TraitObject> { DefaultTraits.Calculating }, 1, 10);
            characterCreationCategory.AddCategoryOption(new TextObject("{=TbNRtUjb}you hunted a dangerous animal."), new MBList<SkillObject>
            {
                DefaultSkills.Polearm,
                DefaultSkills.Crossbow
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentRuralOnCondition, AccomplishmentSiegeHunterOnConsequence, AccomplishmentSiegeHunterOnApply, new TextObject("{=I3PcdaaL}Wolves, bears are a constant menace to the flocks of northern Calradia, while hyenas and leopards trouble the south. You went with a group of your fellow villagers and fired the missile that brought down the beast."), new MBList<TraitObject> { DefaultTraits.Valor }, 1, 5);
            characterCreationCategory.AddCategoryOption(new TextObject("{=WbHfGCbd}you survived a siege."), new MBList<SkillObject>
            {
                DefaultSkills.Bow,
                DefaultSkills.Crossbow
            }, DefaultCharacterAttributes.Control, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentUrbanOnCondition, AccomplishmentSiegeHunterOnConsequence, AccomplishmentSiegeHunterOnApply, new TextObject("{=FhZPjhli}Your hometown was briefly placed under siege, and you were called to defend the walls. Everyone did their part to repulse the enemy assault, and everyone is justly proud of what they endured."), null, 0, 5);
            characterCreationCategory.AddCategoryOption(new TextObject("{=kNXet6Um}you had a famous escapade in town."), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Roguery
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentRuralOnCondition, AccomplishmentEscapadeOnConsequence, AccomplishmentEscapadeOnApply, new TextObject("{=DjeAJtix}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, on one of your trips into town you got into the kind of trouble from which only a quick tongue or quick feet get you out alive."), new MBList<TraitObject> { DefaultTraits.Valor }, 1, 5);
            characterCreationCategory.AddCategoryOption(new TextObject("{=qlOuiKXj}you had a famous escapade."), new MBList<SkillObject>
            {
                DefaultSkills.Athletics,
                DefaultSkills.Roguery
            }, DefaultCharacterAttributes.Endurance, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AccomplishmentUrbanOnCondition, AccomplishmentEscapadeOnConsequence, AccomplishmentEscapadeOnApply, new TextObject("{=lD5Ob3R4}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, you got into the kind of trouble from which only a quick tongue or quick feet get you out alive."), new MBList<TraitObject> { DefaultTraits.Valor }, 1, 5);
            characterCreationCategory.AddCategoryOption(new TextObject("{=Yqm0Dics}you treated people well."), new MBList<SkillObject>
            {
                DefaultSkills.Charm,
                DefaultSkills.Steward
            }, DefaultCharacterAttributes.Social, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, AccomplishmentTreaterOnConsequence, AccomplishmentTreaterOnApply, new TextObject("{=dDmcqTzb}Yours wasn't the kind of reputation that local legends are made of, but it was the kind that wins you respect among those around you. You were consistently fair and honest in your business dealings and helpful to those in trouble. In doing so, you got a sense of what made people tick."), new MBList<TraitObject>
            {
                DefaultTraits.Mercy,
                DefaultTraits.Generosity,
                DefaultTraits.Honor
            }, 1, 5);
            characterCreation.AddNewMenu(characterCreationMenu);
        }
    }
}