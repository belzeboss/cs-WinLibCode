[StructLayout(LayoutKind.Sequential)]
public struct DISPDEV
{
	public int cb;
	[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]
	public string DeviceName;
	[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]
	public string DeviceString;
	public int StateFlags;
	[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]
	public string DeviceID;
	[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]
	public string DeviceKey;
}

[StructLayout(LayoutKind.Sequential)]
public struct DEVMODE
{
	[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]
	public string dmDeviceName;
	public short  dmSpecVersion;
	public short  dmDriverVersion;
	public short  dmSize;
	public short  dmDriverExtra;
	public int    dmFields;

	public Int32 dmPositionX;
	public Int32 dmPositionY;
    public Int32 dmDisplayOrientation;
    public Int32 dmDisplayFixedOutput;

	//public short dmOrientation;
	//public short dmPaperSize;
	//public short dmPaperLength;
	//public short dmPaperWidth;

	//public short dmScale;
	//public short dmCopies;
	//public short dmDefaultSource;
	//public short dmPrintQuality;
	
	public short dmColor;
	public short dmDuplex;
	public short dmYResolution;
	public short dmTTOption;
	public short dmCollate;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string dmFormName;
	public short dmLogPixels;
	public short dmBitsPerPel;
	public int   dmPelsWidth;
	public int   dmPelsHeight;

	public int   dmDisplayFlags;
	public int   dmDisplayFrequency;

	public int   dmICMMethod;
	public int   dmICMIntent;
	public int   dmMediaType;
	public int   dmDitherType;
	public int   dmReserved1;
	public int   dmReserved2;

	public int   dmPanningWidth;
	public int   dmPanningHeight;
	public DISPDEV device;
};

[DllImport("user32.dll")] static extern
bool GetCursorPos(ref System.Drawing.Point point);
static public System.Drawing.Point GetCursorPos() {
	var p = new System.Drawing.Point();
	GetCursorPos(ref p);
	return p;
}

[DllImport("user32.dll")] static extern
bool SystemParametersInfo(UInt32  uiAction, UInt32  uiParam, string pvParam, UInt32  fWinIni);

[DllImport("user32.dll")] static extern
bool SetSysColors(int count, int[] elements, byte[] abgr);

[DllImport("user32.dll")] static extern
int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

[DllImport("user32.dll")] static extern
int GetWindowLong(IntPtr hWnd, int nIndex);

[DllImport("user32.dll", SetLastError = true)] static extern
IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

[DllImport("user32.dll")] static extern
bool EnumDisplaySettingsA(
	string deviceName, 
	int devNum, 
	ref DEVMODE dev);

public enum DISP_CHANGE : int
{
    Successful = 0,
    Restart = 1,
    Failed = -1,
    BadMode = -2,
    NotUpdated = -3,
    BadFlags = -4,
    BadParam = -5,
    BadDualView = -6
}

[DllImport("user32.dll")] static extern
DISP_CHANGE ChangeDisplaySettingsExA(
	string deviceName,
	ref DEVMODE devMode,
	IntPtr hwnd,
	int flags,
	IntPtr lParam);
[DllImport("user32.dll")] static extern
DISP_CHANGE ChangeDisplaySettingsExA(
	string deviceName,
	IntPtr devMode,
	IntPtr hwnd,
	int flags,
	IntPtr lParam);

[DllImport("user32.dll")] static extern
bool EnumDisplayDevicesA(
	string deviceName,
	int modeNum,
	ref DISPDEV dm,
	int flags);

public static DEVMODE GetDisplayMode(int index)
{
	var dm = new DEVMODE();
	dm.dmDeviceName = new String (new char[32]);
	dm.dmFormName = new String (new char[32]);
	dm.dmSize = (short)Marshal.SizeOf(dm);
	if (index < 0) {
		if (!EnumDisplaySettingsA(null, -1, ref dm))
			throw new Exception("FailedToLoadDefault");
		dm.dmDeviceName = null;
	}
	else {
		dm.device.cb = Marshal.SizeOf(dm.device);
		dm.device.DeviceName = new String(new char[32]);
		dm.device.DeviceString = new String(new char[128]);
		dm.device.DeviceID = new String(new char[128]);
		dm.device.DeviceKey = new String(new char[128]);
		if (!EnumDisplayDevicesA(null, index, ref dm.device, 0))
			throw new Exception("FailedToLoadDisplay");
		if (!EnumDisplaySettingsA(dm.device.DeviceName, -1, ref dm))
			throw new Exception("FailedToLoadSpecific");
	}
	return dm;
}
static int BitCountNibble(ulong n)
{
	switch(n)
	{
		case 0: return 0;
		case 1: return 1;
		case 2: return 1;
		case 3: return 2;
		case 4: return 1;
		case 5: return 2;
		case 6: return 2;
		case 7: return 3;
		case 8: return 1;
		case 9: return 2;
		case 10: return 2;
		case 11: return 3;
		case 12: return 2;
		case 13: return 3;
		case 14: return 3;
		case 15: return 4;
		default: return 0;
	}
}
public static int BitCount(ulong n)
{
	int cnt = 0;
	while(n > 0)
	{
		cnt += BitCountNibble(n & 0x0F);
		n >>= 4;
	}
	return cnt;
}

public static DISP_CHANGE ChangeResolution(int index, int w, int h, bool dryRun)
{
	var dm = GetDisplayMode(index);

	dm.dmPelsWidth = w;
	dm.dmPelsHeight = h;

	// CDS_TEST
	var test = ChangeDisplaySettingsExA(
		dm.device.DeviceName,
		ref dm,
		IntPtr.Zero,
		2,
		IntPtr.Zero);
	if (dryRun || test != DISP_CHANGE.Successful)
		return test;

	// CDS_UPDATEREGISTRY
	return ChangeDisplaySettingsExA(
		dm.device.DeviceName,
		ref dm,
		IntPtr.Zero,
		1,
		IntPtr.Zero);
}

static public void AttachHandle(IntPtr hostHandle, IntPtr guestHandle)
{
	const int GWL_STYLE = -16;
	const int WS_CHILD = 0x40000000;
	int newLong = GetWindowLong(guestHandle, GWL_STYLE) | WS_CHILD;
	SetWindowLong(guestHandle, GWL_STYLE, newLong);
	SetParent(guestHandle, hostHandle);
}

static public bool SetColor(byte red, byte green, byte blue)
{
	var abgr = new byte[]{red, green, blue, 0};
	var COLOR_DESKTOP = new int[]{1};
	return SetSysColors(1, COLOR_DESKTOP, abgr);
}

static public string GetWallpaper()
{
	var desktop = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
	return desktop.GetValue("Wallpaper").ToString();
}
static public bool SetWallpaper(string path, int style)
{
	var desktop = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
	if (style != -1)
	{
		desktop.SetValue(@"WallpaperStyle", style.ToString());
		desktop.SetValue(@"TileWallpaper", style == 3 ? "1" : "0");
	}
	desktop.SetValue(@"Wallpaper", path);
	return SystemParametersInfo(0x14, 0, path, 2);
}
static public string GetCursor(string cursorName)
{
	var cursors = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true);
	return cursors.GetValue(cursorName).ToString();
}
static public string[] GetCursors()
{
	var cursors = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true);
	return cursors.GetValueNames();
}
static public bool SetCursor(string cursorName, string cursorFile)
{
	var cursors = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", true);
	cursors.SetValue(cursorName, cursorFile);
	return SystemParametersInfo(0x57, 0, null, 0);
}