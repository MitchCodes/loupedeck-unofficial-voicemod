namespace Loupedeck.UnofficialVoicemodPlugin.Actions
{
    using System;

    public class StopAllMemeSoundCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public StopAllMemeSoundCommand()
            : base(displayName: "Stop All Sounds", description: "Stop all soundboard sounds", groupName: "Voicemod Commands")
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

                await _apiWrapper.StopAllMemeSoundsAsync();

                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }
}