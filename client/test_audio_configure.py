
import hl2ss
import hl2ss_lnm
import hl2ss_uifm

host = '192.168.1.7'

client = hl2ss_lnm.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)
client.open()

display_list = hl2ss_uifm.command_buffer()
display_list.audio_configure(hl2ss_uifm.AudioSetting.VOLUME, 1.0)

client.push(display_list)
response = client.pull(display_list)

print(response)

client.close()
