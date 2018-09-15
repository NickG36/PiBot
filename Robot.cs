
using System;
using System.Threading;
using System.Collections.Generic;
using FileGPIONs;

public class Robot
{
	const double WHOLE_CIRCLE_LEFT_PINGS = 60.0; // The odometer reports this many times per wheel revolution
	private const int NUM_DIRECTIONS = 12; // How many times in a circle to look for target
	private const float SECTOR_WIDTH_DEGS = 360.0f/ (float)NUM_DIRECTIONS;
	private const int SECTOR_WIDTH_DEGS_INT = (int)(SECTOR_WIDTH_DEGS);

	static FileGPIO gpio = new FileGPIO ();
	private Camera camera = null;

	const FileGPIO.enumPIN LEFT_FORWARDS_PIN = FileGPIO.enumPIN.gpio16;
	const FileGPIO.enumPIN RIGHT_FORWARDS_PIN = FileGPIO.enumPIN.gpio13;
	const FileGPIO.enumPIN LEFT_BACKWARDS_PIN = FileGPIO.enumPIN.gpio19;
	const FileGPIO.enumPIN RIGHT_BACKWARDS_PIN = FileGPIO.enumPIN.gpio12;

	public Robot(Camera the_camera)
	{
	    camera = the_camera;
	    camera.SetUp();
		Camera.SceneDetails dummy = camera.getGreenAmount();
	}

	public bool leftWheelSensorState()
	{
	    return gpio.InputPin (FileGPIO.enumPIN.gpio22);
	}

	public bool rightWheelSensorState()
	{
		return gpio.InputPin (FileGPIO.enumPIN.gpio23);
	}

	/**
	 * Will return after so many rotation 'pings' of the left wheel
	 */
	public void stopAfterThisManyPings(int numLeftWheelPings)
	{
		// If the right gearbox fails, increase the number of required left pings
		// by this amount:
		const float GEARBOX_FAILURE_INCREASE_FACTOR = 2.1f;  

		int right_count = 0;
		int left_count = 0;

		bool prevRight = rightWheelSensorState();
		bool prevLeft = leftWheelSensorState();

		bool message_shown = false;

		while(left_count < numLeftWheelPings)
		{
			// See if only one gearbox is moving - sometimes one will fail periodically
			if( (left_count == 4) && !message_shown)
			{
			   if(right_count <= 1)
			   {
			      // Left side is driving but right side isn't
			      numLeftWheelPings = (int)((float)(numLeftWheelPings) * GEARBOX_FAILURE_INCREASE_FACTOR);
			      Console.WriteLine($"RT NOT WORKING: Increasing cut-off factor to {numLeftWheelPings}");
			   }
			   message_shown = true;
			}

			if( (right_count == 4) && !message_shown)
			{
				if(left_count <= 1)
				{
					// Right side is driving but left side isn't
					// Stop for a bit then try turning clockwise to try to free the wheel before returning
					Thread.Sleep(2000);

					gpio.OutputPin(LEFT_FORWARDS_PIN, true);
					gpio.OutputPin(RIGHT_BACKWARDS_PIN, true);

					stop();

					return;
				}
				message_shown = true;
			}
			bool feedbackRight = rightWheelSensorState();
			bool feedbackLeft = leftWheelSensorState();

			if (feedbackRight && !prevRight) {
				right_count++;
			}

			if (feedbackLeft && !prevLeft) {
				left_count++;
			}

			prevRight = feedbackRight;
			prevLeft = feedbackLeft;
		}
	}
	/**
	 *  Will go forwards until so many rotation pings have been reached
	 */
	public void goForwards(int numRtWheelPings)
	{
		goForwards();
		stopAfterThisManyPings(numRtWheelPings);
		stop();
	}

	/**
	 *  Will go backwards until so many rotation pings have been reached
	 */
	public void goBackwards(int numRtWheelPings)
	{
		goBackwards();
		stopAfterThisManyPings(numRtWheelPings);
		stop();
	}

	public void goForwards()
	{	
		stop();

		gpio.OutputPin(LEFT_FORWARDS_PIN, true);
		gpio.OutputPin(RIGHT_FORWARDS_PIN, true);
	}

	public void goBackwards()
	{
		stop();
		Console.WriteLine ($"MOVING BACKWARDS");
		gpio.OutputPin(LEFT_BACKWARDS_PIN, true);
		gpio.OutputPin(RIGHT_BACKWARDS_PIN, true);
	}

	public void goForwardsMS(int duration_ms)
	{	
		stop();
		goForwards();
		Thread.Sleep(duration_ms);
		stop();
	}

	public void goBackwardsMS(int duration_ms)
	{	
		stop();
		goBackwards();
		Thread.Sleep(duration_ms);
		stop();
	}

	public void stop()
	{
		gpio.OutputPin(RIGHT_FORWARDS_PIN, false);
		gpio.OutputPin(LEFT_FORWARDS_PIN, false);
		gpio.OutputPin(LEFT_BACKWARDS_PIN, false);
		gpio.OutputPin(RIGHT_BACKWARDS_PIN, false);
	}

	public void testRightGearbox()
	{
		stop();
		Console.WriteLine ($"TURNING RIGHT");

		gpio.OutputPin(RIGHT_FORWARDS_PIN, true);
		Thread.Sleep(5000);

		gpio.OutputPin(RIGHT_FORWARDS_PIN, false);
		gpio.OutputPin(RIGHT_BACKWARDS_PIN, true);
		Thread.Sleep(5000);

		gpio.OutputPin(RIGHT_BACKWARDS_PIN, false);
	}

	public void turnRightNumPings(int num_pings)
	{
		stop();

		gpio.OutputPin(LEFT_FORWARDS_PIN, true);
		gpio.OutputPin(RIGHT_BACKWARDS_PIN, true);

		stopAfterThisManyPings(num_pings);

		gpio.OutputPin(LEFT_FORWARDS_PIN, false);
		gpio.OutputPin(RIGHT_BACKWARDS_PIN, false);
	}

	public void turnLeftNumPings(int num_pings)
	{
		stop();

		gpio.OutputPin(LEFT_BACKWARDS_PIN, true);
		gpio.OutputPin(RIGHT_FORWARDS_PIN, true);

		stopAfterThisManyPings(num_pings);

		gpio.OutputPin(LEFT_BACKWARDS_PIN, false);
		gpio.OutputPin(RIGHT_FORWARDS_PIN, false);
	}

	public void turnLeftOneNotch()
	{
		turnLeft(SECTOR_WIDTH_DEGS_INT);
	}

	public void turnRightOneNotch()
	{
		turnRight(SECTOR_WIDTH_DEGS_INT);
	}

	public void turnRight(float num_degs)
	{
		int num_pings = (int)(num_degs * WHOLE_CIRCLE_LEFT_PINGS / 360.0);
		turnRightNumPings(num_pings);
	}

	public void turnLeft(float num_degs)
	{
		int num_pings = (int)(num_degs * WHOLE_CIRCLE_LEFT_PINGS / 360.0);
		turnLeftNumPings(num_pings);
	}

	public struct TurningDetails
	{
		public double green_amount;
		public int    dirn_idx;
	};

	/*
	 ** Will look round in a full circle for the most green direction, then
	 *  will turn to face that direction
	 */
	private class ViewByIdx
	{
		public int dirn_idx;   // Which direction we have to turn to face the polygon
		public Camera.SceneDetails scene;

		public ViewByIdx(int dirn_idx, Camera.SceneDetails scene)
		{
			this.dirn_idx = dirn_idx;
			this.scene = scene;
		}
	};

    /*
     * Turns to face a given number of arc segments
     */ 
	public void turnToIdx(int turn_idx)
	{
       // Always turn left
       for(int dirn_idx = NUM_DIRECTIONS; dirn_idx > turn_idx; --dirn_idx)
       {
          Console.WriteLine("Turning left . .");
          turnLeft(SECTOR_WIDTH_DEGS_INT);
       }
	}

	public void driveIntoTarget()
	{
        const int FIVE_SECONDS_IN_MS = 1000 * 5;
        const int DRIVE_INTO_TARGET_TIME_MS = 300;

		goForwardsMS (DRIVE_INTO_TARGET_TIME_MS);
		Console.WriteLine ("TARGET FOUND!!");
		Thread.Sleep (FIVE_SECONDS_IN_MS);
	}

	/*
	 ** Returns true if we are immediately in front of the target
	 */
	public bool isFacingTarget()
	{
		return camera.amNearTarget();
	}

	// Uses yellow IR (obstacle) sensor to determine if an object is near
	public bool obstacleOnLeft()
	{
		return !gpio.InputPin(FileGPIO.enumPIN.gpio4);
	}

	public bool obstacleOnRight()
	{
		return !gpio.InputPin(FileGPIO.enumPIN.gpio17);
	}

	public void cleanupAllPins()
	{
		gpio.CleanUpAllPins();
	}

	public TurningDetails turnToTarget(bool useSettlePeriod, bool amFacingWall, bool barrierIsWall)
	{
		TurningDetails result = new TurningDetails();

		// Look in diff directions and consider the amount of green

        const int SETTLE_TIME_MS = 500;

		// If we have seen the target then stop turning when we can no longer see the target and
		// turn to the direction which has the target in the most central position
		// If we haven't seen enough target area to believe we have seen the target then keep turning
		// and select the direction which has the biggest green area
		// 

		// The direction that has most green:
		int best_green_amount = -1;
		int best_green_dirn_idx = 0;

		bool target_seen = false;

		// The direction that has the target most centrally:
		int most_central_target_idx = -1;
		int most_central_target_posn = -1;

		const int CENTRAL_COORD = 50;
		const int FIELD_OF_VIEW_HALF_WIDTH_DEGS = 25;

		int dirn_idx = 0;
		for(; dirn_idx < NUM_DIRECTIONS - 1; ++dirn_idx)
		{
			Console.WriteLine ($"-----------Idx {dirn_idx}");
			Camera.SceneDetails dummy = camera.getGreenAmount();
			if(useSettlePeriod)
  	          Thread.Sleep(SETTLE_TIME_MS);
	
			Camera.SceneDetails curr_grn_amount = camera.getGreenAmount();	

			if (curr_grn_amount.area > Camera.FOUND_TARGET)
            {
                // We are confident that we can see the target ...
				if (!target_seen) {
                    // .. and we haven't already seen it. This is the best dirction
                    // so far.
					most_central_target_idx = dirn_idx;
					most_central_target_posn = curr_grn_amount.coord;
					Console.WriteLine ($"Now found target: {dirn_idx}: area {curr_grn_amount.area}, coord {curr_grn_amount.coord}");
				} 
				else
				{
                    // .. but we could already see it. See if it is more centrally placed
                    // than before
                    Console.WriteLine ($"Found target: {dirn_idx}: area {curr_grn_amount.area}, coord {curr_grn_amount.coord}");

					if (Math.Abs(curr_grn_amount.coord - CENTRAL_COORD) < Math.Abs (most_central_target_posn - CENTRAL_COORD)) 
					{
						Console.WriteLine ($"This is the best coord now - target: {dirn_idx}: area {curr_grn_amount.area}, coord {curr_grn_amount.coord}");
						most_central_target_idx = dirn_idx;
						most_central_target_posn = curr_grn_amount.coord;
					}
				}
				target_seen = true;
			} else if (target_seen == true) {
				Console.WriteLine ("Can no longer see target");
				// Target was visible but now is not - stop turning and turn back to an angle that showed the target.
				break;
			}

			//Console.WriteLine($"Diff: {(curr_grn_amount.area - curr_grn_amount1.area)}");
			if(curr_grn_amount.area > best_green_amount)
			{
                // If we can see more green than before then store this index too
			    Console.Write($"{dirn_idx}, best so far!:");
			    Console.WriteLine("{0:N4}:", curr_grn_amount);
			    best_green_amount = curr_grn_amount.area;
			    best_green_dirn_idx = dirn_idx;
			}

			Console.Write($"Idx {dirn_idx} , found green amount: {curr_grn_amount.area}");
			Console.Write(", Best so far: ", best_green_amount);
			Console.WriteLine($", idx {best_green_dirn_idx}");

            // Turn and look again
			turnRight(SECTOR_WIDTH_DEGS_INT);
		}

        // We have finished turning. Consider what action to take...
		if(!target_seen)
 		   Console.WriteLine($"Best was idx {best_green_dirn_idx}");
		else
			Console.WriteLine($"Best target was idx {most_central_target_idx}");

		if (!target_seen)
        {
			if (best_green_amount < Camera.NOISE_THRESHOLD) {
				// We have not seen anything that looks like the target... 
				// Just turn round if we are facing a wall, keep going if we were striding, 
				// turn right if we are facing an obstruction

				if (amFacingWall)
				{	
					if (barrierIsWall) {
						Console.WriteLine ("Can't see target - facing wall - turning round");
						for (int turn_idx = 0; turn_idx < NUM_DIRECTIONS / 2; ++turn_idx)
                        {
							turnRight (SECTOR_WIDTH_DEGS_INT);
						}
						result.dirn_idx = (int)(NUM_DIRECTIONS / 2);
					} else {
						Console.WriteLine ("Can't see target - facing obstacle - turning right");
						for (int turn_idx = 0; turn_idx < 3; ++turn_idx)
                        {
							turnRight (SECTOR_WIDTH_DEGS_INT);
						}
						result.dirn_idx = 4;
					}
				}
				else
				{	
					Console.WriteLine ("Can't see target - keeping going");
					result.dirn_idx = 0;
				}
			}
			else 
			{
				Console.WriteLine ("Can't definitely see target - turning to face best candidate");
				for (; dirn_idx > best_green_dirn_idx; --dirn_idx)
                {
					Console.WriteLine ($"Turning left, dirn_idx {dirn_idx}, best_gn_idx {best_green_dirn_idx}");
					turnLeft (SECTOR_WIDTH_DEGS_INT);
				}
				result.dirn_idx = best_green_dirn_idx;
			}
		}
		else
        {
            // TO DO- this method is very big - put the rest in a separate method:


            // Target was seen...

            // Take into account how far across the field of view the centre of the target is at the best idx
            // We can't turn less than SECTOR_WIDTH_DEGS_INT because the gearbox jams...
            // So instead turn an extra amount for the previous turn
            bool at_least_one_turn_needed = false;
            bool at_least_two_turns_needed = false;

			result.dirn_idx = most_central_target_idx;

            if(dirn_idx > most_central_target_idx)
            {
                at_least_one_turn_needed = true;
                Console.WriteLine("At least one turn needed");
            }

            if(dirn_idx > most_central_target_idx + 1)
            {
                at_least_two_turns_needed = true;
                Console.WriteLine("At least two turns needed");
            }

			for (; dirn_idx > most_central_target_idx + 2; --dirn_idx) {
				Console.WriteLine ($"Turning left, dirn_idx {dirn_idx}, most_central_target_idx {most_central_target_idx}");
				turnLeft (SECTOR_WIDTH_DEGS_INT);
			}

            // If we have to turn at all, then make the last turn extra big if necessary if the target position is
            // to the left of the field of vision (coord > halfway)
            // If it is to the left then 
            if(most_central_target_posn > CENTRAL_COORD)
            {
			   Console.WriteLine ("Need to turn further for last turn");
   			   int angle_of_last_turn = SECTOR_WIDTH_DEGS_INT + (int)( (most_central_target_posn - CENTRAL_COORD) * FIELD_OF_VIEW_HALF_WIDTH_DEGS / CENTRAL_COORD);
			   Console.WriteLine ($"Need to turn: {angle_of_last_turn} degs for last turn: posn: {most_central_target_posn}");

                if(at_least_two_turns_needed)
                {
                  Console.WriteLine($"Turning one normal amount and one big amount");
			      turnLeft (SECTOR_WIDTH_DEGS_INT);
			      turnLeft (angle_of_last_turn);
                }
                else if(at_least_one_turn_needed)
                {
                   Console.WriteLine($"Turning one big amount");
			       turnLeft (angle_of_last_turn);
                }
            }
            else
            {
			   Console.WriteLine ("Need to turn less far for for last turn- make one fewer turns but make last one bigger");

				int angle_of_last_turn =  2 * SECTOR_WIDTH_DEGS_INT + (int)( (most_central_target_posn - CENTRAL_COORD) * FIELD_OF_VIEW_HALF_WIDTH_DEGS / CENTRAL_COORD);
			   Console.WriteLine ($"Need to turn: {angle_of_last_turn} degs for last turn: posn: {most_central_target_posn}");

                if(at_least_two_turns_needed)
                {
                   Console.WriteLine($"Turning one big amount (making one fewer turn than otherwise)");
			      turnLeft (angle_of_last_turn);
                }
                else if(at_least_one_turn_needed)
                {
                     // We can't risk damaging the gearbox with a short turn - just make a normal turn
                     Console.WriteLine($"Turning normally");
			         turnLeft (SECTOR_WIDTH_DEGS_INT);
                }

            }

		}
		Console.WriteLine($"Finished turning");
		return result;
	}


}