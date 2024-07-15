
import numpy as np
import wave
import hl2ss
import hl2ss_uifm as uifm
import time

# HoloLens address
host = '192.168.1.7'

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
