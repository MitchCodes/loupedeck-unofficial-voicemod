namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using Loupedeck.UnofficialVoicemodPlugin;

    public class ToggleHearMyVoiceCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public ToggleHearMyVoiceCommand()
            : base(displayName: "Toggle Hear Self", description: "Toggle the Hear My Voice feature on/off", groupName: "Voicemod Commands")
        {
        }

        protected override async void RunCommand(String actionParameter)
        {
            try
            {
                if (_apiWrapper == null)
                {
                    _apiWrapper = new VoicemodApiWrapper();
                }

                var response = await _apiWrapper.ToggleHearMyVoiceAsync();

                string hearSelfDisplay = "Off";
                if (response.ActionType == "hearMySelfEnabledEvent")
                {
                    hearSelfDisplay = "On";
                }

                this.DisplayName = "Hear Self: " + hearSelfDisplay;

                this.ActionImageChanged();
            } catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }
}
