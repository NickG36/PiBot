using System;
using System.Collections.Generic;
using System.IO;

namespace FileGPIONs
{
	public class FileGPIO
	{
        // A list of the pin numbers used by the RoboHat
		public enum enumPIN { 
			gpio0 = 0,
			gpio1 = 1,
			gpio4 = 4,
			gpio5 = 5,
			gpio7 = 7,
			gpio8 = 8,
			gpio9 = 9,
			gpio10 = 10,
			gpio11 = 11,
			gpio12 = 12,
			gpio13 = 13,
			gpio14 = 14,
			gpio15 = 15,
			gpio16 = 16,
			gpio17 = 17,
			gpio18 = 18,
			gpio19 = 19,
			gpio21 = 21,
			gpio22 = 22,
			gpio23 = 23,
			gpio24 = 24,
			gpio25 = 25,
			gpio27 = 27};

		public enum enumDirection {IN, OUT};

        // The gpio operation interation location
		private const string GPIO_PATH = "/sys/class/gpio/";

		// contains list of pins exported with an OUT direction
		List<enumPIN> OutExported = new List<enumPIN>();

		// contains list of pins exported with an IN direction
		List<enumPIN> InExported = new List<enumPIN>();

		private const bool DEBUG = false;

		private void SetupPin(enumPIN pin, enumDirection direction)
		{
			// Unexport it if we're using it already
			if(OutExported.Contains(pin) || InExported.Contains(pin) )
				UnexportPin(pin);

			// Export
			File.WriteAllText(GPIO_PATH + "export", GetPinNumber(pin));

			// Set I/O direction
			File.WriteAllText(GPIO_PATH + pin.ToString() + "/direction", 
						 	 direction.ToString().ToLower());

			// Record the fact that we've set up that pin
			if(direction == enumDirection.OUT)
				OutExported.Add(pin);
			else
				InExported.Add(pin);
		}
			
		// No need to set up pin, this is done already
		public void OutputPin(enumPIN pin, bool value)
		{
			// If we haven't used the pin before, or if we used it as an input before, set it up
			if (!OutExported.Contains (pin) || InExported.Contains (pin))
				SetupPin (pin, enumDirection.OUT);

			string writeValue = "0";
			if (value)
				writeValue = "1";

			File.WriteAllText (GPIO_PATH + pin.ToString () + "/value", writeValue);
		}

		// No need to set up pin, this is done already
		public bool InputPin(enumPIN pin)
		{
			bool returnValue = false;

			// If we haven't used the pin before, or if we used it as an output before, 
			// set it up
			if(!InExported.Contains(pin) || OutExported.Contains(pin) )
				SetupPin(pin, enumDirection.IN);

			string filename = GPIO_PATH + pin.ToString() + "/value";

			if (File.Exists (filename)) 
			{
				string readValue = File.ReadAllText (filename);
				if (readValue != null && readValue.Length > 0 &&
				    readValue [0] == '1') {
					returnValue = true;
				}
			} 
			else 
			{
				throw new Exception (string.Format ("Can't read from {0}. File does not exist", pin));
			}
						
			return returnValue;
		}

		// If for any reason you want to unexport a particular pin, use this, otherwise
		// just call CleanUpAllPins when you're done
		public void UnexportPin(enumPIN pin)
		{
			bool found = false;
			if (OutExported.Contains(pin)) 
			{
				found = true;
				OutExported.Remove(pin);
			}

			if (InExported.Contains (pin)) 
			{
				found = true;
				InExported.Remove (pin);
			}

			if (found) 
			{
				File.WriteAllText (GPIO_PATH + "unexport", GetPinNumber (pin));
			}
		}

		public void CleanUpAllPins()
		{
			// Unexport in reverse order
			for (int p = OutExported.Count - 1; p >= 0; p--) 
			{
				UnexportPin (OutExported [p]);
			}

			for (int p = InExported.Count - 1; p >= 0; p--) 
			{
				UnexportPin (InExported [p]);
			}
		}

		private string GetPinNumber(enumPIN pin)
		{
			return ((int)pin).ToString (); // e.g. returns 17 for enum value of gpio17
		}
	} // end class

} // end namespace
