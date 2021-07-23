using System;

/// <summary>
/// Summary description for Class1
/// </summary>
public class Class1
{
	public Class1()
	{
		//
		// TODO: Add constructor logic here
		//
	}

	public bool checkParadrome(string phase)
	{
		bool bCheck = false;

		if (string.IsNullOrWhiteSpace(phase))
        {
			return false;
        }

		phase = phase.Trim();

		int len =  phase.Length;

		if (len == 0)
        {
			bCheck = false;
        }
		else if (len == 1)
        {
			bCheck = true;
        }
		else if (len > 1)
        {
			/* Method 1:  divide and compare.  (Method 2 is much better)
			if (len % 2 == 0)
            {
				// check paradrome			
            }
			else
            {
				// remove the center
				// check paradrome
            }
			*/

			// Method 2: this is better and simpler
			bCheck = true;

			for (int i=0, j = len-1; i < j; i++, j--)
			{
				if (phase[i] != phase[j])
				{
					bCheck = false;
					break;
				}
			}

		}

		return bCheck;
	}

}
