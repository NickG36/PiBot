using System;
using System.Net.Sockets;
using System.Threading;

public class Camera
{
	TcpClient client_socket = new TcpClient();

    // 
    // Thresholds to determine which state to go into dependent on how many green pixels
    // are reported by the camera:
	public static float NEAR_TARGET = 25000; // Above this robot will drive forwards into target
	public static float FOUND_TARGET = 150;  // Above this robot will stop turning to look for target
	public static float NOISE_THRESHOLD = 40; // Below this robot will use a rule to decide which dirn to look

    //
    // Thresholds to determine whether we are near a white obstacle or a black wall
    // dependent on how many white pixels are seen by the camera
    //
    public static int WHITE_STOP_THRESHOLD = 11000; // Below this amount of white we stop as we are near a wall
	public static int WHITE_WALL_THRESHOLD = 22000; // Below this amount of white we decide we are near a wall not an obstacle

	public void SetUp()
	{
        Console.WriteLine($"Trying to connect to python process");
        bool am_connected = false;

        while(!am_connected)
        {
		    try
		    {
		        client_socket.Connect ("127.0.0.1", 10000);
                am_connected = true;
            }
            catch(System.Net.Sockets.SocketException)
            {
		        Console.WriteLine($"Can't connect. Retrying..");
		        Thread.Sleep(500);
            }
		    Console.WriteLine($"Have connected");
        }
	}

    public void Settle()
	{
        // The first few reports from the camera are unreliable so request a few 
        // before we trust them
		requestGreenAmount();
		requestGreenAmount();
		requestGreenAmount();
		requestGreenAmount();
		requestGreenAmount();
	}

    // Details as reported by the camera
    public struct SceneDetails
    {
        public int area;  // The size of the biggest green region (in pixels squared)
        public int coord; // The x/y co-ord of the centre of the region:
                          // 0= on the far right-hand side
                          // 600 = on the far left-hand side                  
    };

    /*
     * Report the details of amount of green pixels seen by the camera
     */ 
	public SceneDetails getGreenAmount()
	{
        SceneDetails result;

		// Discard the first two values to account for the camera lag
		float amount = requestGreenAmount();
		Console.Write("{0:N4} ", amount); 
		amount = requestGreenAmount();
		Console.Write("{0:N4} ", amount); 
		amount = requestGreenAmount();
		Console.WriteLine ("Gn amount: {0:N4}", amount); 

        // The integer part of the amount equals the number of pixels squared 
        // of the biggest green polygon,
        // the decimal part represents how far across the field of view the centre
        // of the polygon is
        result.area = (int)amount;

		float coord = (float)amount - (float)result.area;
		result.coord = (int)(100.0 * coord);

		return result;
	}

    /*
     * Send a message to python process to request details of biggest green polygon.
     * The only green object in the arena should be the target ball
     */ 
	private float requestGreenAmount()
	{
		const string str = "Green request";
		NetworkStream server_stream = client_socket.GetStream ();
		byte[] out_stream = System.Text.Encoding.ASCII.GetBytes (str);
		server_stream.Write (out_stream, 0, out_stream.Length);
		server_stream.Flush ();

        // Wait for response
        const int MAX_MSG_LENGTH = 26;
		byte[] in_stream = new byte [MAX_MSG_LENGTH];

        server_stream.Read (in_stream, 0, MAX_MSG_LENGTH);
		string return_data = System.Text.Encoding.ASCII.GetString (in_stream);

		float green_flt = (float)Convert.ToDouble(return_data);
		return green_flt;
	}

    /*
     * Send a message to python process to request details of biggest white polygon.
     * The only white objects in the arena should be the obstructions
     */
     // TO DO: This method could be combined with requestGreenAmount
    private float requestWhiteAmount()
	{
		const string str = "Barrier request";
		NetworkStream server_stream = client_socket.GetStream ();

		byte[] out_stream = System.Text.Encoding.ASCII.GetBytes (str);
		server_stream.Write (out_stream, 0, out_stream.Length);
		server_stream.Flush ();

        // Wait for response
        const int MAX_MSG_LENGTH = 26;

        byte[] in_stream = new byte[MAX_MSG_LENGTH];
		server_stream.Read (in_stream, 0, MAX_MSG_LENGTH);
		string return_data = System.Text.Encoding.ASCII.GetString (in_stream);

		float white_flt = (float)Convert.ToDouble(return_data);
		return white_flt;
	}

	//
    // When the IR sensors report that we are close to a barrier, this method
    // attempts to determine whether the barrier is a white obstacle or a black
    // wall.
	// Returns true if barrier is white
	// 
	public bool isBarrierObstacle()
	{
		float white_amount = requestWhiteAmount ();

		Console.WriteLine ($"White amount: {white_amount}");

		if (white_amount >= WHITE_WALL_THRESHOLD) 
		{
			Console.WriteLine ("Found white barrier.");
			return true;
		} 
		else 
		{
			Console.WriteLine ("Found black wall.");
			return false;
		}
	}

    /*
     * Returns true if the size of the biggest white polygon seen by the camera
     * is big enough to indicate that we are close to a wall
     */
	public bool amNearWall()
	{
		float white_amount = requestWhiteAmount ();

		Console.WriteLine ($"White amount: {white_amount}");

		if (white_amount < WHITE_STOP_THRESHOLD) 
		{
			Console.WriteLine ("Found black wall.");
			return true;
		} 
		else 
		{
			return false;
		}
	}

    /*
     * Returns true if the size of the biggest green polygon seen by the camera
     * is big enough to indicate that we are close to the target (we can then 
     * drive forwards knowing that we will be driving into the target not a wall
     * or obstacle)
     */
    public bool amNearTarget()
	{
		bool result = false;
		SceneDetails grn_amount = getGreenAmount();

		if(grn_amount.area > NEAR_TARGET)
		    result = true;

		return result;
	}

    /*
     * Returns true if the size of the biggest green polygon seen by the camera
     * is big enough to indicate that we can see the target
     */
    public bool canSeeTarget()
	{
		bool result = false;
		SceneDetails grn_amount = getGreenAmount();

		if(grn_amount.area > FOUND_TARGET)
			result = true;

		return result;
	}
}