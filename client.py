
import socket
import sys # for exit
import time

class Client:
	def __init__(self):
		try:
			# Create a TCP socket
			self.s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
		except socket.error, msg:
			print 'Failed to create socket. Error code: ' + str(msg[0]) + ' , Error message : ' + msg[1]
			sys.exit()

		print 'Socket created'

		host = 'localhost'
		port = 10000

		try:
			remote_ip = socket.gethostbyname(host)
		except socket.gaierror:
			print 'Hostname could not be resolved. Exiting'
			sys.exit()
			
		print 'Ip address of ' + host + ' is ' + remote_ip

		# Connect to remote server
		self.s.connect((remote_ip, port))

		print 'Socket connected to ' + host + ' on ip ' + remote_ip

	def requestValue(self):
		message = "Hello there"

		try:
			self.s.sendall(message)
			print 'Sent: %s' % message
		except socket.error:
			print 'Send failed'
			sys.exit()
		print 'Msg sent successfully'

		reply = self.s.recv(4096)
		print 'Rx: %s' % reply

def main():
	print("Running client")
	client = Client()
	client.requestValue()
	client.requestValue()
	time.sleep(5)
	client.requestValue()
	client.requestValue()
	time.sleep(3)
	client.requestValue()
	client.requestValue()
	time.sleep(1)
	client.requestValue()
	client.requestValue()
	time.sleep(1)
	client.requestValue()
	client.requestValue()
	
	while True:
		time.sleep(1)
		client.requestValue()
		client.requestValue()

	time.sleep(1)
	client.requestValue()
	client.requestValue()
	time.sleep(50)

if __name__ == "__main__":
    main()