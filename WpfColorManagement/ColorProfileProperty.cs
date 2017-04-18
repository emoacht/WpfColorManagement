using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WpfColorManagement
{
	public class ColorProfileProperty : Freezable
	{
		#region Freezable member

		protected override Freezable CreateInstanceCore()
		{
			return new ColorProfileProperty();
		}

		#endregion

		public static ColorProfileProperty GetAttachedProperty(Window window)
		{
			return (ColorProfileProperty)window.GetValue(AttachedPropertyProperty);
		}
		public static void SetAttachedProperty(Window window, ColorProfileProperty attachedProperty)
		{
			window.SetValue(AttachedPropertyProperty, attachedProperty);
		}
		public static readonly DependencyProperty AttachedPropertyProperty =
			DependencyProperty.RegisterAttached(
				"AttachedProperty",
				typeof(ColorProfileProperty),
				typeof(ColorProfileProperty),
				new FrameworkPropertyMetadata(
					null,
					(d, e) =>
					{
						var window = d as Window;
						if (window == null)
							return;

						((ColorProfileProperty)e.NewValue).OwnerWindow = window;
					}));

		public string ColorProfilePath
		{
			get { return (string)GetValue(ColorProfilePathProperty); }
			set { SetValue(ColorProfilePathProperty, value); }
		}
		public static readonly DependencyProperty ColorProfilePathProperty =
			DependencyProperty.Register(
				"ColorProfilePath",
				typeof(string),
				typeof(ColorProfileProperty),
				new FrameworkPropertyMetadata(null));

		private Window OwnerWindow
		{
			get { return _ownerWindow; }
			set
			{
				_ownerWindow = value;

				_ownerWindow.SourceInitialized += OnSourceInitialized;
				_ownerWindow.Loaded += OnLoaded;
				_ownerWindow.Closed += OnClosed;
			}
		}
		private Window _ownerWindow;

		private void OnSourceInitialized(object sender, EventArgs e)
		{
			OwnerWindow.SourceInitialized -= OnSourceInitialized;

			ColorProfilePath = GetColorProfilePath(OwnerWindow);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			OwnerWindow.Loaded -= OnLoaded;

			OwnerWindow.LocationChanged += OnLocationSizeChanged;
			OwnerWindow.SizeChanged += OnLocationSizeChanged;
		}

		private void OnClosed(object sender, EventArgs e)
		{
			OwnerWindow.Closed -= OnClosed;

			OwnerWindow.LocationChanged -= OnLocationSizeChanged;
			OwnerWindow.SizeChanged -= OnLocationSizeChanged;
		}

		private void OnLocationSizeChanged(object sender, EventArgs e)
		{
			var filePath = GetColorProfilePath(OwnerWindow);

			if (string.IsNullOrEmpty(filePath) ||
				filePath.Equals(ColorProfilePath, StringComparison.OrdinalIgnoreCase))
				return;

			ColorProfilePath = filePath;
		}

		/// <summary>
		/// Get color profile file path used by the monitor to which a specified Window belongs.
		/// </summary>
		/// <param name="sourceVisual">Source Window</param>
		/// <returns>Color profile file path</returns>
		private static string GetColorProfilePath(Visual sourceVisual)
		{
			var source = PresentationSource.FromVisual(sourceVisual) as HwndSource;
			if (source == null)
				return null;

			var monitorHandle = NativeMethod.MonitorFromWindow(
				source.Handle,
				NativeMethod.MONITOR_DEFAULTTO.MONITOR_DEFAULTTONEAREST);

			var monitorInfo = new NativeMethod.MONITORINFOEX
			{
				cbSize = (uint)Marshal.SizeOf(typeof(NativeMethod.MONITORINFOEX))
			};

			if (!NativeMethod.GetMonitorInfo(monitorHandle, ref monitorInfo))
				return null;

			IntPtr deviceContext = IntPtr.Zero;

			try
			{
				deviceContext = NativeMethod.CreateDC(
					monitorInfo.szDevice,
					monitorInfo.szDevice,
					null,
					IntPtr.Zero);

				if (deviceContext == IntPtr.Zero)
					return null;

				// First, get the length of file path.
				var lpcbName = 0U;
				NativeMethod.GetICMProfile(deviceContext, ref lpcbName, null);

				// Second, get the file path using StringBuilder which has the same length. 
				var sb = new StringBuilder((int)lpcbName);
				NativeMethod.GetICMProfile(deviceContext, ref lpcbName, sb);

				return sb.ToString();
			}
			finally
			{
				if (deviceContext != IntPtr.Zero)
					NativeMethod.DeleteDC(deviceContext);
			}
		}
	}
}