关于Play to特性，请参考该篇文档，里边有详细的介绍，目前已经在自己的电脑已经实验成功。
http://msdn.microsoft.com/en-us/library/windows/apps/xaml/Hh465191(v=win.10).aspx

具体的使用也比较简单，请考虑下面的例子：
其中的VideoSource就是具体使用的播放器，我看我们用的这个这个开源的播放器也有PlayToSource属性，所以理论应该可以。

    /// <summary>
    /// Invoked when this page is about to be displayed in a Frame.
    /// </summary>
    /// <param name="e">Event data that describes how this page was reached.  The Parameter
    /// property is typically used to configure the page.</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        playToManager = PlayToManager.GetForCurrentView();
        playToManager.SourceRequested += playToManager_SourceRequested;
    }

    void playToManager_SourceRequested(PlayToManager sender, PlayToSourceRequestedEventArgs args)
    {
        var deferral = args.SourceRequest.GetDeferral();
        var handler = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                args.SourceRequest.SetSource(VideoSource.PlayToSource);
                deferral.Complete();
            });
    }

	protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        playToManager.SourceRequested -= playToManager_SourceRequested;
    }

完成程序后实验过程可以参考下面的描述：
6. Create a Play To target (optional)
To run the application, you need a target device to which Play To can stream media. 
If you do not have a certified Play To receiver, 
you can use Windows Media Player as a target device. 
To use Windows Media Player as a target device, 
your computer must be connected to a private network.
Start Windows Media Player.
Expand the Stream menu and enable the Allow remote control of my Player... option. 
Leave Windows Media Player open, because it must be running to be available as a Play To target.

Open the Devices and Printers control panel. 
Click Add devices and printers. 
In the Add devices and printers wizard, in the Choose a device or printer to add to this PC window, 
locate the Digital media renderer for your PC. 
This is the Windows Media Player for your PC. Select it and click Next. 
When the wizard finishes, you will see your instance of Windows Media Player in the list of Multimedia Devices.

7. Run the application
In Visual Studio Express 2012 for Windows 8, 
press F5 (debug) to run the application. 
You can select any of the media buttons to play or view the first media item in the various media libraries. 
To stream the media to the target device while the media is playing, 
open the Devices charm and select your Play To target.