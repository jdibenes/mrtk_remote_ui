#------------------------------------------------------------------------------
# Remote UI with Videos, Images, and Text demo
# This script creates a window in the Unity scene which is used to show a
# video, an image, and text to the HoloLens user.
#
# How to use this script:
# 1. Run the Unity application (uifm) on the HoloLens.
# 2. In this script, set the host variable to your HoloLens IP address.
# 3. Run this script.
#
# This script works as follows:
# 1. Upload all video and image files to the HoloLens.
# 2. Create a panel with 2 surfaces (for image and video), 2 text labels, and
#    2 buttons.
#    The hololens user can grab the panel to move it around or to scale it.
# 3. The HoloLens user clicks the buttons to select a video. This
#    script reads the status of the buttons and performs the corresponding
#    actions.
# 4. Press Esc to destroy the panel and stop this script.
#------------------------------------------------------------------------------

#------------------------------------------------------------------------------

from pynput import keyboard

import os
import hl2ss
import hl2ss_lnm
import hl2ss_uifm

# Settings --------------------------------------------------------------------

# HoloLens address
host = '192.168.1.7'

# Folder containing the media files
data_folder = './data_demo/graphics'

# Image and video file names
image_names = ['image.jpg']
video_names = ['Grouse.mp4', 'Bear.mp4', 'Wolves.mp4']

# Panel title
title = 'UI Sample - Video, Images, and Text'

# Panel text
text = '<color="red">Lorem</color> <i>ipsum</i> <b>dolor</b> <mark=#FFFF0044>sit amet</mark>, <s>consectetur</s> <u>adipiscing</u> <cspace=1em>elit</cspace>, <sup>sed</sup> <sub>do</sub> eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.'

#------------------------------------------------------------------------------

# Connect to HoloLens ---------------------------------------------------------

client_outbound = hl2ss_lnm.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)
client_outbound.open()

image_index = 0
video_index = 0

# Upload files to HoloLens ----------------------------------------------------

file_names = image_names + video_names

for file_name in file_names:
    print(f'Uploading file {file_name} to HoloLens...')

    with open(os.path.join(data_folder, file_name), 'rb') as image_file:
        buffer = hl2ss_uifm.command_buffer()
        buffer.file_upload(file_name, image_file.read())

        client_outbound.push(buffer)
        response = client_outbound.pull(buffer)

        print(f'File upload response: {response[0]}')

# UI Layout -------------------------------------------------------------------

spawn_position = [0, -0.1, 1.0]
panel_position = [0, 0, 0]
panel_scale = [0.2, 0.15, 0.01]
pin_position = [panel_position[0] + (panel_scale[0] / 2) + (0.8 * 0.032 / 2) + 0.005, panel_position[1] + (panel_scale[1] / 2) - (0.8 * 0.032 / 2), -0.016 / 2]

surface_video_scale = [0.09, 0.05, 1]
surface_video_position = [panel_position[0] - (panel_scale[0] / 2) + (surface_video_scale[0] / 2) + 0.005, panel_position[1] + (panel_scale[1] / 2) - (surface_video_scale[1] / 2) - 0.005, -0.016 / 2]

surface_image_scale = [0.09, 0.05, 1]
surface_image_position = [panel_position[0] + (panel_scale[0] / 2) - (surface_video_scale[0] / 2) - 0.005, panel_position[1] + (panel_scale[1] / 2) - (surface_video_scale[1] / 2) - 0.005, -0.016 / 2]

text_body_position = [panel_position[0], panel_position[1] - (0.15-0.09) / 2, -0.016 / 2]
text_body_dimensions = [ 0.2 - 0.01, 0.15 - 0.06 ]

text_title_position = [panel_position[0], panel_position[1] + (panel_scale[1] / 2) + 0.01, -0.016 / 2]
text_title_dimensions = [ 0.2, 0.02 ]

button_prev_position = [panel_position[0] - (panel_scale[0] / 2) + (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) - (0.032 / 2) - 0.005, -0.016 / 2]
button_next_position = [panel_position[0] + (panel_scale[0] / 2) - (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) - (0.032 / 2) - 0.005, -0.016 / 2]

ui_panel = 'sample_panel'

ui_surface_video = 'surface_video'
ui_surface_image = 'surface_image'

ui_text_body = 'text_body'
ui_text_title = 'text_title'

ui_button_next = 'button_next_video'
ui_button_prev = 'button_prev_video'

# Create main window ----------------------------------------------------------

buffer = hl2ss_uifm.command_buffer()

buffer.panel_destroy(ui_panel)
buffer.panel_create(ui_panel, spawn_position)
buffer.panel_set_transform(ui_panel, pin_position, panel_position, panel_scale)

buffer.surface_create(ui_panel, ui_surface_video)
buffer.surface_set_transform(ui_panel, ui_surface_video, surface_video_position, [0,0,0,1], surface_video_scale)
buffer.surface_set_active(ui_panel, ui_surface_video, True)

buffer.surface_create(ui_panel, ui_surface_image)
buffer.surface_set_transform(ui_panel, ui_surface_image, surface_image_position, [0,0,0,1], surface_image_scale)
buffer.surface_set_active(ui_panel, ui_surface_image, True)
buffer.surface_set_texture_file(ui_panel, ui_surface_image, image_names[image_index])

buffer.text_create(ui_panel, ui_text_body)
buffer.text_set_transform(ui_panel, ui_text_body, text_body_position, text_body_dimensions)
buffer.text_set_format(ui_panel, ui_text_body, hl2ss_uifm.TextFontStyle.Normal, 0.08, False, [1,1,1,1], hl2ss_uifm.TextHorizontalAlignment.Left, hl2ss_uifm.TextVerticalAlignment.Top, True, hl2ss_uifm.TextOverflowMode.Overflow)
buffer.text_set_text(ui_panel, ui_text_body, text)
buffer.text_set_active(ui_panel, ui_text_body, True)

buffer.text_create(ui_panel, ui_text_title)
buffer.text_set_transform(ui_panel, ui_text_title, text_title_position, text_title_dimensions)
buffer.text_set_format(ui_panel, ui_text_title, hl2ss_uifm.TextFontStyle.Bold, 0.1, False, [1,1,1,1], hl2ss_uifm.TextHorizontalAlignment.Center, hl2ss_uifm.TextVerticalAlignment.Middle, True, hl2ss_uifm.TextOverflowMode.Overflow)
buffer.text_set_text(ui_panel, ui_text_title, title)
buffer.text_set_active(ui_panel, ui_text_title, True)

buffer.button_create(ui_panel, ui_button_prev)
buffer.button_set_transform(ui_panel, ui_button_prev, button_prev_position, [0,0,0,1], [1,1,1])
buffer.button_set_text(ui_panel, ui_button_prev, 'Previous Video')
buffer.button_set_active(ui_panel, ui_button_prev, True)

buffer.button_create(ui_panel, ui_button_next)
buffer.button_set_transform(ui_panel, ui_button_next, button_next_position, [0,0,0,1], [1,1,1])
buffer.button_set_text(ui_panel, ui_button_next, 'Next Video')
buffer.button_set_active(ui_panel, ui_button_next, True)

buffer.panel_set_active(ui_panel, True)

buffer.surface_set_video_file(ui_panel, ui_surface_video, video_names[video_index])
buffer.surface_video_configure(ui_panel, ui_surface_video, hl2ss_uifm.VideoSetting.LOOP, 1)
buffer.surface_video_control(ui_panel, ui_surface_video, hl2ss_uifm.VideoOperation.PLAY)

client_outbound.push(buffer)
response = client_outbound.pull(buffer)

print(f'Create UI response: {response}')

# Main loop -------------------------------------------------------------------

enable = True

def on_press(key):
    global enable
    enable = key != keyboard.Key.esc
    return enable

listener = keyboard.Listener(on_press=on_press)
listener.start()

while (enable):
    display_list = hl2ss_uifm.command_buffer()
    display_list.button_get_state(ui_panel, ui_button_next)
    display_list.button_get_state(ui_panel, ui_button_prev)

    client_outbound.push(display_list)
    response = client_outbound.pull(display_list)

    delta = (1 if (response[0] != 0) else 0) + (-1 if (response[1] != 0) else 0)

    if (delta != 0):
        video_index = (video_index + delta) % len(video_names)

        display_list = hl2ss_uifm.command_buffer()
        display_list.surface_set_video_file(ui_panel, ui_surface_video, video_names[video_index])
        display_list.surface_video_configure(ui_panel, ui_surface_video, hl2ss_uifm.VideoSetting.LOOP, 1)
        display_list.surface_video_control(ui_panel, ui_surface_video, hl2ss_uifm.VideoOperation.PLAY)

        client_outbound.push(display_list)
        response = client_outbound.pull(display_list)

        print(f'Set surface video response: {response}')

# Cleanup --------------------------------------------------------------------- 

buffer = hl2ss_uifm.command_buffer()
buffer.panel_destroy(ui_panel)

client_outbound.push(buffer)
response = client_outbound.pull(buffer)

print(f'Destroy UI response: {response}')

client_outbound.close()

listener.join()
