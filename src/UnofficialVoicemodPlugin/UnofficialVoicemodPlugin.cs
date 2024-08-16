namespace Loupedeck.UnofficialVoicemodPlugin
{
    using System;
    using System.IO;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class UnofficialVoicemodPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public UnofficialVoicemodPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded during the Loupedeck service start-up.
        public override void Load()
        {
            // initialize to setup websocket
            try
            {
                var clientKey = LoadClientKey();
                if (!string.IsNullOrEmpty(clientKey))
                {
                    VoicemodApiWrapper.Initialize(clientKey);
                }
                else
                {
                    PluginLog.Error("Client key not found");
                }
            } catch (Exception ex)
            {
                PluginLog.Error(ex, ex.Message);
            }
        }

        // This method is called when the plugin is unloaded during the Loupedeck service shutdown.
        public override void Unload()
        {
        }

        private string LoadClientKey()
        {
            try
            {
                string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string folderPath = Path.Combine(userDirectory, ".loupedeck", "UnofficialVoicemod");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(userDirectory, ".loupedeck", "UnofficialVoicemod", "clientKey.txt");

                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
                else
                {
                    File.WriteAllText(filePath, "");
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                PluginLog.Error(ex, ex.Message);
                return $"Error: {ex.Message}";
            }
        }
    }
}
