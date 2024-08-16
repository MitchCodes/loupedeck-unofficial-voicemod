namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using Loupedeck.UnofficialVoicemodPlugin;

    public class SelectRandomVoiceCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public SelectRandomVoiceCommand()
            : base(displayName: "Select Random Voice", description: "Select a random voice", groupName: "Voicemod Commands")
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

                await _apiWrapper.SelectRandomVoiceAsync("AllVoices");
                this.ActionImageChanged();
            } catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }

    public class SelectRandomFavoriteVoiceCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public SelectRandomFavoriteVoiceCommand()
            : base(displayName: "Select Random Favorite Voice", description: "Select a random voice from your favorites", groupName: "Voicemod Commands")
        {
        }

        protected override async void RunCommand(String actionParameter)
        {
            if (_apiWrapper == null)
            {
                _apiWrapper = new VoicemodApiWrapper();
            }

            await _apiWrapper.SelectRandomVoiceAsync("FavoriteVoices");
            this.ActionImageChanged();
        }
    }

    public class SelectRandomCustomVoiceCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public SelectRandomCustomVoiceCommand()
            : base(displayName: "Select Random Custom Voice", description: "Select a random voice from your customs", groupName: "Voicemod Commands")
        {
        }

        protected override async void RunCommand(String actionParameter)
        {
            if (_apiWrapper == null)
            {
                _apiWrapper = new VoicemodApiWrapper();
            }

            await _apiWrapper.SelectRandomVoiceAsync("CustomVoices");
            this.ActionImageChanged();
        }
    }
}
