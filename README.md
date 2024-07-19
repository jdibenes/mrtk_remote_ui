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

1. Install the uifm appxbundle provided in [Releases](https://github.com/jdibenes/mrtk_remote_ui/releases) or build the Unity project (2020.3.42f1) located in the [uifm](uifm) directory. See below for more information.
2. Run the uifm application on the HoloLens.
3. Set the host variable of the Python scripts to your HoloLens IP address.
4. Run the Python script.

**Required packages**

- [OpenCV](https://github.com/opencv/opencv-python) `pip install opencv-python`
- [PyAV](https://github.com/PyAV-Org/PyAV) `pip install av`
- [NumPy](https://numpy.org/) `pip install numpy`
- [pynput](https://github.com/moses-palmer/pynput) `pip install pynput`

## Installation (sideloading)

The application is distributed as a single appxbundle file and can be installed using one of the two following methods.

**Method 1 (local)**

1. On your HoloLens, open Microsoft Edge and navigate to this repository.
2. Download the [latest appxbundle](https://github.com/jdibenes/mrtk_remote_ui/releases).
3. Open the appxbundle and tap Install.

**Method 2 (remote)**

1. Download the [latest appxbundle](https://github.com/jdibenes/mrtk_remote_ui/releases).
2. Go to the Device Portal and navigate to Views -> Apps. Under Deploy apps, select Local Storage, click Browse, and select the appxbundle.
3. Click Install, wait for the installation to complete, then click Done.

You can find the server application (uifm) in the All apps list.

## Unity project

A sample Unity project (2020.3.42f1) can be found in the [uifm](uifm) directory.

**Build and run the sample project**

1. Open the project in Unity. If the MRTK Project Configurator window pops up just close it.
2. Go to Build Settings (File -> Build Settings).
3. Switch to Universal Windows Platform.
4. Set Target Device to HoloLens.
5. Set Architecture to ARM64.
6. Set Build and Run on Remote Device (via Device Portal).
7. Set Device Portal Address to your HoloLens IP address (e.g., https://192.168.1.7) and set your Device Portal Username and Password.
8. Click Build and Run. Unity may ask for a Build folder. You can create a new one named Build.
