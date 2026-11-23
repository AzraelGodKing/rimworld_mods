using UnityEngine;
using Verse;

namespace LLMStoryteller
{
    public class LLMStorytellerSettings : ModSettings
    {
        public const string OllamaEndpoint = "http://localhost:11434/v1";
        public const string LmStudioEndpoint = "http://localhost:1234/v1";
        public const string DefaultModel = "llama3.1:8b";
        public const float MinIntensity = 0.5f;

        public bool useModel = true;
        public string endpoint = OllamaEndpoint;
        public string model = DefaultModel;
        public string apiKey = "";
        public int timeoutSeconds = 60;
        public float hoursBetweenDecisions = 24f;
        public float maxIntensity = 1.5f;
        public float temperature = 0.8f;
        public int maxMenuOptions = 25;
        public bool reasoningLetters = true;
        public string personality = StoryPrompt.DefaultPersonality;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref useModel, "useModel", true);
            Scribe_Values.Look(ref endpoint, "endpoint", OllamaEndpoint);
            Scribe_Values.Look(ref model, "model", DefaultModel);
            Scribe_Values.Look(ref apiKey, "apiKey", "");
            Scribe_Values.Look(ref timeoutSeconds, "timeoutSeconds", 60);
            Scribe_Values.Look(ref hoursBetweenDecisions, "hoursBetweenDecisions", 24f);
            Scribe_Values.Look(ref maxIntensity, "maxIntensity", 1.5f);
            Scribe_Values.Look(ref temperature, "temperature", 0.8f);
            Scribe_Values.Look(ref maxMenuOptions, "maxMenuOptions", 25);
            Scribe_Values.Look(ref reasoningLetters, "reasoningLetters", true);
            Scribe_Values.Look(ref personality, "personality", StoryPrompt.DefaultPersonality);
        }

        Vector2 scroll;

        public void DoWindowContents(Rect inRect)
        {
            var view = new Rect(0f, 0f, inRect.width - 20f, 900f);
            Widgets.BeginScrollView(inRect, ref scroll, view);
            var l = new Listing_Standard();
            l.Begin(view);

            l.CheckboxLabeled("LLMStoryteller_UseModel".Translate(), ref useModel, "LLMStoryteller_UseModelTip".Translate());
            l.GapLine();

            l.Label("LLMStoryteller_Presets".Translate());
            var row = l.GetRect(30f);
            if (Widgets.ButtonText(row.LeftHalf().ContractedBy(2f), "LLMStoryteller_PresetOllama".Translate()))
                endpoint = OllamaEndpoint;
            if (Widgets.ButtonText(row.RightHalf().ContractedBy(2f), "LLMStoryteller_PresetLmStudio".Translate()))
                endpoint = LmStudioEndpoint;
            endpoint = l.TextEntryLabeled("LLMStoryteller_Endpoint".Translate(), endpoint);
            model = l.TextEntryLabeled("LLMStoryteller_Model".Translate(), model);
            apiKey = l.TextEntryLabeled("LLMStoryteller_ApiKey".Translate(), apiKey);

            if (l.ButtonText("LLMStoryteller_TestConnection".Translate()))
                LlmClient.StartConnectionTest(this);
            l.Label(LlmClient.ConnectionTestStatus);
            l.GapLine();

            timeoutSeconds = (int)l.SliderLabeled("LLMStoryteller_Timeout".Translate(timeoutSeconds), timeoutSeconds, 10f, 180f);
            hoursBetweenDecisions = Mathf.Round(l.SliderLabeled("LLMStoryteller_Interval".Translate(hoursBetweenDecisions.ToString("0")),
                hoursBetweenDecisions, 6f, 72f));
            maxIntensity = l.SliderLabeled("LLMStoryteller_MaxIntensity".Translate(maxIntensity.ToString("0.00")), maxIntensity, 1f, 2.5f);
            temperature = l.SliderLabeled("LLMStoryteller_Temperature".Translate(temperature.ToString("0.00")), temperature, 0f, 1.5f);
            maxMenuOptions = (int)l.SliderLabeled("LLMStoryteller_MaxOptions".Translate(maxMenuOptions), maxMenuOptions, 8f, 60f);
            l.CheckboxLabeled("LLMStoryteller_ReasoningLetters".Translate(), ref reasoningLetters);
            l.GapLine();

            l.Label("LLMStoryteller_Personality".Translate());
            personality = Widgets.TextArea(l.GetRect(120f), personality);
            if (l.ButtonText("LLMStoryteller_ResetPersonality".Translate(), widthPct: 0.4f))
                personality = StoryPrompt.DefaultPersonality;

            l.End();
            Widgets.EndScrollView();
        }
    }
}
