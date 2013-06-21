sauce-connect-service
=====================

Windows Service to Setup SauceConnect Tunnel

### Pre-Requisites
* Microsoft .NET Framework 4.0 or higher
* Java JRE7+

### Setup
1. Unzip [built version](build/SauceConnect.zip) to a location on your windows machine
2. Run `C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe C:\path\to\SauceConnectService.exe` from an Administrator Command Prompt
3. Add system-wide environment variables for `JAVA_HOME`, `SAUCE_CONNECT_ID`, and `SAUCE_CONNECT_KEY`
4. Start the service from the services panel

### Arguments
|Argument|Description|
|----|-------|
| `JAVA_HOME` | path to java home folder |
| `SAUCE_CONNECT_ID` | user id for SauceLabs |
| `SAUCE_CONNECT_ID` | user id for SauceLabs |
| `SAUCE_CONNECT_KEY` | key for SauceLabs user |
| `SAUCE_CONNECT_ARGS` | addition arguments such as --shared-tunnel |
| `SAUCE_CONNECT_POLLING_INTERVAL` | how often (in milliseconds) to poll to see if the tunnel is still open [default is 30000] |

### Uninstall
`sc delete SauceConnectService`
