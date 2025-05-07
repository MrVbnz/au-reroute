![logo](/Media/icon64x64.png)
## AuReroute

Created to manage audio when using multiple output devices on windows without installing any drivers. It works by capturing loopback and playing it on other devices.

Sample buffer is used so small delay is present on secondary devices. It can be reduced by using lower buffer size as an argument.
```
AuReroute.exe 4194304

```
Default buffer size is 4194304

![Main window screenshot](/Media/main-window.png)
