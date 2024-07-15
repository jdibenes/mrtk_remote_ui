#------------------------------------------------------------------------------
# Remote UI Example
#------------------------------------------------------------------------------

from html2image import Html2Image

import hl2ss
import hl2ss_uifm as uifm

# Settings --------------------------------------------------------------------

# HoloLens address
host = '192.168.1.7'

#------------------------------------------------------------------------------

# UI elements
# All positions defined in local coordinates in meters

html_name = 'pbj.html'
video_names = ['video_1.webm', 'video_2.webm', 'video_3.webm']
image_name = 'image.jpg'#'pbj.png'#'image.jpg'
title = 'UI Sample - Video, Images, and Text'
text = '<color="red">Lorem</color> <i>ipsum</i> <b>dolor</b> <mark=#FFFF0044>sit amet</mark>, <s>consectetur</s> <u>adipiscing</u> <cspace=1em>elit</cspace>, <sup>sed</sup> <sub>do</sub> eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.'

spawn_position = [0.05, 0.05, 0.6]

panel_position = [0.07, -0.034, 0]
panel_scale = [0.2, 0.15, 0.01]
pin_position = [panel_position[0] + panel_scale[0] / 2 + 0.015, 0.028, -0.01]

surface_video_scale    = [0.09, 0.05, 1]
surface_video_position = [panel_position[0] - panel_scale[0] / 2 + surface_video_scale[0] / 2 + 0.005, panel_position[1] + panel_scale[1] / 2 - surface_video_scale[1] / 2 - 0.005, -0.01]
surface_video_rotation = [0, 0, 0, 1]

surface_image_scale    = [0.09, 0.05, 1]
surface_image_position = [panel_position[0] + panel_scale[0] / 2 - surface_video_scale[0] / 2 - 0.005, panel_position[1] + panel_scale[1] / 2 - surface_video_scale[1] / 2 - 0.005, -0.01]
surface_image_rotation = [0, 0, 0, 1]

text_body_dimensions = [ 0.2 - 0.01, 0.15 - 0.06 ]
text_body_position = [panel_position[0], panel_position[1] - (0.15-0.09) / 2, -0.01]

text_title_dimensions = [ 0.2, 0.02 ]
text_title_position = [panel_position[0], panel_position[1] + panel_scale[1] / 2 + 0.01, -0.01]

button1_position = [panel_position[0] - panel_scale[0] / 2 + 0.096 / 2, panel_position[1] - panel_scale[1] / 2 - 0.032 / 2 - 0.005, -0.01]
button2_position = [panel_position[0] + panel_scale[0] / 2 - 0.096 / 2, panel_position[1] - panel_scale[1] / 2 - 0.032 / 2 - 0.005, -0.01]

# Load sample video and image

hti = Html2Image(output_path='.', custom_flags=['--default-background-color=FFFFFF'])
hti.screenshot(html_file='pbj.html', save_as='pbj.png')

ui_panel = 'sample_panel'
ui_video = 'surface_video'
ui_image = 'surface_image'
ui_text1 = 'text_body'
ui_text2 = 'text_title'
ui_button1 = 'button_next_video'
ui_button2 = 'button_prev_video'

ipc = hl2ss.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)
ipc.open()

key = 0
video_index = 0

display_list = uifm.command_buffer()

# Begin display list

image = open(image_name, 'rb')
data_image = image.read()
image.close()

display_list.file_upload(image_name, data_image)

for video_name in video_names:
    video = open(video_name, 'rb')
    data_video = video.read()
    video.close()
    display_list.file_upload(video_name, data_video)


display_list.panel_destroy(ui_panel)
display_list.panel_create(ui_panel, spawn_position)
display_list.panel_set_transform(ui_panel, pin_position, panel_position, panel_scale)
display_list.panel_set_active(ui_panel, uifm.ActiveState.Active)
#display_list.panel_exists

display_list.surface_create(ui_panel, ui_video)
display_list.surface_set_transform(ui_panel, ui_video, surface_video_position, surface_video_rotation, surface_video_scale)
display_list.surface_set_active(ui_panel, ui_video, uifm.ActiveState.Active)
display_list.surface_set_video(ui_panel, ui_video, video_names[video_index], True)
#display_list.surface_destroy
#display_list.surface_exists

display_list.surface_create(ui_panel, ui_image)
display_list.surface_set_transform(ui_panel, ui_image, surface_image_position, surface_image_rotation, surface_image_scale)
display_list.surface_set_active(ui_panel, ui_image, uifm.ActiveState.Active)
display_list.surface_set_texture(ui_panel, ui_image, image_name)

display_list.text_create(ui_panel, ui_text1)
display_list.text_set_transform(ui_panel, ui_text1, text_body_position, text_body_dimensions)
display_list.text_set_format(ui_panel, ui_text1, uifm.FontStyles.Normal, 0.08, False, [1,1,1,1], uifm.HorizontalAlignmentOptions.Left, uifm.VerticalAlignmentOptions.Top, True, uifm.TextOverflowModes.Overflow)
display_list.text_set_text(ui_panel, ui_text1, text)
display_list.text_set_active(ui_panel, ui_text1, uifm.ActiveState.Active)
#display_list.text_destroy
#display_list.text_exists

display_list.text_create(ui_panel, ui_text2)
display_list.text_set_transform(ui_panel, ui_text2, text_title_position, text_title_dimensions)
display_list.text_set_format(ui_panel, ui_text2, uifm.FontStyles.Bold, 0.1, False, [1,1,1,1], uifm.HorizontalAlignmentOptions.Center, uifm.VerticalAlignmentOptions.Middle, True, uifm.TextOverflowModes.Overflow)
display_list.text_set_text(ui_panel, ui_text2, title)
display_list.text_set_active(ui_panel, ui_text2, uifm.ActiveState.Active)
#display_list.text_destroy
#display_list.text_exists

display_list.button_create(ui_panel, ui_button1, 0)
display_list.button_set_transform(ui_panel, ui_button1, button1_position, [0,0,0,1], [1,1,1])
display_list.button_set_text(ui_panel, ui_button1, 'Next Video')
display_list.button_set_active(ui_panel, ui_button1, uifm.ActiveState.Active)
#display_list.button_exists
#display_list.button_destroy

display_list.button_create(ui_panel, ui_button2, 1)
display_list.button_set_transform(ui_panel, ui_button2, button2_position, [0,0,0,1], [1,1,1])
display_list.button_set_text(ui_panel, ui_button2, 'Previous Video')
display_list.button_set_active(ui_panel, ui_button2, uifm.ActiveState.Active)

# End display list

ipc.push(display_list) # Send command to server
results = ipc.pull(display_list) # Get result from server
print(f'Response: {results}')

while (True):
    display_list = uifm.command_buffer()
    display_list.button_get_state(ui_panel)
    ipc.push(display_list)
    results = ipc.pull(display_list)[0]
    if (results != 0):
        video_index += (1 if ((results & 1) != 0) else 0) + (-1 if ((results & 2) != 0) else 0)
        video_index %= len(video_names)
        display_list = uifm.command_buffer()
        display_list.surface_set_video(ui_panel, ui_video, video_names[video_index], True)
        ipc.push(display_list)
        ipc.pull(display_list)
        print(f'Button pressed {results}')

ipc.close()
