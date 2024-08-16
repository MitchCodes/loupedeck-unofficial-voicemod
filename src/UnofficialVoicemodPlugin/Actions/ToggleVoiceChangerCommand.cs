namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using Loupedeck.UnofficialVoicemodPlugin;

    public class ToggleVoiceChangerCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public ToggleVoiceChangerCommand()
            : base(displayName: "Toggle Voice Changer", description: "Toggle the voice changer on/off", groupName: "Voicemod Commands")
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

                var response = await this._apiWrapper.ToggleVoiceChangerAsync();

                string voiceChangerDisplay = "On";
                if (response.ActionType == "voiceChangerDisabledEvent")
                {
                    voiceChangerDisplay = "Off";
                }

                this.DisplayName = "Voice Changer: " + voiceChangerDisplay;

                this.ActionImageChanged();
            } catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }
}
