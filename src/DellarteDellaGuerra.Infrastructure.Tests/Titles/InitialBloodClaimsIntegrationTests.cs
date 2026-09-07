using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class InitialBloodClaimsIntegrationTests
{
    private const string ExpectedClaims =
        """
        barony_arundel|dadg_lord_3_2|clan_fitzalan|Strong
        barony_arundel|dadg_lord_3_4|clan_fitzalan|Strong
        barony_arundel|dadg_lord_3_5|clan_fitzalan|Strong
        barony_arundel|dadg_lord_3_6|clan_fitzalan|Strong
        barony_arundel|dadg_lord_3_7|clan_fitzalan|Weak
        barony_baconsthorpe|dadg_lord_33_3|clan_paston|Strong
        barony_baconsthorpe|dadg_lord_33_5|clan_paston|Strong
        barony_baconsthorpe|dadg_lord_33_6|clan_paston|Strong
        barony_baconsthorpe|dadg_lord_33_7|clan_paston|Strong
        barony_bamburgh|dadg_lord_51_3|clan_lumley|Strong
        barony_bamburgh|dadg_lord_51_5|clan_lumley|Weak
        barony_bamburgh|dadg_lord_51_6|clan_lumley|Weak
        barony_bamburgh|dadg_lord_51_7|clan_lumley|Weak
        barony_barnard|dadg_lord_46_3|clan_harrington|Strong
        barony_barnard|dadg_lord_46_4|clan_harrington|Weak
        barony_barnard|dadg_lord_46_5|clan_harrington|Weak
        barony_barnard|dadg_lord_46_6|clan_harrington|Strong
        barony_barnard|dadg_lord_51_2|clan_lumley|Weak
        barony_beaumaris|dadg_lord_40_2|clan_devereux|Strong
        barony_beaumaris|dadg_lord_40_3|clan_devereux|Strong
        barony_beaumaris|dadg_lord_40_4|clan_devereux|Weak
        barony_beaumaris|dadg_lord_40_5|clan_devereux|Weak
        barony_beaumaris|dadg_lord_40_6|clan_devereux|Strong
        barony_beaumaris|dadg_lord_40_7|clan_devereux|Strong
        barony_beeston|dadg_lord_20_3|clan_vernon|Strong
        barony_beeston|dadg_lord_20_4|clan_vernon|Strong
        barony_beeston|dadg_lord_20_5|clan_vernon|Strong
        barony_berkhamsted|dadg_lord_2_3|clan_lancaster|Strong
        barony_brecon|dadg_lord_15_4|clan_stafford|Weak
        barony_brecon|dadg_lord_27_3|clan_neville_of_bergavenny|Strong
        barony_brecon|dadg_lord_27_4|clan_neville_of_bergavenny|Strong
        barony_brecon|dadg_lord_27_6|clan_neville_of_bergavenny|Weak
        barony_brecon|dadg_lord_27_7|clan_neville_of_bergavenny|Weak
        barony_brecon|dadg_lord_27_8|clan_neville_of_bergavenny|Weak
        barony_brecon|dadg_lord_28_2|clan_stanley|Weak
        barony_caerphilly|dadg_lord_11_2|clan_hastings|Weak
        barony_caerphilly|dadg_lord_2_4|clan_lancaster|Weak
        barony_caerphilly|dadg_lord_30_2|clan_fitzhugh|Weak
        barony_caerphilly|dadg_lord_41_2|clan_de_vere|Weak
        barony_caerphilly|dadg_lord_45_2|clan_blount|Weak
        barony_caerphilly|dadg_lord_9_2|clan_neville_of_middleham|Strong
        barony_caerphilly|dadg_lord_9_4|clan_neville_of_middleham|Weak
        barony_caerphilly|dadg_lord_9_5|clan_neville_of_middleham|Weak
        barony_caerphilly|dadg_lord_9_6|clan_neville_of_middleham|Weak
        barony_caerphilly|dadg_lord_9_7|clan_neville_of_middleham|Weak
        barony_caerphilly|dadg_lord_9_8|clan_neville_of_middleham|Weak
        barony_caerphilly|dadg_lord_9_9|clan_neville_of_middleham|Weak
        barony_cardigan|dadg_lord_7_2|clan_tudor|Weak
        barony_cardigan|dadg_lord_7_4|clan_tudor|Weak
        barony_carisbrooke|dadg_lord_1_4|clan_york|Weak
        barony_carisbrooke|dadg_lord_15_5|clan_stafford|Strong
        barony_carisbrooke|dadg_lord_15_6|clan_stafford|Weak
        barony_carisbrooke|dadg_lord_21_2|clan_woodville|Weak
        barony_carisbrooke|dadg_lord_21_3|clan_woodville|Strong
        barony_carisbrooke|dadg_lord_21_4|clan_woodville|Strong
        barony_carisbrooke|dadg_lord_21_5|clan_woodville|Strong
        barony_carisbrooke|dadg_lord_24_6|clan_grey_of_ruthin|Weak
        barony_carisbrooke|dadg_lord_3_3|clan_fitzalan|Weak
        barony_carisbrooke|dadg_mary_woodville_1443|clan_herbert|Weak
        barony_carmarthen|dadg_lord_2_3|clan_lancaster|Strong
        barony_chepstow|dadg_lord_37_3|clan_berkeley|Weak
        barony_chepstow|dadg_lord_37_4|clan_berkeley|Strong
        barony_chepstow|dadg_lord_37_5|clan_berkeley|Strong
        barony_chepstow|dadg_lord_37_7|clan_berkeley|Weak
        barony_chepstow|dadg_lord_37_8|clan_berkeley|Strong
        barony_chirk|dadg_lord_1_2|clan_york|Strong
        barony_chirk|dadg_lord_1_3|clan_york|Strong
        barony_chirk|dadg_lord_1_5|clan_york|Weak
        barony_chirk|dadg_lord_1_6|clan_york|Weak
        barony_chirk|dadg_lord_1_7|clan_york|Strong
        barony_chirk|dadg_lord_14_3|clan_de_la_pole|Weak
        barony_chirk|dadg_lord_17_2|clan_holland|Weak
        barony_clare|dadg_lord_42_3|clan_morley|Strong
        barony_clare|dadg_lord_42_4|clan_morley|Weak
        barony_conisbrough|dadg_lord_44_3|clan_burgh|Strong
        barony_conisbrough|dadg_lord_44_4|clan_burgh|Weak
        barony_conisbrough|dadg_lord_44_5|clan_burgh|Weak
        barony_conwy|dadg_lord_1_4|clan_york|Weak
        barony_conwy|dadg_lord_15_5|clan_stafford|Strong
        barony_conwy|dadg_lord_15_6|clan_stafford|Weak
        barony_conwy|dadg_lord_21_2|clan_woodville|Weak
        barony_conwy|dadg_lord_21_3|clan_woodville|Strong
        barony_conwy|dadg_lord_21_4|clan_woodville|Strong
        barony_conwy|dadg_lord_21_5|clan_woodville|Strong
        barony_conwy|dadg_lord_24_6|clan_grey_of_ruthin|Weak
        barony_conwy|dadg_lord_3_3|clan_fitzalan|Weak
        barony_conwy|dadg_mary_woodville_1443|clan_herbert|Weak
        barony_corfe|dadg_lord_55_3|clan_stourton|Strong
        barony_corfe|dadg_lord_55_4|clan_stourton|Strong
        barony_corfe|dadg_lord_55_5|clan_stourton|Strong
        barony_corfe|dadg_lord_55_6|clan_stourton|Weak
        barony_corfe|dadg_lord_55_7|clan_stourton|Weak
        barony_denbigh|dadg_lord_13_10|clan_herbert|Weak
        barony_denbigh|dadg_lord_13_4|clan_herbert|Strong
        barony_denbigh|dadg_lord_13_5|clan_herbert|Strong
        barony_denbigh|dadg_lord_13_6|clan_herbert|Strong
        barony_denbigh|dadg_lord_13_7|clan_herbert|Weak
        barony_denbigh|dadg_lord_13_8|clan_herbert|Weak
        barony_denbigh|dadg_lord_13_9|clan_herbert|Weak
        barony_donnington|dadg_lord_35_3|clan_willoughby|Strong
        barony_dover|dadg_lord_38_3|clan_west|Strong
        barony_dover|dadg_lord_38_4|clan_west|Strong
        barony_dover|dadg_lord_38_5|clan_west|Strong
        barony_dover|dadg_lord_38_6|clan_west|Strong
        barony_dunster|dadg_lord_34_4|clan_dynham|Weak
        barony_dunster|dadg_lord_34_5|clan_dynham|Weak
        barony_durham|dadg_lord_22_2|clan_greystoke|Strong
        barony_durham|dadg_lord_22_4|clan_greystoke|Strong
        barony_durham|dadg_lord_22_5|clan_greystoke|Strong
        barony_durham|dadg_lord_50_2|clan_scrope_of_masham|Weak
        barony_farleigh|dadg_lord_5_2|clan_grey_of_groby|Weak
        barony_farnham|dadg_lord_18_2|clan_st_leger|Strong
        barony_farnham|dadg_lord_18_3|clan_st_leger|Strong
        barony_fotheringhay|dadg_lord_1_2|clan_york|Strong
        barony_fotheringhay|dadg_lord_1_3|clan_york|Strong
        barony_fotheringhay|dadg_lord_1_5|clan_york|Weak
        barony_fotheringhay|dadg_lord_1_6|clan_york|Weak
        barony_fotheringhay|dadg_lord_1_7|clan_york|Strong
        barony_fotheringhay|dadg_lord_14_3|clan_de_la_pole|Weak
        barony_fotheringhay|dadg_lord_17_2|clan_holland|Weak
        barony_gloucester|dadg_lord_54_2|clan_beauchamp|Strong
        barony_gloucester|dadg_lord_54_4|clan_beauchamp|Weak
        barony_gloucester|dadg_lord_54_5|clan_beauchamp|Weak
        barony_hedingham|dadg_lord_41_3|clan_de_vere|Strong
        barony_hedingham|dadg_lord_41_6|clan_de_vere|Strong
        barony_hedingham|dadg_lord_41_8|clan_de_vere|Strong
        barony_helmsley|dadg_lord_50_3|clan_scrope_of_masham|Strong
        barony_helmsley|dadg_lord_50_4|clan_scrope_of_masham|Strong
        barony_helmsley|dadg_lord_50_5|clan_scrope_of_masham|Strong
        barony_helmsley|dadg_lord_50_6|clan_scrope_of_masham|Strong
        barony_hertford|dadg_lord_12_3|clan_bourchier|Strong
        barony_hertford|dadg_lord_12_5|clan_bourchier|Strong
        barony_hertford|dadg_lord_12_6|clan_bourchier|Strong
        barony_hertford|dadg_lord_12_8|clan_bourchier|Weak
        barony_hertford|dadg_lord_23_3|clan_mowbray|Weak
        barony_kendal|dadg_lord_39_3|clan_parr|Strong
        barony_kendal|dadg_lord_39_4|clan_parr|Strong
        barony_kenilworth|dadg_lord_48_3|clan_clinton|Strong
        barony_lewes|dadg_lord_43_3|clan_fienne_of_dacre|Strong
        barony_lewes|dadg_lord_43_5|clan_fienne_of_dacre|Weak
        barony_lewes|dadg_lord_43_6|clan_fienne_of_dacre|Strong
        barony_lewes|dadg_lord_43_7|clan_fienne_of_dacre|Strong
        barony_lewes|dadg_lord_43_8|clan_fienne_of_dacre|Strong
        barony_liverpool|dadg_lord_11_3|clan_hastings|Strong
        barony_liverpool|dadg_lord_11_5|clan_hastings|Strong
        barony_liverpool|dadg_lord_11_7|clan_hastings|Strong
        barony_liverpool|dadg_lord_11_8|clan_hastings|Strong
        barony_ludlow|dadg_lord_1_2|clan_york|Strong
        barony_ludlow|dadg_lord_1_3|clan_york|Strong
        barony_ludlow|dadg_lord_1_5|clan_york|Weak
        barony_ludlow|dadg_lord_1_6|clan_york|Weak
        barony_ludlow|dadg_lord_1_7|clan_york|Strong
        barony_ludlow|dadg_lord_14_3|clan_de_la_pole|Weak
        barony_ludlow|dadg_lord_17_2|clan_holland|Weak
        barony_middleham|dadg_lord_11_2|clan_hastings|Weak
        barony_middleham|dadg_lord_2_4|clan_lancaster|Weak
        barony_middleham|dadg_lord_30_2|clan_fitzhugh|Weak
        barony_middleham|dadg_lord_41_2|clan_de_vere|Weak
        barony_middleham|dadg_lord_45_2|clan_blount|Weak
        barony_middleham|dadg_lord_9_2|clan_neville_of_middleham|Strong
        barony_middleham|dadg_lord_9_4|clan_neville_of_middleham|Weak
        barony_middleham|dadg_lord_9_5|clan_neville_of_middleham|Weak
        barony_middleham|dadg_lord_9_6|clan_neville_of_middleham|Weak
        barony_middleham|dadg_lord_9_7|clan_neville_of_middleham|Weak
        barony_middleham|dadg_lord_9_8|clan_neville_of_middleham|Weak
        barony_middleham|dadg_lord_9_9|clan_neville_of_middleham|Weak
        barony_nottingham|dadg_lord_11_3|clan_hastings|Strong
        barony_nottingham|dadg_lord_11_5|clan_hastings|Strong
        barony_nottingham|dadg_lord_11_7|clan_hastings|Strong
        barony_nottingham|dadg_lord_11_8|clan_hastings|Strong
        barony_okehampton|dadg_lord_16_2|clan_courtenay|Weak
        barony_okehampton|dadg_lord_16_3|clan_courtenay|Weak
        barony_okehampton|dadg_lord_16_4|clan_courtenay|Weak
        barony_okehampton|dadg_lord_16_5|clan_courtenay|Weak
        barony_okehampton|dadg_lord_8_6|clan_clifford|Weak
        barony_painscastle|dadg_lord_25_3|clan_grey_of_wilton|Strong
        barony_painscastle|dadg_lord_25_5|clan_grey_of_wilton|Weak
        barony_piel|dadg_lord_28_3|clan_stanley|Strong
        barony_piel|dadg_lord_28_5|clan_stanley|Strong
        barony_piel|dadg_lord_28_6|clan_stanley|Strong
        barony_piel|dadg_lord_28_7|clan_stanley|Strong
        barony_piel|dadg_lord_28_8|clan_stanley|Strong
        barony_plymouth|dadg_lord_45_3|clan_blount|Strong
        barony_plymouth|dadg_lord_45_6|clan_blount|Strong
        barony_plymouth|dadg_lord_45_7|clan_blount|Strong
        barony_richmond|dadg_lord_7_2|clan_tudor|Weak
        barony_richmond|dadg_lord_7_4|clan_tudor|Weak
        barony_rochester|dadg_lord_22_3|clan_greystoke|Weak
        barony_rochester|dadg_lord_24_3|clan_grey_of_ruthin|Strong
        barony_rochester|dadg_lord_24_4|clan_grey_of_ruthin|Strong
        barony_rochester|dadg_lord_24_5|clan_grey_of_ruthin|Strong
        barony_rye|dadg_lord_11_2|clan_hastings|Weak
        barony_rye|dadg_lord_2_4|clan_lancaster|Weak
        barony_rye|dadg_lord_30_2|clan_fitzhugh|Weak
        barony_rye|dadg_lord_41_2|clan_de_vere|Weak
        barony_rye|dadg_lord_45_2|clan_blount|Weak
        barony_rye|dadg_lord_9_2|clan_neville_of_middleham|Strong
        barony_rye|dadg_lord_9_4|clan_neville_of_middleham|Weak
        barony_rye|dadg_lord_9_5|clan_neville_of_middleham|Weak
        barony_rye|dadg_lord_9_6|clan_neville_of_middleham|Weak
        barony_rye|dadg_lord_9_7|clan_neville_of_middleham|Weak
        barony_rye|dadg_lord_9_8|clan_neville_of_middleham|Weak
        barony_rye|dadg_lord_9_9|clan_neville_of_middleham|Weak
        barony_saint_michaels_mount|dadg_lord_16_2|clan_courtenay|Weak
        barony_saint_michaels_mount|dadg_lord_16_3|clan_courtenay|Weak
        barony_saint_michaels_mount|dadg_lord_16_4|clan_courtenay|Weak
        barony_saint_michaels_mount|dadg_lord_16_5|clan_courtenay|Weak
        barony_saint_michaels_mount|dadg_lord_8_6|clan_clifford|Weak
        barony_scarborough|dadg_lord_31_3|clan_scrope_of_bolton|Strong
        barony_scarborough|dadg_lord_31_6|clan_scrope_of_bolton|Strong
        barony_sherborne|dadg_lord_5_2|clan_grey_of_groby|Weak
        barony_skelton|dadg_lord_30_3|clan_fitzhugh|Strong
        barony_skelton|dadg_lord_30_4|clan_fitzhugh|Strong
        barony_skelton|dadg_lord_31_2|clan_scrope_of_bolton|Weak
        barony_skelton|dadg_lord_43_4|clan_fienne_of_dacre|Weak
        barony_skelton|dadg_lord_49_2|clan_lovell|Weak
        barony_stafford|dadg_lord_36_3|clan_tuchet|Strong
        barony_stafford|dadg_lord_36_4|clan_tuchet|Weak
        barony_swansea|dadg_lord_13_10|clan_herbert|Weak
        barony_swansea|dadg_lord_13_4|clan_herbert|Strong
        barony_swansea|dadg_lord_13_5|clan_herbert|Strong
        barony_swansea|dadg_lord_13_6|clan_herbert|Strong
        barony_swansea|dadg_lord_13_7|clan_herbert|Weak
        barony_swansea|dadg_lord_13_8|clan_herbert|Weak
        barony_swansea|dadg_lord_13_9|clan_herbert|Weak
        barony_tattershall|dadg_lord_35_3|clan_willoughby|Strong
        barony_taunton|dadg_lord_46_3|clan_harrington|Strong
        barony_taunton|dadg_lord_46_4|clan_harrington|Weak
        barony_taunton|dadg_lord_46_5|clan_harrington|Weak
        barony_taunton|dadg_lord_46_6|clan_harrington|Strong
        barony_taunton|dadg_lord_51_2|clan_lumley|Weak
        barony_tonbridge|dadg_lord_47_2|clan_fiennes_of_saye_and_sele|Strong
        barony_tonbridge|dadg_lord_48_2|clan_clinton|Weak
        barony_totnes|dadg_lord_32_3|clan_basset|Strong
        barony_warwick|dadg_lord_11_2|clan_hastings|Weak
        barony_warwick|dadg_lord_2_4|clan_lancaster|Weak
        barony_warwick|dadg_lord_30_2|clan_fitzhugh|Weak
        barony_warwick|dadg_lord_41_2|clan_de_vere|Weak
        barony_warwick|dadg_lord_45_2|clan_blount|Weak
        barony_warwick|dadg_lord_9_2|clan_neville_of_middleham|Strong
        barony_warwick|dadg_lord_9_4|clan_neville_of_middleham|Weak
        barony_warwick|dadg_lord_9_5|clan_neville_of_middleham|Weak
        barony_warwick|dadg_lord_9_6|clan_neville_of_middleham|Weak
        barony_warwick|dadg_lord_9_7|clan_neville_of_middleham|Weak
        barony_warwick|dadg_lord_9_8|clan_neville_of_middleham|Weak
        barony_warwick|dadg_lord_9_9|clan_neville_of_middleham|Weak
        barony_winchester|dadg_lord_15_3|clan_stafford|Strong
        barony_winchester|dadg_lord_19_2|clan_talbot|Weak
        barony_winchester|dadg_lord_29_2|clan_beaumont|Weak
        barony_winchester|dadg_lord_41_4|clan_de_vere|Weak
        barony_winchester|dadg_lord_54_3|clan_beauchamp|Weak
        barony_windsor|dadg_lord_12_3|clan_bourchier|Strong
        barony_windsor|dadg_lord_12_5|clan_bourchier|Strong
        barony_windsor|dadg_lord_12_6|clan_bourchier|Strong
        barony_windsor|dadg_lord_12_8|clan_bourchier|Weak
        barony_windsor|dadg_lord_23_3|clan_mowbray|Weak
        barony_worcester|dadg_lord_53_3|clan_sutton|Strong
        barony_worcester|dadg_lord_53_5|clan_sutton|Weak
        barony_worcester|dadg_lord_53_6|clan_sutton|Strong
        county_bristol|dadg_lord_25_2|clan_grey_of_wilton|Weak
        county_bristol|dadg_lord_32_4|clan_basset|Weak
        county_bristol|dadg_lord_6_2|clan_beaufort|Strong
        county_bristol|dadg_lord_6_3|clan_beaufort|Weak
        county_bristol|dadg_lord_7_3|clan_tudor|Weak
        county_ceredigion|dadg_lord_7_2|clan_tudor|Weak
        county_ceredigion|dadg_lord_7_4|clan_tudor|Weak
        county_cheshire|dadg_lord_1_4|clan_york|Weak
        county_cheshire|dadg_lord_15_5|clan_stafford|Strong
        county_cheshire|dadg_lord_15_6|clan_stafford|Weak
        county_cheshire|dadg_lord_21_2|clan_woodville|Weak
        county_cheshire|dadg_lord_21_3|clan_woodville|Strong
        county_cheshire|dadg_lord_21_4|clan_woodville|Strong
        county_cheshire|dadg_lord_21_5|clan_woodville|Strong
        county_cheshire|dadg_lord_24_6|clan_grey_of_ruthin|Weak
        county_cheshire|dadg_lord_3_3|clan_fitzalan|Weak
        county_cheshire|dadg_mary_woodville_1443|clan_herbert|Weak
        county_cornwall|dadg_lord_16_2|clan_courtenay|Weak
        county_cornwall|dadg_lord_16_3|clan_courtenay|Weak
        county_cornwall|dadg_lord_16_4|clan_courtenay|Weak
        county_cornwall|dadg_lord_16_5|clan_courtenay|Weak
        county_cornwall|dadg_lord_8_6|clan_clifford|Weak
        county_devon|dadg_lord_5_2|clan_grey_of_groby|Weak
        county_essex|dadg_lord_12_3|clan_bourchier|Strong
        county_essex|dadg_lord_12_5|clan_bourchier|Strong
        county_essex|dadg_lord_12_6|clan_bourchier|Strong
        county_essex|dadg_lord_12_8|clan_bourchier|Weak
        county_essex|dadg_lord_23_3|clan_mowbray|Weak
        county_glamorgan|dadg_lord_11_2|clan_hastings|Weak
        county_glamorgan|dadg_lord_2_4|clan_lancaster|Weak
        county_glamorgan|dadg_lord_30_2|clan_fitzhugh|Weak
        county_glamorgan|dadg_lord_41_2|clan_de_vere|Weak
        county_glamorgan|dadg_lord_45_2|clan_blount|Weak
        county_glamorgan|dadg_lord_9_2|clan_neville_of_middleham|Strong
        county_glamorgan|dadg_lord_9_4|clan_neville_of_middleham|Weak
        county_glamorgan|dadg_lord_9_5|clan_neville_of_middleham|Weak
        county_glamorgan|dadg_lord_9_6|clan_neville_of_middleham|Weak
        county_glamorgan|dadg_lord_9_7|clan_neville_of_middleham|Weak
        county_glamorgan|dadg_lord_9_8|clan_neville_of_middleham|Weak
        county_glamorgan|dadg_lord_9_9|clan_neville_of_middleham|Weak
        county_gwynedd|dadg_lord_40_2|clan_devereux|Strong
        county_gwynedd|dadg_lord_40_3|clan_devereux|Strong
        county_gwynedd|dadg_lord_40_4|clan_devereux|Weak
        county_gwynedd|dadg_lord_40_5|clan_devereux|Weak
        county_gwynedd|dadg_lord_40_6|clan_devereux|Strong
        county_gwynedd|dadg_lord_40_7|clan_devereux|Strong
        county_hallamshire|dadg_lord_19_3|clan_talbot|Strong
        county_hallamshire|dadg_lord_19_5|clan_talbot|Strong
        county_hallamshire|dadg_lord_19_6|clan_talbot|Strong
        county_hallamshire|dadg_lord_19_7|clan_talbot|Strong
        county_hallamshire|dadg_lord_19_8|clan_talbot|Strong
        county_hallamshire|dadg_lord_20_2|clan_vernon|Weak
        county_hallamshire|dadg_lord_23_2|clan_mowbray|Weak
        county_hampshire|dadg_lord_15_3|clan_stafford|Strong
        county_hampshire|dadg_lord_19_2|clan_talbot|Weak
        county_hampshire|dadg_lord_29_2|clan_beaumont|Weak
        county_hampshire|dadg_lord_41_4|clan_de_vere|Weak
        county_hampshire|dadg_lord_54_3|clan_beauchamp|Weak
        county_hereford|dadg_lord_15_4|clan_stafford|Weak
        county_hereford|dadg_lord_27_3|clan_neville_of_bergavenny|Strong
        county_hereford|dadg_lord_27_4|clan_neville_of_bergavenny|Strong
        county_hereford|dadg_lord_27_6|clan_neville_of_bergavenny|Weak
        county_hereford|dadg_lord_27_7|clan_neville_of_bergavenny|Weak
        county_hereford|dadg_lord_27_8|clan_neville_of_bergavenny|Weak
        county_hereford|dadg_lord_28_2|clan_stanley|Weak
        county_hull|dadg_lord_1_2|clan_york|Strong
        county_hull|dadg_lord_1_3|clan_york|Strong
        county_hull|dadg_lord_1_5|clan_york|Weak
        county_hull|dadg_lord_1_6|clan_york|Weak
        county_hull|dadg_lord_1_7|clan_york|Strong
        county_hull|dadg_lord_14_3|clan_de_la_pole|Weak
        county_hull|dadg_lord_17_2|clan_holland|Weak
        county_kent|dadg_lord_22_3|clan_greystoke|Weak
        county_kent|dadg_lord_24_3|clan_grey_of_ruthin|Strong
        county_kent|dadg_lord_24_4|clan_grey_of_ruthin|Strong
        county_kent|dadg_lord_24_5|clan_grey_of_ruthin|Strong
        county_lancashire|dadg_lord_2_3|clan_lancaster|Strong
        county_lincoln|dadg_lord_14_4|clan_de_la_pole|Strong
        county_lincoln|dadg_lord_14_5|clan_de_la_pole|Strong
        county_lincoln|dadg_lord_14_6|clan_de_la_pole|Weak
        county_lincoln|dadg_lord_14_7|clan_de_la_pole|Strong
        county_london|dadg_lord_2_3|clan_lancaster|Strong
        county_norwich|dadg_lord_4_3|clan_howard|Strong
        county_norwich|dadg_lord_4_4|clan_howard|Weak
        county_norwich|dadg_lord_4_5|clan_howard|Weak
        county_oxford|dadg_lord_41_3|clan_de_vere|Strong
        county_oxford|dadg_lord_41_6|clan_de_vere|Strong
        county_oxford|dadg_lord_41_8|clan_de_vere|Strong
        county_pembroke|dadg_lord_13_10|clan_herbert|Weak
        county_pembroke|dadg_lord_13_4|clan_herbert|Strong
        county_pembroke|dadg_lord_13_5|clan_herbert|Strong
        county_pembroke|dadg_lord_13_6|clan_herbert|Strong
        county_pembroke|dadg_lord_13_7|clan_herbert|Weak
        county_pembroke|dadg_lord_13_8|clan_herbert|Weak
        county_pembroke|dadg_lord_13_9|clan_herbert|Weak
        county_shropshire|dadg_lord_19_3|clan_talbot|Strong
        county_shropshire|dadg_lord_19_5|clan_talbot|Strong
        county_shropshire|dadg_lord_19_6|clan_talbot|Strong
        county_shropshire|dadg_lord_19_7|clan_talbot|Strong
        county_shropshire|dadg_lord_19_8|clan_talbot|Strong
        county_shropshire|dadg_lord_20_2|clan_vernon|Weak
        county_shropshire|dadg_lord_23_2|clan_mowbray|Weak
        county_suffolk|dadg_lord_14_4|clan_de_la_pole|Strong
        county_suffolk|dadg_lord_14_5|clan_de_la_pole|Strong
        county_suffolk|dadg_lord_14_6|clan_de_la_pole|Weak
        county_suffolk|dadg_lord_14_7|clan_de_la_pole|Strong
        county_sussex|dadg_lord_3_2|clan_fitzalan|Strong
        county_sussex|dadg_lord_3_4|clan_fitzalan|Strong
        county_sussex|dadg_lord_3_5|clan_fitzalan|Strong
        county_sussex|dadg_lord_3_6|clan_fitzalan|Strong
        county_sussex|dadg_lord_3_7|clan_fitzalan|Weak
        county_warkworth|dadg_lord_10_3|clan_percy|Weak
        county_warkworth|dadg_lord_10_4|clan_percy|Weak
        county_warkworth|dadg_lord_41_7|clan_de_vere|Weak
        county_warwick|dadg_lord_2_3|clan_lancaster|Strong
        county_york|dadg_lord_1_2|clan_york|Strong
        county_york|dadg_lord_1_3|clan_york|Strong
        county_york|dadg_lord_1_5|clan_york|Weak
        county_york|dadg_lord_1_6|clan_york|Weak
        county_york|dadg_lord_1_7|clan_york|Strong
        county_york|dadg_lord_14_3|clan_de_la_pole|Weak
        county_york|dadg_lord_17_2|clan_holland|Weak
        duchy_lancaster|dadg_lord_2_3|clan_lancaster|Strong
        duchy_northumberland|dadg_lord_10_3|clan_percy|Weak
        duchy_northumberland|dadg_lord_10_4|clan_percy|Weak
        duchy_northumberland|dadg_lord_41_7|clan_de_vere|Weak
        duchy_somerset|dadg_lord_25_2|clan_grey_of_wilton|Weak
        duchy_somerset|dadg_lord_32_4|clan_basset|Weak
        duchy_somerset|dadg_lord_6_2|clan_beaufort|Strong
        duchy_somerset|dadg_lord_6_3|clan_beaufort|Weak
        duchy_somerset|dadg_lord_7_3|clan_tudor|Weak
        duchy_wales|dadg_lord_2_3|clan_lancaster|Strong
        duchy_york|dadg_lord_1_2|clan_york|Strong
        duchy_york|dadg_lord_1_3|clan_york|Strong
        duchy_york|dadg_lord_1_5|clan_york|Weak
        duchy_york|dadg_lord_1_6|clan_york|Weak
        duchy_york|dadg_lord_1_7|clan_york|Strong
        duchy_york|dadg_lord_14_3|clan_de_la_pole|Weak
        duchy_york|dadg_lord_17_2|clan_holland|Weak
        kingdom_england|dadg_lord_2_3|clan_lancaster|Strong
        """;

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void Real1471ContentProducesTheExpectedInitialClaims()
    {
        using Stream titlesStream = DadgContent.OpenTitles();
        using Stream heroesStream = DadgContent.OpenHeroes();
        using Stream charactersStream = DadgContent.OpenCharacters();
        using Stream clansStream = DadgContent.OpenClans();

        DadgXmlGenealogy genealogy = DadgXmlGenealogy.Load(heroesStream, charactersStream, clansStream);
        var feudalStructure = new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream));
        var titleRepository = new InMemoryTitleRegistry(genealogy);
        titleRepository.Initialise(feudalStructure.BuildInitialTitles(genealogy));

        var claimRepository = new InMemoryClaimRegistry();
        var useCase = new GenerateBloodClaimsUseCase(titleRepository, claimRepository, genealogy);

        IReadOnlyList<Claim> claims = useCase.Execute();
        Assert.Equal(ExpectedClaims, Project(claims));
        Assert.Equal(97, titleRepository.GetAllTitles().Count);
        Assert.Equal(
            53,
            titleRepository.GetAllTitles()
                .Where(title => genealogy.GetHolderClanOf(title) is not null)
                .Select(title => genealogy.GetHolderClanOf(title))
                .Distinct()
                .Count());
        Assert.Equal(442, genealogy.HeroCount);
        Assert.Equal(399, claims.Count);
        Assert.Equal(183, claims.Count(claim => claim.Strength == ClaimStrength.Strong));
        Assert.Equal(216, claims.Count(claim => claim.Strength == ClaimStrength.Weak));
        Assert.Equal(53, claims.Select(claim => claim.ClaimantClanId).Distinct().Count());
        Assert.Equal(89, claims.Select(claim => claim.TitleId).Distinct().Count());
        Assert.All(claims, claim => Assert.False(string.IsNullOrEmpty(claim.ClaimantHeroId)));
        Assert.Equal(claims.Count, claims.Select(claim => claim.Id).Distinct().Count());
        Assert.All(
            titleRepository.GetAllTitles().Where(title => genealogy.GetHolderClanOf(title) is not null),
            title =>
            {
                string? leaderId = genealogy.GetClanLeaderId(genealogy.GetHolderClanOf(title)!);
                Assert.False(string.IsNullOrEmpty(leaderId));
                Assert.NotNull(genealogy.GetHero(leaderId!));
            });
        Assert.All(
            claims,
            claim =>
            {
                HeroNode claimant = Assert.IsType<HeroNode>(genealogy.GetHero(claim.ClaimantHeroId!));
                Title title = Assert.IsType<Title>(titleRepository.GetTitle(claim.TitleId));
                Assert.True(claimant.IsAlive);
                Assert.Equal(claimant.ClanId, claim.ClaimantClanId);
                Assert.NotEqual(title.HolderHeroId, claim.ClaimantHeroId);
                Assert.Equal(ClaimOrigin.Inheritance, claim.Origin);
                Assert.Equal($"{claim.TitleId}:{claim.ClaimantHeroId}:blood", claim.Id);
            });

        AssertWeakClaim(claims, "clan_de_la_pole", "duchy_york");
        AssertWeakClaim(claims, "clan_holland", "duchy_york");
        AssertWeakClaim(claims, "clan_tudor", "duchy_somerset");

        // Kin inside the holding house hold Strong claims of their own: Warwick's brother on
        // Middleham, Somerset's brother on the duchy he stands to inherit, and two of York's
        // own on the duchy. This is what lets a house go to war with itself.
        AssertStrongClaim(claims, "dadg_lord_9_2", "barony_middleham", "clan_neville_of_middleham");
        AssertStrongClaim(claims, "dadg_lord_6_2", "duchy_somerset", "clan_beaufort");
        AssertStrongClaim(claims, "dadg_lord_1_2", "duchy_york", "clan_york");
        AssertStrongClaim(claims, "dadg_lord_1_3", "duchy_york", "clan_york");
    }

    private static string Project(IEnumerable<Claim> claims) =>
        string.Join(
            "\n",
            claims
                .OrderBy(claim => claim.TitleId)
                .ThenBy(claim => claim.ClaimantHeroId)
                .ThenBy(claim => claim.ClaimantClanId)
                .ThenBy(claim => claim.Strength)
                .Select(claim =>
                    $"{claim.TitleId}|{claim.ClaimantHeroId}|{claim.ClaimantClanId}|{claim.Strength}"));

    private static void AssertWeakClaim(IEnumerable<Claim> claims, string claimantClanId, string titleId)
    {
        Assert.Contains(
            claims,
            claim => claim.ClaimantClanId == claimantClanId
                     && claim.TitleId == titleId
                     && claim.Strength == ClaimStrength.Weak
                     && claim.Origin == ClaimOrigin.Inheritance);
    }

    private static void AssertStrongClaim(
        IEnumerable<Claim> claims,
        string claimantHeroId,
        string titleId,
        string claimantClanId)
    {
        Assert.Contains(
            claims,
            claim => claim.ClaimantHeroId == claimantHeroId
                     && claim.TitleId == titleId
                     && claim.ClaimantClanId == claimantClanId
                     && claim.Strength == ClaimStrength.Strong
                     && claim.Origin == ClaimOrigin.Inheritance);
    }
}
