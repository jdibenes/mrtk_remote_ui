# MRTK Remote UI
Experimental framework for creating and controlling simple User Interfaces on HoloLens 2, with Unity and MRTK, remotely from Python code running on desktop.

**Supported controls**

- Panel (container for controls)
- 2D surfaces (for displaying images and videos)
- Text labels
- Buttons

**Supported functions**

- File upload
- Video playback
- Audio playback
- TTS
- Create 3D primitive (sphere, capsule, cylinder, cube, plane, and quad)

## Demos

Two demos showcasing the capabilities of the framework are provided.

- [demo_remote_audio_player.py](client/demo_remote_audio_player.py): Creates a window in the Unity scene that allows the HoloLens user to control the playback of a set of audio files.
- [demo_ui_videos_images_text.py](client/demo_ui_videos_images_text.py): Creates a window in the Unity scene which is used to show a video, an image, and text to the HoloLens user. The window contains two buttons that allow the HoloLens user to select one of multiple videos.

## Unity project



