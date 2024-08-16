namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using Loupedeck.UnofficialVoicemodPlugin;

    public class ToggleMuteMicCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public ToggleMuteMicCommand()
            : base(displayName: "Toggle Mic", description: "Toggle the microphone mute on/off", groupName: "Voicemod Commands")
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

                var response = await _apiWrapper.ToggleMuteMicAsync();

                string micDisplay = "Off";
                if (response.ActionType == "muteMicrophoneDisabledEvent")
                {
                    micDisplay = "On";
                }

                this.DisplayName = "Mic: " + micDisplay;

                this.ActionImageChanged();
            } catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }
}
