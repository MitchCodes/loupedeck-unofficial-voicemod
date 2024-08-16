# Loupedeck Unofficial Voicemod Plugin

## Description
This project was created because the official Loupedeck Voicemod plugin was not working for me with Voicemod version 3.

This plugin is rough around the edges but gets the job done for the most basic scenarios. It is not a replacement for the official plugin, but it is a temporary solution until the official plugin is updated.

## Features
- Feature 1: Playing a soundboard sound & stopping all sounds
- Feature 2: Changing the voice changer to a specific voice or random voice
- Feature 3: Toggling microphone, hearing self, background effect and voice changer

## Installation
1. Download the latest release from the releases page (.lplug4 file).
2. Double click the lplug4 file and the Loupedeck software should open and install the plugin.
3. Get a client key for the Voicemod Control API from the Voicemod developer page:
4. Make sure the %USERPROFILE%\.loupedeck\UnofficialVoicemod folder is created. The plugin will create this file for you the first time you install if you want to skip this step.
5. Enter your client key into the %USERPROFILE%\.loupedeck\UnofficialVoicemod\clientKey.txt file and save it.
6. Restart Loupedeck software completely (need to exit Loupedeck2.exe from task manager) or restart the computer and the plugin should be configured correctly. Sorry about this annoying step! Definitely a big potential for improvement to make it easier. Luckily, you only need to do this once.

## Developer Installation
1. Install Visual Studio 2022 and make sure .NET Framework 4.7.2 is installed with it.
2. Open the .csproj file and make sure the build targets and paths all point correctly to where Loupedeck is installed. During the build event the plugin is copied to the Loupedeck plugins folder and debugging is setup to work as well.
3. Make sure the references are not broken in the references folder. If they are, you can find the necessary DLLs in the Loupedeck installation folder. E.g. 'C:\Program Files (x86)\Loupedeck\Loupedeck2\PluginApi.dll'
4. Build the project and run the application. The plugin should now be available in the Loupedeck software.

## Contributing
Contributions are welcome! 
