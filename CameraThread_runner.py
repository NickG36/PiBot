#!/usr/bin/python
# -*- coding: utf-8 -*-
"""
Provides the test functionality for the Camera Thread
"""

import logging
import time
import cv2
import SetupConsoleLogger
import NGCameraThread
import operator
import numpy as np

LOGGER = logging.getLogger(__name__)
SetupConsoleLogger.setup_console_logger(LOGGER, logging.DEBUG)


class Processor(object):
    """
    Defines a class to apply additional processing to the image
    returned by the callback
    """

    def __init__(self):
        """
        Constructor
        """
        LOGGER.debug("Processor constructor called")

        self.width = 320
        self.height = 240

    def __del__(self):
        """
        Destructor
        """
        self.cleanup()

    def cleanup(self):
        """
        Cleanup
        """

    def findBlackAmount(self, bgr_image, width, height):
	    print ("findBlackAmount")
        # Convert the image from 'BGR' to HSV colour space
	    height,width, channels=bgr_image.shape
	    upper_left= (int(width/4), int(height/4))
	    bottom_right = (int(width *3/4), int(height *3/4))

	    centre_bgr_img = bgr_image[upper_left[1] : bottom_right[1], upper_left[0]: bottom_right[0]]

	    grey_sm_img = cv2.cvtColor(centre_bgr_img, cv2.COLOR_BGR2GRAY)
	    ret1, grey_sm_thresh = cv2.threshold(grey_sm_img, 30, 255, cv2.THRESH_BINARY)

	    tolerance = 20
	    im2, contours, hierarchy = cv2.findContours(grey_sm_thresh, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

	    largest_area = 0
	    largest_contour_index = 0
	    counter = 0

	    x_coord_of_largest_area_centre = -1
	    for i in contours:
	        area = cv2.contourArea(i)

	    if(area > largest_area):
	        largest_area = area
	        largest_contour_index = counter
	        x,y,w,h = cv2.boundingRect(i)
	        x_coord_of_largest_area_centre = x + w/2.0

	    counter = counter + 1

	    print ("largest white area %.2f" % largest_area)

	    result = largest_area

	    print ("result %.2f" % result)
        #cv2.imshow('Grey Sm T.', grey_sm_thresh)

	    cv2.waitKey(1) & 0xFF

	    return result

    def findGreenAmount(self, bgr_image, width, height):
        print ("findGreenAmount")
        # Convert the image from 'BGR' to HSV colour space
        hsv_image = cv2.cvtColor(bgr_image, cv2.COLOR_BGR2HSV)

	#tolerance = 30
        tolerance = 20
        green_col = 65
        GREEN_MIN = np.array([green_col-tolerance,50,50], np.uint8)
        GREEN_MAX = np.array([green_col+tolerance,255,255], np.uint8)
        frame_threshed = cv2.inRange(hsv_image, GREEN_MIN, GREEN_MAX)

        im2, contours, hierarchy = cv2.findContours(frame_threshed, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

        largest_area = 0
        largest_contour_index = 0
        counter = 0

        x_coord_of_largest_area_centre = -1
        for i in contours:
            area = cv2.contourArea(i)

            if(area > largest_area):
                largest_area = area
                largest_contour_index = counter
                x,y,w,h = cv2.boundingRect(i)
                x_coord_of_largest_area_centre = x + w/2.0
                
            counter = counter + 1

        print ("largest contour area %.2f" % largest_area)
        print ("x_coord_of_largest_area_centre %i" % x_coord_of_largest_area_centre)

        if x_coord_of_largest_area_centre == -1:
            result = 0.0
        else:
            intArea = int(largest_area)
            intCoord = int(x_coord_of_largest_area_centre)

            result = float(intArea) + float(intCoord/600.0);

        print ("result %.2f" % result)
    
        return result

def main():
    """
    Performs the "Camera Capture and stream mechanism" test
    """
    LOGGER.info("'Camera Capture and stream mechanism' Starting.")
    LOGGER.info("CTRL^C to terminate program")

    try:
	    print ("====Calling processor")
        # Create the object that will process the images
        # passed in to the image_process_entry function
	    image_processor = Processor()

	    print ("====Have called processor")
        # Start stream process to handle images and
        # pass then to the callback function
	    stream_processor = NGCameraThread.StreamProcessor(
            640, 480, image_processor.findGreenAmount, 
        image_processor.findBlackAmount,
            False)

	    print ("====Sleeping")
        # Wait for the interval period for finishing
	    time.sleep(3000)

    except KeyboardInterrupt:
        LOGGER.info("Stopping 'Camera Capture and stream mechanism'.")

    finally:
        stream_processor.exit_now()
        stream_processor.join()
        image_processor.cleanup()
        cv2.destroyAllWindows()

    LOGGER.info("'Camera Capture and stream mechanism' Finished.")


if __name__ == "__main__":
    main()
