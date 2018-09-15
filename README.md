# PiBot
Code to control a robot driven by a raspberry pi round an arena.

The robot uses infra-red sensors and a Rasperry Pi camera.

The arena has black walls and contains a number of white obstacles.
The aim is to control the bot around an arena, avoiding the walls and 
obstacles with the aim of trying to find the target (a green ball) whilst
also avoiding the decoy targets (blue balls).

The code to control the motors and to process the IR sensor feedback and 
image details is made in C#.
A python process is used to process an image when asked by the C# process.
The python will find the biggest green or white polygon (as requested) and will
send the size of the polygon and the x co-ordinate of its centre to the C#.

The two processes communicate via Unix sockets.
