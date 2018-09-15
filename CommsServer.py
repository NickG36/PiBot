import socket
import sys
from thread import *

class CommsServer:
	def __init__(self):
		print "......In CommsServer init"
		# Create a TCP/IP socket		
		self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

		print "......bind"
		# Bind the socket to the port
		server_address = ('localhost', 10000)

		print >> sys.stderr, 'starting up on %s port %s' % server_address
		#print >> sys.stderr, 'starting up on %s port 10000' % server_address


		try:
			self.sock.bind(server_address)
		except socket.error as msg:
			print 'Bind failed. Error Code : ' + str(msg[0]) + ' Message ' + msg[1]
			sys.exit()
			
		print 'Socket bind complete'

		# Listen for incoming connections
		self.sock.listen(1)
		print 'Listening'

		# Wait for a connection
		print >> sys.stderr, 'waiting for a message'
		self.conn, client_address = self.sock.accept()
		print 'Connected'

	def wait(self):
		data = self.conn.recv(1024)
		print 'Received: "%s"' % data

		if data == "Green request":
		  print 'Green request'
		  return True
                else:
		  print 'Black request'
                  return False

	def send(self, green_level):
		#reply = 'Green level 0.2'
		#print 'Sending "%s"' % reply
		reply = repr(green_level)
		print 'Sending "%s"' % reply
		self.conn.sendall(reply)

	def run(self):
		print("Server.run")

	def close(self):
		print 'Closing socket'
		self.sock.close()

		print 'Closed connection'
		self.conn.close()
 

def main():
	print("Running server")
	server = CommsServer()

	print("Waiting ==========")
	server.wait()

	print("Responding ==========")
	server.send()

	print("Waiting =========")
	server.wait()

	print("Responding ============")
	server.send()

	print("Waiting =========")
	server.wait()

	print("Responding ============")
	server.send()

if __name__ == "__main__":
    main()