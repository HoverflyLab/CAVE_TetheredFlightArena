"""
A socket connector for sending dlc-live-gui data into Unity
"""
import socket

class unity_connector():

    def __init__(self):
        self.count = 0
        self.byte_message = bytes("", encoding='utf-8')
        self.opened_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.opened_socket.settimeout(0.050)

    def SendtoUnity(self):
        print("sending to unity")
        self.byte_message = self.byte_message[:-1] #removes last ":" as it causes string splitting issues in unity
        print(self.byte_message)
        self.opened_socket.sendto(self.byte_message, ("127.0.0.1", 8051))
        self.byte_message = bytes("", encoding='utf-8')

    def AddValuestoList(self,id, x, y):
        print("adding values")
        self.byte_message += bytes(str(id), encoding='utf-8')
        self.byte_message += bytes(str(","), encoding='utf-8')
        self.byte_message += bytes(str(x), encoding='utf-8')
        self.byte_message += bytes(str(","), encoding='utf-8')
        self.byte_message += bytes(str(y), encoding='utf-8')
        self.byte_message += bytes(str(":"), encoding='utf-8')

uc = unity_connector()
uc.AddValuestoList(0,333.1231535,2.532532)
uc.AddValuestoList(1,5,6)
uc.AddValuestoList(2,11,7)
uc.AddValuestoList(3,124.54,65.98)
uc.SendtoUnity()
