# MRTK Remote UI
Experimental Unity/MRTK framework for creating and controlling simple User Interfaces on HoloLens 2 remotely from Python code running on desktop.

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
- [demo_ui_videos_images_text.py](client/demo_ui_videos_images_text.py): Creates a window in the Unity scene which is used to show a video, an image, and text to the HoloLens user.

See [hl2ss_uifm.py](client/hl2ss_uifm.py) for details on the available functionality.

**Running the demos**

1. Install the uifm appxbundle provided in [Releases](https://github.com/jdibenes/mrtk_remote_ui/releases) or build the Unity project (2020.3.42f1) located in the [uifm](uifm) directory.
2. Run the uifm application on the HoloLens.
3. Set the host variable of the Python scripts to your HoloLens IP address.
4. Run the Python script.

**Required packages**

- [OpenCV](https://github.com/opencv/opencv-python) `pip install opencv-python`
- [PyAV](https://github.com/PyAV-Org/PyAV) `pip install av`
- [NumPy](https://numpy.org/) `pip install numpy`
- [pynput](https://github.com/moses-palmer/pynput) `pip install pynput`
