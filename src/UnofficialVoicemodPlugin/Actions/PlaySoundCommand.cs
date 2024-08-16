namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Loupedeck.UnofficialVoicemodPlugin;

    using static Loupedeck.UnofficialVoicemodPlugin.GetMemesResponse.GetMemesResponseActionObject;

    public class PlaySoundCommand : PluginDynamicCommand
    {
        private VoicemodApiWrapper _apiWrapper;

        public PlaySoundCommand()
            : base(displayName: "Play Soundboard Sound", description: "Play a soundboard sound", groupName: "Voicemod Commands")
        {
            this.MakeProfileAction("text;Enter sound name:");
        }

        protected override async void RunCommand(String actionParameter)
        {
            try
            {
                if (_apiWrapper == null)
                {
                    _apiWrapper = new VoicemodApiWrapper();
                }

                this.Plugin.TryGetPluginSetting("SoundboardSound_" + actionParameter, out string soundId);
                if (String.IsNullOrEmpty(soundId))
                {
                    var memes = await _apiWrapper.GetMemesAsync();
                    var sound = memes.ActionObject.ListOfMemes.FirstOrDefault(meme => meme.Name == actionParameter);

                    if (sound != null)
                    {
                        soundId = sound.FileName;
                    }

                    this.Plugin.SetPluginSetting("SoundboardSound_" + actionParameter, soundId);
                }

                var response = await _apiWrapper.PlayMemeAsync(soundId, true);

                this.ActionImageChanged();
            } catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }
    }
}
