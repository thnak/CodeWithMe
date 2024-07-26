using Microsoft.JSInterop;

namespace WebApp.Client.Services.UserInterfaces;

public class DocumentObjectModelEventListener : IDisposable
{
    #region Enter

    public Func<Task>? EnterClickedAsync
    {
        get => SelfEnterClickedAsync;
        set => SelfEnterClickedAsync = value;
    }

    public Action? EnterClicked
    {
        get => SelfEnterClicked;
        set => SelfEnterClicked = value;
    }

    private static Func<Task>? SelfEnterClickedAsync { get; set; }
    private static Action? SelfEnterClicked { get; set; }

    [JSInvokable]
    public static void EnterEventListener()
    {
        SelfEnterClickedAsync?.Invoke();
        SelfEnterClicked?.Invoke();
    }

    #endregion

    #region Context Menu

    public Func<Task>? ContextMenuClickedAsync
    {
        get => SelfContextMenuClickedAsync;
        set => SelfContextMenuClickedAsync = value;
    }

    public Func<int, int, Task>? ContextMenuClickedWithParamAsync
    {
        get => SelfContextMenuClickedWithParamAsync;
        set => SelfContextMenuClickedWithParamAsync = value;
    }

    public static Action? ContextMenuClicked
    {
        get => SelfContextMenuClicked;
        set => SelfContextMenuClicked = value;
    }

    private static Func<Task>? SelfContextMenuClickedAsync { get; set; }
    private static Func<int, int, Task>? SelfContextMenuClickedWithParamAsync { get; set; }
    private static Action? SelfContextMenuClicked { get; set; }

    [JSInvokable]
    public static void ContextMenuEventListener()
    {
        SelfContextMenuClickedAsync?.Invoke();
        SelfContextMenuClicked?.Invoke();
    }

    [JSInvokable]
    public static void ContextMenuEventListenerWithParam(int x, int y)
    {
        SelfContextMenuClickedWithParamAsync?.Invoke(x, y);
    }

    #endregion

    #region Offline

    public Func<Task>? OfflineAsync
    {
        get => SelfOfflineClickedAsync;
        set => SelfOfflineClickedAsync = value;
    }

    public Action? Offline
    {
        get => SelfOfflineClicked;
        set => SelfOfflineClicked = value;
    }

    private static Func<Task>? SelfOfflineClickedAsync { get; set; }
    private static Action? SelfOfflineClicked { get; set; }

    [JSInvokable]
    public static void OfflineEventListener()
    {
        SelfOfflineClickedAsync?.Invoke();
        SelfOfflineClicked?.Invoke();
    }

    #endregion


    #region Online

    public Func<Task>? OnlineAsync
    {
        get => SelfOnlineAsync;
        set => SelfOnlineAsync = value;
    }

    public Action? Online
    {
        get => SelfOnline;
        set => SelfOnline = value;
    }

    private static Func<Task>? SelfOnlineAsync { get; set; }
    private static Action? SelfOnline { get; set; }

    [JSInvokable]
    public static void OnlineEventListener()
    {
        SelfOnlineAsync?.Invoke();
        SelfOnline?.Invoke();
    }

    #endregion

    #region Page Hide

    public Func<Task>? PageHideEventAsync
    {
        get => SelfPageHideEventAsync;
        set => SelfPageHideEventAsync = value;
    }

    public Action? PageHideEvent
    {
        get => SelfPageHideEvent;
        set => SelfPageHideEvent = value;
    }

    private static Func<Task>? SelfPageHideEventAsync { get; set; }
    private static Action? SelfPageHideEvent { get; set; }

    [JSInvokable]
    public static void PageHideEventEventListener()
    {
        SelfPageHideEventAsync?.Invoke();
        SelfPageHideEvent?.Invoke();
    }

    #endregion

    #region Page Show

    public Func<Task>? PageShowEventAsync
    {
        get => SelfPageShowEventAsync;
        set => SelfPageShowEventAsync = value;
    }

    public Action? PageShowEvent
    {
        get => SelfPageShowEvent;
        set => SelfPageShowEvent = value;
    }

    private static Func<Task>? SelfPageShowEventAsync { get; set; }
    private static Action? SelfPageShowEvent { get; set; }


    [JSInvokable]
    public static void PageShowEventEventListener()
    {
        SelfPageShowEventAsync?.Invoke();
        SelfPageShowEvent?.Invoke();
    }

    #endregion

    #region Scroll

    public static Func<Task>? ScrollEventAsync { get; set; }
    public static Action? ScrollEvent { get; set; }

    [JSInvokable]
    public static void ScrollEventEventListener()
    {
        ScrollEventAsync?.Invoke();
        ScrollEvent?.Invoke();
    }

    #endregion

    public void Dispose()
    {
        ScrollEventAsync = null;
        ScrollEvent = null;

        PageHideEvent = null;
        PageHideEventAsync = null;

        PageShowEvent = null;
        PageShowEventAsync = null;

        Online = null;
        OnlineAsync = null;

        Offline = null;
        OfflineAsync = null;

        ContextMenuClickedAsync = null;
        ContextMenuClicked = null;
    }
}