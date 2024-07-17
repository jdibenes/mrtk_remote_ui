
import hl2ss
import hl2ss_lnm
import hl2ss_uifm

host = '192.168.1.7'

image = open('data/image.jpg', 'rb')
data_image = image.read()
image.close()

display_list = hl2ss_uifm.command_buffer()
display_list.file_exists('image_1.jpg')
display_list.file_upload('image_1.jpg', data_image)
display_list.file_exists('image_1.jpg')
display_list.file_move('image_1.jpg', 'image_2.jpg')
display_list.file_exists('image_2.jpg')
display_list.file_delete('image_2.jpg')
display_list.file_exists('image_2.jpg')
display_list.file_exists('image_1.jpg')

client = hl2ss_lnm.ipc_umq(host, hl2ss.IPCPort.UNITY_MESSAGE_QUEUE)
client.open()

client.push(display_list)
response = client.pull(display_list)

client.close()

print(f'Response: {response}')
