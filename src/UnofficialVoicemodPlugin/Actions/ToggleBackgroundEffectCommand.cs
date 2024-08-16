namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;

    using Loupedeck.UnofficialVoicemodPlugin;

    public class ToggleBackgroundEffectCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public ToggleBackgroundEffectCommand()
            : base(displayName: "Toggle Background Effect", description: "Toggle the background effect on/off", groupName: "Voicemod Commands")
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

                var response = await _apiWrapper.ToggleBackgroundEffectAsync();

                string backgroundEffectDisplay = "Off";
                if (response.ActionType == "backgroundEffectsEnabledEvent")
                {
                    backgroundEffectDisplay = "On";
                }

                this.DisplayName = "Background Effect: " + backgroundEffectDisplay;

                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }
}
