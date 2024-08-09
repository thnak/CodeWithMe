if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/js/service-worker.js')
        .then(function (registration) {
            console.log('Service Worker registered with scope:', registration.scope);
        }).catch(function (error) {
        console.log('Service Worker registration failed:', error);
    });
}

window.deleteTemplateCache = () => {
    caches.keys().then(function (names) {
        for (let name of names) caches.delete(name);
    });
}

window.Download = (url) => {
    const ele = document.createElement("a");
    ele.href = url;
    ele.classList.add('d-none');
    document.body.appendChild(ele);
    ele.click();
    document.body.removeChild(ele);
}

window.CloseProgressBar = () => {
    const progressWrapper = document.getElementById("progress-wrapper");
    progressWrapper.classList.add('closed');
}

window.requestNotificationPermission = async () => {
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
        console.log('Notification permission not granted.');
    }
}
window.subscribeUserToPush = async () => {
    const register = await navigator.serviceWorker.ready;
    const subscription = await register.pushManager.subscribe({
        userVisibleOnly: true, applicationServerKey: 'BCpg-oRcOjQLQZs5N_7MBdIqaDDui3G4C_0F3y6iazjmjCyrzYag_Bkh-1KFPLq7eLQggBWkskaYrY_7XWiABYM'
    });

    return {
        endpoint: subscription.endpoint, p256dh: btoa(String.fromCharCode.apply(null, new Uint8Array(subscription.getKey('p256dh')))), auth: btoa(String.fromCharCode.apply(null, new Uint8Array(subscription.getKey('auth'))))
    };
};

window.getCookie = (cname) => {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

window.setCookie = (cname, cvalue, exdays) => {
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

window.RequestFullScreen = () => {
    var elem = document.documentElement;
    if (elem.requestFullscreen) {
        elem.requestFullscreen();
    } else if (elem.msRequestFullscreen) {
        elem.msRequestFullscreen();
    } else if (elem.mozRequestFullScreen) {
        elem.mozRequestFullScreen();
    } else if (elem.webkitRequestFullscreen) {
        elem.webkitRequestFullscreen();
    }
}

function PageShowEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'PageShowEventEventListener')
}

function PageHideEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'PageHideEventEventListener')
}

function ContextMenuEvent(e) {
    e.preventDefault();
    DotNet.invokeMethodAsync("WebApp.Client", 'ContextMenuEventListener')
    DotNet.invokeMethodAsync("WebApp.Client", 'ContextMenuEventListenerWithParam', e.clientX, e.clientY)
}

function KeyDownEvent(event) {
    event.preventDefault();
    if (event.key === "Enter") {
        DotNet.invokeMethodAsync("WebApp.Client", 'EnterEventListener');
    }
    if (event.keyCode === 122) {
        FullScreenEvent();
    }
    
    DotNet.invokeMethodAsync("WebApp.Client", 'KeyPressChangeEventListener', event.keyCode);
}

function OfflineEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'OfflineEventListener')

}

function OnlineEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'OnlineEventListener')
}

let visibilityChange;
if (typeof document.hidden !== "undefined") {
    visibilityChange = "visibilitychange";
} else if (typeof document.mozHidden !== "undefined") { // Firefox up to v17
    visibilityChange = "mozvisibilitychange";
} else if (typeof document.webkitHidden !== "undefined") { // Chrome up to v32, Android up to v4.4, Blackberry up to v10
    visibilityChange = "webkitvisibilitychange";
}

function VisibilitychangeEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'VisibilityChangeEventListener', document.hidden === false)
}

function AppInstalledEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'InstalledEventListener')
}

function FullScreenEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'FullScreenChangeEventListener', document.fullscreenElement != null || window.innerHeight === screen.height)
}

function PageChangeSize() {
    FullScreenEvent();
}

//
// Events Listener
//

window.InitAppEventListener = () => {
    window.addEventListener("pagehide", PageHideEvent);
    window.addEventListener("pageshow", PageShowEvent);
    window.addEventListener("contextmenu", ContextMenuEvent);
    window.addEventListener("keydown", KeyDownEvent);
    window.addEventListener('online', OnlineEvent);
    window.addEventListener('offline', OfflineEvent);
    window.addEventListener(visibilityChange, VisibilitychangeEvent);
    window.addEventListener("appinstalled", AppInstalledEvent);
    document.addEventListener("fullscreenchange", FullScreenEvent);
    window.addEventListener('resize', PageChangeSize);
    console.log("Init event listener");
}