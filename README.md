sauce-connect-service
=====================

Windows Service to Setup SauceConnect Tunnel

### Setup
1. Unzip [built version](build/SauceConnect.zip) to a location on your windows machine
2. `C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe C:\path\to\SauceConnectService.exe`
3. Add system-wide environment variables for `SAUCE_CONNECT_ID` and `SAUCE_CONNECT_KEY`
4. Start the service from the services panel

### Arguments
|Argument|Description|
|----|-------|
| `SAUCE_CONNECT_ID` | user id for SauceLabs |
| `SAUCE_CONNECT_KEY` | key for SauceLabs user |
| `SAUCE_CONNECT_ARGS` | addition arguments such as --shared-tunnel |
| `SAUCE_CONNECT_POLLING_INTERVAL` | how often (in milliseconds) to poll to see if the tunnel is still open (default is 30,000) |

### Uninstall
`sc delete SauceConnectService`
