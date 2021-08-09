using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using UnityEngine;
using RimWorld;
using Verse;

namespace O21Anima
{
    public class AntiAnimaMod : Mod
    {
        public static AntiAnimaMod mod;
        public static AntiAnimaSettings settings;

        public AntiAnimaPage currentPage = AntiAnimaPage.Anima_Scream;

        public AntiAnimaMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<AntiAnimaSettings>();
            mod = this;
        }

        public override string SettingsCategory()
        {
            return "Adjustable Anima";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float secondStageHeight;
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.ValueLabeled("Settings Page", "Cycle this setting to change page.", ref currentPage);
            listingStandard.GapLine();
            listingStandard.Gap(38);
            secondStageHeight = listingStandard.CurHeight;
            listingStandard.End();

            Listing_Standard ls = new Listing_Standard();
            inRect.yMin = secondStageHeight;
            ls.Begin(inRect);
            if(currentPage == AntiAnimaPage.Anima_Scream)
            {
                ls.CheckboxLabeled("Anima Scream", ref settings.animaScream, "Enables/Disables the Anima Scream debuff from chopping them down.");
                ls.Label("If disabled, the message and sound effect will still happen, but a null thought will be applied for less than a fraction of a second instead, resulting in no debuff/buff.");
                if (settings.animaScream)
                {
                    ls.GapLine();
                    ls.Label("Anima Scream Debuff Value");
                    ls.Label("Default: -6, Min-Max: -20 - 20, Current: " + settings.animaScreamDebuff.ToString("0"));
                    settings.animaScreamDebuff = Mathf.RoundToInt(ls.Slider(settings.animaScreamDebuff, -20, 20));
                    ls.Label("Anima Scream Duration (in days)");
                    ls.Label("Default: 5, Min-Max: 0.1 - 20, Current: " + settings.animaScreamLength.ToString("0.0"));
                    settings.animaScreamLength = ls.Slider(settings.animaScreamLength, 0.1f, 20f);
                }
            }
            if(currentPage == AntiAnimaPage.Anima_Tree)
            {
                ls.Label("Building Radius", tooltip: "Enables/Disables the radius around the tree in which artificial building make it more or less effective.");
                {
                    ls.GapLine();
                    ls.Label("Artificial Building Radius", tooltip: "The radius in which a debuff is applied to the trees effects if artificial buildings are built in it.");
                    ls.Label("Default: 34.9, Min-Max: 0.1 - 40.9, Current: " + settings.buildingRadius.ToString("0.0"));
                    settings.buildingRadius = ls.Slider(settings.buildingRadius, 0.1f, 40.9f);
                    ls.Label("Natural Building Radius", tooltip: "The radius in which a buff is applied to the trees effects for natural buildings.");
                    ls.Label("Default: 9.9, Min-Max: 0.1 - 40.9, Current: " + settings.buffBuildingRadius.ToString("0.0"));
                    settings.buffBuildingRadius = ls.Slider(settings.buffBuildingRadius, 0.1f, 40.9f);
                    ls.Label("Max Natural Buildings", tooltip: "This is the maximum number of buildings which can buff the Anima Tree, default sucks taint.");
                    ls.Label("Default: 4, Min-Max: 1 - 40, Current: " + settings.maxBuffBuildings.ToString("0"));
                    settings.maxBuffBuildings = Mathf.RoundToInt(ls.Slider(settings.maxBuffBuildings, 1, 40));
                }
            }
            if(currentPage == AntiAnimaPage.Anima_Grass) 
            {
                ls.Label("Anima Grass per Psylink Level");
                {
                    ls.GapLine();
                    if (GetPsylinkStuff)
                    {
                        int levelint = 0;
                        for (int i = 0; i < settings.psylinkLevelNeeds.Count; i++)
                        {
                            levelint++;
                            string intBufferString = settings.psylinkLevelNeeds[i].ToString();
                            int intBufferInt = settings.psylinkLevelNeeds[i];
                            ls.TextFieldNumericLabeled("Psylink Level " + levelint, ref intBufferInt, ref intBufferString, 0, 500);
                            settings.psylinkLevelNeeds[i] = intBufferInt;
                        }
                    }
                }
            }
            if(currentPage == AntiAnimaPage.Anima_Focus)
            {
                ls.Label("Most of the settings on this page require a restart to be enabled/disabled. Sliders are applied immediately.");
                ls.GapLine();
                ls.CheckboxLabeled("All meditation items/buildings allow all meditation types", ref settings.meditationAll, "Pretty self explanatory.");
                ls.CheckboxLabeled("Nature shrines always buildable", ref settings.buildableShrines, "Nature Shrines are usually only buildable when you have a nature based Psycaster.");
                ls.CheckboxLabeled("No backstory restrictions for Natural/Artistic meditation", ref settings.allPawnsNaturalArtistic, "Again, pretty self explanatory.");
                ls.Label("Artificial Building Radius", tooltip: "The radius in which a debuff is applied to the focus object effects if artificial buildings are built in it.");
                ls.Label("Default: 34.9, Min-Max: 0.1 - 40.9, Current: " + settings.shrineBuildingRadius.ToString("0.0"));
                settings.shrineBuildingRadius = Mathf.Round(ls.Slider(settings.shrineBuildingRadius, 0.1f, 40.9f) * 100f) / 100f;
                ls.Label("Natural Building Radius", tooltip: "The radius in which a buff is applied to the shrine effects for natural buildings.");
                ls.Label("Default: 9.9, Min-Max: 0.1 - 40.9, Current: " + settings.shrineBuffBuildingRadius.ToString("0.0"));
                settings.shrineBuffBuildingRadius = Mathf.Round(ls.Slider(settings.shrineBuffBuildingRadius, 0.1f, 40.9f) * 100f) / 100f;
                ls.Label("Meditation Psyfocus Gain Rate", tooltip: "The amount of psyfocus a pawn gains per day of mediation.");
                ls.Label("Default: 0.5, Min-Max: 0.1 - 20.0, Current: " + settings.meditationGain.ToString("0.0"));
                settings.meditationGain = Mathf.Round(ls.Slider(settings.meditationGain, 0.1f, 20.0f) * 100f) / 100f;
            }
            ls.End();

            AntiAnimaStartup.ApplySettingsNow(settings);

            base.DoSettingsWindowContents(inRect);
        }

        public bool GetPsylinkStuff
        {
            get
            {
                if (settings.psylinkLevelNeeds.NullOrEmpty())
                {
                    CompProperties_Psylinkable psycomp = AnimaDefOf.Plant_TreeAnima.GetCompProperties<CompProperties_Psylinkable>();
                    settings.psylinkLevelNeeds = psycomp.requiredSubplantCountPerPsylinkLevel;
                }

                return true;
            }
        }
    }

    public enum AntiAnimaPage
    {
        Anima_Scream,
        Anima_Tree,
        Anima_Grass,
        Anima_Focus
    }

    public class AntiAnimaSettings : ModSettings
    {
        public bool animaScream = true;
        public int animaScreamDebuff = -6;
        public float animaScreamLength = 5f;
        public float buildingRadius = 34.9f;
        public float shrineBuildingRadius = 34.9f;
        public float buffBuildingRadius = 9.9f;
        public float shrineBuffBuildingRadius = 9.9f;
        public int maxBuffBuildings = 4;
        public List<int> psylinkLevelNeeds = new List<int>();
        public bool meditationAll = false;
        public bool buildableShrines = false;
        public bool allPawnsNaturalArtistic = false;
        public float meditationGain = 0.5f;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref animaScream, "animaScream", true);
            Scribe_Values.Look(ref animaScreamDebuff, "animaScreamDebuff", -6);
            Scribe_Values.Look(ref animaScreamLength, "animaScreamLength", 5f);
            Scribe_Values.Look(ref buildingRadius, "buildingRadius", 34.9f);
            Scribe_Values.Look(ref shrineBuildingRadius, "shrineBuildingRadius", 34.9f);
            Scribe_Values.Look(ref buffBuildingRadius, "buffBuildingRadius", 9.9f);
            Scribe_Values.Look(ref shrineBuffBuildingRadius, "shrineBuffBuildingRadius", 9.9f);
            Scribe_Values.Look(ref maxBuffBuildings, "maxBuffBuildings", 4);
            Scribe_Collections.Look(ref psylinkLevelNeeds, "psylinkLevelNeeds");
            Scribe_Values.Look(ref meditationAll, "meditationAll", false);
            Scribe_Values.Look(ref buildableShrines, "buildableShrines", false);
            Scribe_Values.Look(ref allPawnsNaturalArtistic, "allPawnsNaturalArtistic", false);
            Scribe_Values.Look(ref meditationGain, "meditationGain", 0.5f);
        }

        public IEnumerable<string> GetEnabledSettings
        {
            get
            {
                return GetType().GetFields().Where(p => p.FieldType == typeof(bool) && (bool)p.GetValue(this)).Select(p => p.Name);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class AntiAnimaStartup
    {
        static AntiAnimaStartup()
        {
            ApplySettingsNow(AntiAnimaMod.settings);
        }

        public static void ApplySettingsNow(AntiAnimaSettings s)
        {
            if (AntiAnimaMod.mod.GetPsylinkStuff)
            {
                // Just loads the initial settings for psylink levels.
            }

            // Deal with the Tree Scream
            if (!s.animaScream)
            {
                AnimaDefOf.Plant_TreeAnima.GetCompProperties<CompProperties_GiveThoughtToAllMapPawnsOnDestroy>().thought = AnimaDefOf.O21_NullThought;
            }
            else
            {
                AnimaDefOf.Plant_TreeAnima.GetCompProperties<CompProperties_GiveThoughtToAllMapPawnsOnDestroy>().thought = AnimaDefOf.AnimaScream;
                AnimaDefOf.AnimaScream.stages.First().baseMoodEffect = s.animaScreamDebuff;
                AnimaDefOf.AnimaScream.durationDays = s.animaScreamLength;
            }

            // Deal with Tree Radii
            CompProperties_MeditationFocus focus = AnimaDefOf.Plant_TreeAnima.GetCompProperties<CompProperties_MeditationFocus>();

            FocusStrengthOffset_ArtificialBuildings artificialOffset = 
                (FocusStrengthOffset_ArtificialBuildings)focus.offsets.Find(os => os.GetType() == typeof(FocusStrengthOffset_ArtificialBuildings));
            FocusStrengthOffset_BuildingDefs naturalOffset = 
                (FocusStrengthOffset_BuildingDefs)focus.offsets.Find(os => os.GetType() == typeof(FocusStrengthOffset_BuildingDefs));

            artificialOffset.radius = s.buildingRadius;

            StatPart_ArtificalBuildingsNearbyOffset nearbyBuildingOffset = (StatPart_ArtificalBuildingsNearbyOffset)StatDefOf.MeditationPlantGrowthOffset.parts.Find(sp => sp.GetType() == typeof(StatPart_ArtificalBuildingsNearbyOffset));
            nearbyBuildingOffset.radius = s.buildingRadius;

            naturalOffset.radius = s.buffBuildingRadius;
            naturalOffset.maxBuildings = s.maxBuffBuildings;

            // Deal with grass requirements
            CompProperties_Psylinkable psycomp = AnimaDefOf.Plant_TreeAnima.GetCompProperties<CompProperties_Psylinkable>();
            psycomp.requiredSubplantCountPerPsylinkLevel = s.psylinkLevelNeeds;


            // Get all viable shrines quietly
            List<ThingDef> shrineList = new List<ThingDef>();
            // Shrine Small
            ThingDef shrine_natureSmall = DefDatabase<ThingDef>.GetNamedSilentFail("NatureShrine_Small");
            if (shrine_natureSmall != null)
            {
                shrineList.Add(shrine_natureSmall);
            }
            // Shrine Large
            ThingDef shrine_natureLarge = DefDatabase<ThingDef>.GetNamedSilentFail("NatureShrine_Large");
            if (shrine_natureLarge != null)
            {
                shrineList.Add(shrine_natureLarge);
            }
            // Animus Stone
            ThingDef shrine_animusStone = DefDatabase<ThingDef>.GetNamedSilentFail("AnimusStone");
            if (shrine_animusStone != null)
            {
                shrineList.Add(shrine_animusStone);
            }
            // Runestone
            ThingDef shrine_runestone = DefDatabase<ThingDef>.GetNamedSilentFail("VFEV_RuneStone");
            if(shrine_runestone != null)
            {
                shrineList.Add(shrine_runestone);
            }

            // Deal with shrines
            if (!shrineList.NullOrEmpty())
            {
                foreach (ThingDef shrine in shrineList)
                {
                    CompProperties_MeditationFocus shrineFocus = shrine.GetCompProperties<CompProperties_MeditationFocus>();

                    FocusStrengthOffset_ArtificialBuildings shrineRange =
                        (FocusStrengthOffset_ArtificialBuildings)shrineFocus.offsets.Find(os => os.GetType() == typeof(FocusStrengthOffset_ArtificialBuildings));
                    FocusStrengthOffset_BuildingDefs shrineNatureRange =
                        (FocusStrengthOffset_BuildingDefs)focus.offsets.Find(os => os.GetType() == typeof(FocusStrengthOffset_BuildingDefs));

                    shrineRange.radius = s.shrineBuildingRadius;
                    shrineNatureRange.radius = s.shrineBuffBuildingRadius;
                }
            }

            // Deal with Psyfocus regen rate

            StatDefOf.MeditationFocusGain.defaultBaseValue = s.meditationGain;
        }
    }

    [DefOf]
    public static class AnimaDefOf
    {
        static AnimaDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnimaDefOf));
        }

        public static ThingDef Plant_TreeAnima;
        public static ThoughtDef AnimaScream;
        public static ThoughtDef O21_NullThought;
    }
}
