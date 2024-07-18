
import numpy as np
import wave
import hl2ss
import hl2ss_lnm
import hl2ss_uifm
import time

# HoloLens address
host = '192.168.1.7'


audio_file_1 = open('./data/the_lost_forest.mp3', 'rb')
mp3_data = audio_file_1.read()
audio_file_1.close()
audio_file_2 = open('./data/soul.wav', 'rb')
wav_data = audio_file_2.read()
audio_file_2.close()


client = hl2ss.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)
client.open()

wfile = wave.open('./data/fanfare2.wav', mode='rb')
samples = wfile.getnframes()
audio = wfile.readframes(samples)
audio_s16 = np.frombuffer(audio, dtype=np.int16)
audio_f32 = audio_s16.astype(np.float32) / 32768.0




display_list = hl2ss_uifm.command_buffer()
display_list.audio_play_data('fanfare2', wfile.getnchannels(), wfile.getframerate(), audio_f32.tobytes())
display_list.file_upload('the_lost_forest.mp3', mp3_data)
display_list.file_upload('crying_soul.wav', wav_data)
display_list.audio_configure(hl2ss_uifm.AudioSetting.PAN_STEREO, 0.0)
display_list.audio_configure(hl2ss_uifm.AudioSetting.PITCH, 1.0)
display_list.audio_configure(hl2ss_uifm.AudioSetting.VOLUME, 1.0)
display_list.audio_play_file('the_lost_forest.mp3', hl2ss_uifm.AudioType.MPEG)
#display_list.audio_control(hl2ss_uifm.AudioOperation.STOP)

client.push(display_list)
response = client.pull(display_list)

print(f'Response: {response}')

while (True):
    display_list = hl2ss_uifm.command_buffer()
    display_list.audio_control(hl2ss_uifm.AudioOperation.IS_PLAYING)
    client.push(display_list)
    response = client.pull(display_list)
    if (response == 0):
        break

display_list = hl2ss_uifm.command_buffer()
display_list.audio_play_file('crying_soul.wav', hl2ss_uifm.AudioType.WAV)

client.push(display_list)
response = client.pull(display_list)

client.close()




'''

ipc = hl2ss.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)
ipc.open()

display_list = uifm.command_buffer()

wfile = wave.open('fanfare2.wav', mode='rb')
samples = wfile.getnframes()
audio = wfile.readframes(samples)
audio_s16 = np.frombuffer(audio, dtype=np.int16)
audio_f32 = audio_s16.astype(np.float32) / 32768.0

display_list.play_wav('test', True, wfile.getnchannels(), wfile.getframerate(), audio_f32.tobytes())
display_list.is_wav_playing()

ipc.push(display_list) # Send commands to server
results = ipc.pull(display_list) # Get results from server
print(f'Response: {results}')

time.sleep(2)

display_list = uifm.command_buffer()
display_list.stop_wav()

ipc.push(display_list) # Send commands to server
results = ipc.pull(display_list) # Get results from server
print(f'Response: {results}')

# Disconnect ------------------------------------------------------------------

ipc.close()
'''