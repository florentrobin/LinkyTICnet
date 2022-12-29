using System;
namespace LinkyTIC.API
{
	public enum TICMode
	{
		Legacy,
		Standard
	}

	public static class TICModeExtension
	{
		public static int BaudRate(this TICMode mode)
		{
			switch (mode)
			{
				case TICMode.Legacy:
					return 1200;
                case TICMode.Standard:
                    return 9600;
				default:
					throw new Exception("Unsupported mode");
            }
		}
	}
}

