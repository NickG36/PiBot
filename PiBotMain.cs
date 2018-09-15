using System;
using System.Threading;

namespace PiBot
{
    class PiBotMain
    {
        static bool abort_now = false;

        static void Main(string[] args)
        {
            Console.WriteLine("In main");

            var camera = new Camera();
            var robot = new Robot(camera);

            Console.WriteLine("LOOKING FOR OBSTACLES");

            // The period (in ms) that we should wait after moving to give the IR sensor
            // time to react to any near obstacles
            const int IR_SENSOR_WAIT_PERIOD_MS = 50;

            // The amount of time (in ms) we should move in a straight line before looking
            // for the target again:
            const int STEP_LENGTH_MS = 120;

            const int CENTRE_OF_VIEW = 50;

            const int MAX_NUM_STEPS_IN_ROW = 23; // Before we stop to look for target again
            int num_steps_since_last_stop = 0;

            const int REVERSING_DURATION_MS = 200; // Time to reverse if near blockade

            int prev_turn_idx = -1;
            bool just_started = true;

            Console.WriteLine("Initialising camera"); // TO DO: Add back
                                                      // Initialise camera before moving
            // Allow camera time to settle before we move:
            for (int idx = 0; idx < 10; ++idx)
            {
                camera.amNearWall();
                camera.getGreenAmount();
                Thread.Sleep(100);
            }

            Console.WriteLine("Finished initialising camera");

            while (true && !abort_now)
            {
                bool near_wall = camera.amNearWall();

                if (just_started ||
                    near_wall ||
                    robot.obstacleOnLeft() ||
                    robot.obstacleOnRight())
                {
                    just_started = false;
                    Console.WriteLine(".... Stopping");
                    robot.stop();

                    if (robot.obstacleOnLeft())
                        Console.WriteLine("Obstacle found on left");

                    if (robot.obstacleOnRight())
                        Console.WriteLine("Obstacle found on right");

                    if (near_wall)
                        Console.WriteLine("Near wall");

                    if (camera.amNearTarget())
                    {
                        robot.driveIntoTarget();
                        robot.turnToTarget(useSettlePeriod: true, amFacingWall: false, barrierIsWall: false);
                    }
                    else if (camera.canSeeTarget())
                    {
                        var view = camera.getGreenAmount();
                        Console.WriteLine($"Can see target behind obstruction. Posn: {view.coord}");

                        if (view.coord > CENTRE_OF_VIEW)
                        {
                            // Centre of target is in left half of image
                            Console.WriteLine($"Turning left");
                            robot.turnLeft(90);
                        }
                        else
                        {
                            Console.WriteLine($"Turning right");
                            robot.turnRight(90);
                        }
                    }
                    else
                    {
                        robot.goBackwardsMS(REVERSING_DURATION_MS); 

                        Console.WriteLine("Turning to target due to object in front");

                        // Determine if barrier is obstacle or wall
                        bool is_obstacle = camera.isBarrierObstacle();
                        Console.WriteLine($"is_obstacle : {is_obstacle}");

                        // Keep turning until most promising direction
                        Robot.TurningDetails turn_details = robot.turnToTarget(useSettlePeriod: true,
                                                                               amFacingWall: true,
                                                                               barrierIsWall: !is_obstacle);

                        int dirn_idx = turn_details.dirn_idx;

                        if (turn_details.green_amount > Camera.NEAR_TARGET)
                        {
                            robot.driveIntoTarget();
                            Console.WriteLine("TARGET NEARBY");
                            robot.turnToTarget(useSettlePeriod: true, amFacingWall: false, barrierIsWall: false);
                        }

                        if ((dirn_idx == 1) &&
                           (prev_turn_idx == 1) &&
                           (num_steps_since_last_stop < 2))
                        {
                            // We are just going in a circle and need to break out 
                            const int RIGHT_IDX = 3;
                            robot.turnToIdx(RIGHT_IDX);
                        }

                        prev_turn_idx = dirn_idx;

                        Thread.Sleep(IR_SENSOR_WAIT_PERIOD_MS);
                    }
                    num_steps_since_last_stop = 0;
                }

                robot.goForwardsMS(STEP_LENGTH_MS);  // 10 breaks the gearbox			    
                num_steps_since_last_stop++;
                Thread.Sleep(IR_SENSOR_WAIT_PERIOD_MS);

                if (num_steps_since_last_stop > MAX_NUM_STEPS_IN_ROW)
                {
                    num_steps_since_last_stop = 0;

                    Console.WriteLine("Turning to target due to walk length");

                    // We are probably looking roughly at target so turn left one part then
                    // the turnToTarget can incrementally look right from there.
                    robot.turnLeftOneNotch();
                    Robot.TurningDetails turn_details = robot.turnToTarget(useSettlePeriod: false,
                                                                           amFacingWall: false,
                                                                            barrierIsWall: false);

                    if (turn_details.green_amount > Camera.NEAR_TARGET)
                    {
                        robot.driveIntoTarget();
                        Console.WriteLine("TARGET NEARBY");
                        robot.turnToTarget(useSettlePeriod: false, amFacingWall: false, barrierIsWall: false);
                    }
                    prev_turn_idx = turn_details.dirn_idx;
                    robot.goForwardsMS(STEP_LENGTH_MS);
                }

                Console.Write(".");
            }

            robot.cleanupAllPins();
            Console.WriteLine("Exiting....");
            Thread.Sleep(10000);
        }
    }
}
