
from pynput import keyboard

import hl2ss
import hl2ss_lnm
import hl2ss_uifm
import time
import os

# Settings --------------------------------------------------------------------

# HoloLens address
host = '192.168.1.7'

# Folder containing the audio files
data_folder = './data'

# Audio file names
# Pure file names only
# Type is not autodetected currently, please specify audio type
audio_file_descriptors = [
    ('the_lost_forest.mp3', hl2ss_uifm.AudioType.MPEG),
    ('soul.wav',            hl2ss_uifm.AudioType.WAV)
]

#------------------------------------------------------------------------------

enable = True

def on_press(key):
    global enable
    enable = key != keyboard.Key.esc
    return enable

listener = keyboard.Listener(on_press=on_press)
listener.start()

client_outbound = hl2ss_lnm.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)
client_outbound.open()

for audio_file_descriptor in audio_file_descriptors:
    file_name = audio_file_descriptor[0]
    audio_type = audio_file_descriptor[1]

    print(f'Uploading file {file_name} to HoloLens...')

    with open(os.path.join(data_folder, file_name), 'rb') as audio_file:
        cmdbuf = hl2ss_uifm.command_buffer()
        cmdbuf.file_upload(file_name, audio_file.read())

        client_outbound.push(cmdbuf)
        response = client_outbound.pull(cmdbuf)

        print(f'file_upload response: {response[0]}')

audio_index = 0

while (enable):
    print('Playing next audio file...')

    audio_file_descriptor = audio_file_descriptors[audio_index]

    file_name = audio_file_descriptor[0]
    audio_type = audio_file_descriptor[1]

    cmdbuf = hl2ss_uifm.command_buffer()
    cmdbuf.audio_play_file(file_name, audio_type)
    
    client_outbound.push(cmdbuf)
    response = client_outbound.pull(cmdbuf)

    print(f'audio_play_file response: {response}')

    while (enable):
        time.sleep(1)

        cmdbuf = hl2ss_uifm.command_buffer()
        cmdbuf.audio_control(hl2ss_uifm.AudioOperation.IS_PLAYING)

        client_outbound.push(cmdbuf)
        response = client_outbound.pull(cmdbuf)

        if (response[0] == 0):
            print('Audio file playback complete')

            audio_index = (audio_index + 1) % len(audio_file_descriptors)
            break

cmdbuf = hl2ss_uifm.command_buffer()
cmdbuf.audio_control(hl2ss_uifm.AudioOperation.STOP)

client_outbound.push(cmdbuf)
client_outbound.pull(cmdbuf)

print('Audio stopped')

client_outbound.close()
