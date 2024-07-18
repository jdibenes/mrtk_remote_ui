
from pynput import keyboard

import os
import time
import hl2ss
import hl2ss_lnm
import hl2ss_uifm

# Settings --------------------------------------------------------------------

# HoloLens address
host = '192.168.1.7'

# Folder containing the audio files
data_folder = './data'

# Audio file names
# Pure file names only
# Type is not autodetected currently, please specify audio type
playlist = [
    ('the_lost_forest.mp3', hl2ss_uifm.AudioType.MPEG),
    ('soul.wav',            hl2ss_uifm.AudioType.WAV)
]

#------------------------------------------------------------------------------

class remote_player:
    def __init__(self, host, audio_file_descriptors):
        self.client_outbound = hl2ss_lnm.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)        
        self.audio_file_descriptors = audio_file_descriptors
        self.audio_index = 0
        self.playing = False
        self.paused = False
        self.muted = False

    def open(self):
        self.client_outbound.open()

    def upload_audio_files(self):
        for audio_file_descriptor in self.audio_file_descriptors:
            file_name = audio_file_descriptor[0]
            print(f'Uploading file {file_name} to HoloLens...')

            with open(os.path.join(data_folder, file_name), 'rb') as audio_file:
                buffer = hl2ss_uifm.command_buffer()
                buffer.file_upload(file_name, audio_file.read())

                self.client_outbound.push(buffer)
                response = self.client_outbound.pull(buffer)

                print(f'File upload response: {response[0]}')

    def create_window(self):
        spawn_position = [0, -0.1, 1.0]
        panel_position = [0, 0, 0]
        panel_scale = [0.096*2, 0.08, 0.01]
        pin_position = [panel_position[0] + (panel_scale[0] / 2) + (0.8 * 0.032 / 2), panel_position[1] + (panel_scale[1] / 2) - (0.8 * 0.032 / 2), -0.016 / 2]

        button_play_position = [panel_position[0] - (panel_scale[0] / 2) + (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) -     (0.032 / 2), -0.016 / 2]
        button_stop_position = [panel_position[0] + (panel_scale[0] / 2) - (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) -     (0.032 / 2), -0.016 / 2]
        button_prev_position = [panel_position[0] - (panel_scale[0] / 2) + (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) - 3 * (0.032 / 2), -0.016 / 2]
        button_next_position = [panel_position[0] + (panel_scale[0] / 2) - (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) - 3 * (0.032 / 2), -0.016 / 2]
        button_mute_position = [panel_position[0] - (panel_scale[0] / 2) + (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) - 5 * (0.032 / 2), -0.016 / 2]
        button_quit_position = [panel_position[0] + (panel_scale[0] / 2) - (0.096 / 2), panel_position[1] - (panel_scale[1] / 2) - 5 * (0.032 / 2), -0.016 / 2]

        text_title_position = [panel_position[0], panel_position[1] + (panel_scale[1] / 2) + 0.016, -0.016 / 2]
        text_title_dimensions = [0.01, 0.01]

        text_status_position = [panel_position[0], panel_position[1], -0.016 / 2]
        text_status_dimensions = [panel_scale[0] * 0.95, panel_scale[1] * 0.95]

        ui_panel = 'uifm_player_demo_panel'

        ui_button_play = 'uifm_button_play'
        ui_button_stop = 'uifm_button_stop'        
        ui_button_prev = 'uifm_button_prev'
        ui_button_next = 'uifm_button_next'
        ui_button_mute = 'uifm_button_mute'
        ui_button_quit = 'uifm_button_quit'

        ui_text_title = 'uifm_text_title'
        ui_text_status = 'uifm_text_status'

        self.ui_panel = ui_panel

        self.ui_button_play = ui_button_play
        self.ui_button_stop = ui_button_stop
        self.ui_button_prev = ui_button_prev
        self.ui_button_next = ui_button_next
        self.ui_button_mute = ui_button_mute
        self.ui_button_quit = ui_button_quit

        self.ui_text_title = ui_text_title
        self.ui_text_status = ui_text_status

        buffer = hl2ss_uifm.command_buffer()

        buffer.panel_destroy(ui_panel)
        buffer.panel_create(ui_panel, spawn_position)
        buffer.panel_set_transform(ui_panel, pin_position, panel_position, panel_scale)

        buffer.button_create(ui_panel, ui_button_play)
        buffer.button_set_transform(ui_panel, ui_button_play, button_play_position, [0,0,0,1], [1,1,1])
        buffer.button_set_text(ui_panel, ui_button_play, "Play")
        buffer.button_set_active(ui_panel, ui_button_play, True)

        buffer.button_create(ui_panel, ui_button_stop)
        buffer.button_set_transform(ui_panel, ui_button_stop, button_stop_position, [0,0,0,1], [1,1,1])
        buffer.button_set_text(ui_panel, ui_button_stop, "Stop")
        buffer.button_set_active(ui_panel, ui_button_stop, True)

        buffer.button_create(ui_panel, ui_button_prev)
        buffer.button_set_transform(ui_panel, ui_button_prev, button_prev_position, [0,0,0,1], [1,1,1])
        buffer.button_set_text(ui_panel, ui_button_prev, "Previous")
        buffer.button_set_active(ui_panel, ui_button_prev, True)

        buffer.button_create(ui_panel, ui_button_next)
        buffer.button_set_transform(ui_panel, ui_button_next, button_next_position, [0,0,0,1], [1,1,1])
        buffer.button_set_text(ui_panel, ui_button_next, "Next")
        buffer.button_set_active(ui_panel, ui_button_next, True)

        buffer.button_create(ui_panel, ui_button_mute)
        buffer.button_set_transform(ui_panel, ui_button_mute, button_mute_position, [0,0,0,1], [1,1,1])
        buffer.button_set_text(ui_panel, ui_button_mute, "Mute")
        buffer.button_set_active(ui_panel, ui_button_mute, True)

        buffer.button_create(ui_panel, ui_button_quit)
        buffer.button_set_transform(ui_panel, ui_button_quit, button_quit_position, [0,0,0,1], [1,1,1])
        buffer.button_set_text(ui_panel, ui_button_quit, "Quit")
        buffer.button_set_active(ui_panel, ui_button_quit, True)

        buffer.text_create(ui_panel, ui_text_title)
        buffer.text_set_transform(ui_panel, ui_text_title, text_title_position, text_title_dimensions)
        buffer.text_set_format(ui_panel, ui_text_title, hl2ss_uifm.TextFontStyle.Normal, 0.1, False, [1,1,1,1], hl2ss_uifm.TextHorizontalAlignment.Center, hl2ss_uifm.TextVerticalAlignment.Middle, False, hl2ss_uifm.TextOverflowMode.Overflow)
        buffer.text_set_text(ui_panel, ui_text_title, 'Remote Audio Player')
        buffer.text_set_active(ui_panel, ui_text_title, True)

        buffer.text_create(ui_panel, ui_text_status)
        buffer.text_set_transform(ui_panel, ui_text_status, text_status_position, text_status_dimensions)
        buffer.text_set_format(ui_panel, ui_text_status, hl2ss_uifm.TextFontStyle.Normal, 0.08, False, [1,1,1,1], hl2ss_uifm.TextHorizontalAlignment.Left, hl2ss_uifm.TextVerticalAlignment.Top, True, hl2ss_uifm.TextOverflowMode.Overflow)
        buffer.text_set_text(ui_panel, ui_text_status, '[Waiting for client]')
        buffer.text_set_active(ui_panel, ui_text_status, True)

        buffer.panel_set_active(ui_panel, True)

        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        print(f'Create UI response: {response}')

    def query_status(self):
        buffer = hl2ss_uifm.command_buffer()
        buffer.button_get_state(self.ui_panel, self.ui_button_play) # 0
        buffer.button_get_state(self.ui_panel, self.ui_button_stop) # 1
        buffer.button_get_state(self.ui_panel, self.ui_button_prev) # 2
        buffer.button_get_state(self.ui_panel, self.ui_button_next) # 3
        buffer.button_get_state(self.ui_panel, self.ui_button_mute) # 4
        buffer.button_get_state(self.ui_panel, self.ui_button_quit) # 5
        buffer.audio_control(hl2ss_uifm.AudioOperation.IS_PLAYING)  # 6

        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        status = {'play' : response[0], 'stop' : response[1], 'prev' : response[2], 'next' : response[3], 'mute' : response[4], 'quit' : response[5], 'is_playing' : response[6]}

        return status
    
    def play(self):
        audio_file_descriptor = self.audio_file_descriptors[self.audio_index]

        file_name = audio_file_descriptor[0]
        audio_type = audio_file_descriptor[1]

        buffer = hl2ss_uifm.command_buffer()
        buffer.audio_control(hl2ss_uifm.AudioOperation.STOP)
        buffer.audio_play_file(file_name, audio_type)
        buffer.button_set_text(self.ui_panel, self.ui_button_play, 'Pause')
        buffer.text_set_text(self.ui_panel, self.ui_text_status, f'Playing {file_name}')

        self.playing = True
        self.paused = False

        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        print(f'Play response: {response}')

    def pause(self):
        buffer = hl2ss_uifm.command_buffer()
        buffer.audio_control(hl2ss_uifm.AudioOperation.PAUSE)
        buffer.button_set_text(self.ui_panel, self.ui_button_play, 'Play')

        self.playing = False
        self.paused = True

        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        print(f'Pause response: {response}')

    def unpause(self):
        buffer = hl2ss_uifm.command_buffer()
        buffer.audio_control(hl2ss_uifm.AudioOperation.UNPAUSE)
        buffer.button_set_text(self.ui_panel, self.ui_button_play, 'Pause')

        self.playing = True
        self.paused = False
        
        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        print(f'Unpause response: {response}')

    def toggle_play(self):
        if (not self.playing):
            if (not self.paused):
                self.play()
            else:
                self.unpause()
        else:
            if (not self.paused):
                self.pause()
            else:
                pass # ???
    
    def stop(self):
        buffer = hl2ss_uifm.command_buffer()
        buffer.audio_control(hl2ss_uifm.AudioOperation.STOP)
        buffer.button_set_text(self.ui_panel, self.ui_button_play, 'Play')
        buffer.text_set_text(self.ui_panel, self.ui_text_status, "Stopped")

        self.playing = False
        self.paused = False

        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        print(f'Stop response: {response}')

    def prev(self):
        self.audio_index = (self.audio_index - 1) % len(self.audio_file_descriptors)
        self.play()

    def next(self):
        self.audio_index = (self.audio_index + 1) % len(self.audio_file_descriptors)
        self.play()

    def toggle_mute(self):
        buffer = hl2ss_uifm.command_buffer()
        if (not self.muted):
            buffer.audio_control(hl2ss_uifm.AudioOperation.MUTE)
            buffer.button_set_text(self.ui_panel, self.ui_button_mute, 'Unmute')
        else:
            buffer.audio_control(hl2ss_uifm.AudioOperation.UNMUTE)
            buffer.button_set_text(self.ui_panel, self.ui_button_mute, 'Mute')

        self.muted = not self.muted

        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        print(f'Toggle mute response: {response}')

    def destroy_window(self):
        buffer = hl2ss_uifm.command_buffer()
        buffer.audio_control(hl2ss_uifm.AudioOperation.STOP)
        buffer.panel_destroy(self.ui_panel)

        self.client_outbound.push(buffer)
        response = self.client_outbound.pull(buffer)

        print(f'Destroy UI response: {response}')

    def close(self):
        self.client_outbound.close()


enable = True

def on_press(key):
    global enable
    enable = key != keyboard.Key.esc
    return enable

listener = keyboard.Listener(on_press=on_press)
listener.start()

player = remote_player(host, playlist)
player.open()
player.upload_audio_files()
player.create_window()

while (enable):
    status = player.query_status()
    if (status['quit'] != 0):
        break
    elif (player.playing and (status['is_playing'] == 0)):
        player.stop()
    elif (status['play'] != 0):
        player.toggle_play()
    elif (status['stop'] != 0):
        player.stop()
    elif (status['prev'] != 0):
        player.prev()
    elif (status['next'] != 0):
        player.next()
    elif (status['mute'] != 0):
        player.toggle_mute()

player.destroy_window()
player.close()

print('Press ESC to continue...')
listener.join()
