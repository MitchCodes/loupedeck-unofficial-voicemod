namespace Loupedeck.UnofficialVoicemodPlugin.Actions
{
    using System;
    using System.Linq;


    public class LoadVoiceCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public LoadVoiceCommand()
            : base(displayName: "Load Voice", description: "Load a specific voice", groupName: "Voicemod Commands")
        {
            this.MakeProfileAction("text;Enter voice name:");
        }

        protected override async void RunCommand(String actionParameter)
        {
            try
            {
                if (_apiWrapper == null)
                {
                    _apiWrapper = new VoicemodApiWrapper();
                }

                this.Plugin.TryGetPluginSetting("Voice_" + actionParameter, out string voiceId);
                if (String.IsNullOrEmpty(voiceId))
                {
                    var voices = await _apiWrapper.GetVoicesAsync();
                    var voice = voices.ActionObject.Voices.FirstOrDefault(v => v.FriendlyName == actionParameter);

                    if (voice != null)
                    {
                        voiceId = voice.Id;
                    }

                    this.Plugin.SetPluginSetting("Voice_" + actionParameter, voiceId);
                }

                await _apiWrapper.LoadVoiceAsync(voiceId);

                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }
}
