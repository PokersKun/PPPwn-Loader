# PPPwn Loader
[中文](README_CN.md)

Deprecated, check out the [PPPwn-Lite](https://github.com/PSGO/PPPwn-Lite) repository.
## Overview
A Windows front-end desktop program based on [PPPwn](https://github.com/TheOfficialFloW/PPPwn) that aims to reduce the environmental dependencies needed to run PPPwn, and implement one-click RCE in the simplest way possible.
## Technology
- NET Framework 4.7.2 based WPF application.
- Interface elements are implemented using [Panuon.WPF.UI](https://github.com/Panuon/Panuon.WPF.UI).
- `pppwn.exe` in the `PPPwn` folder uses a C++ rewrite of [PPPwn_cpp](https://github.com/xfangfang/PPPwn_cpp), `payload` in the `PPPwn` folder is used for testing the `PPPwn.exe` and `PPPwn.exe` in the `PPPwn` folder. The `stage1.bin` and `stage2.bin` files in the `payload` folder for testing are compiled from the [PPPwn](https://github.com/TheOfficialFloW/PPPwn) repository.
## Requirements
- A Windows computer (preferably Windows 10 x64 or above)
- A network cable
- A PS4 (system version 7.50 ~ 11.00)
## Usage
### First time exploit
1. Download the latest build of `PPPwn Loader` from [Release](https://github.com/PokersKun/PPPwn-Loader/releases).
2. Unzip the whole thing and run `PPPwn Loader.exe`, in the first drop down box select the Ethernet port you are connecting to the PS4 (I've tried connecting directly to the PS4 through a cable with better success).
3. Select your PS4's current system version in the second drop-down box (the supported versions in there will change as [PPPwn](https://github.com/TheOfficialFloW/PPPwn) is updated).
4. Click `Select Stage2 File...` to select the stage2.bin file you want to load, you can get the latest stage2.bin and goldhen.bin files from the [GoldHEN](https://github.com/GoldHEN/GoldHEN/releases) repository to use for injecting jailbreak functionality, or you can use the @LightningMods's [PPPwn](https://github.com/LightningMods/PPPwn/releases) branch to get stage2.bin files for various functions, and in addition to that you can try to use the `stage2.bin` file in the `stage2` folder for testing purposes to see if your PS4 can use the exploit.
5. [Optional] Place the Payload file to be loaded, e.g. `goldhen.bin` file, on an `exFAT/FAT32` formatted USB flash drive and insert it into your PS4 console.
6. The `READY` button on the screen should change to a `START` button, clicking on it at this point will prompt `[*] Waiting for PADI...`.
Translated with DeepL.com (free version)
7. Follow [PPPwn#usage](https://github.com/TheOfficialFloW/PPPwn?tab=readme-ov-file#usage) to open a PPPoE connection on your PS4:
    - Go to `Settings` and then `Network`
    - Select `Set Up Internet connection` and choose `Use a LAN Cable`
    - Choose `Custom` setup and choose `PPPoE` for `IP Address Settings`
    - Enter anything for `PPPoE User ID` and `PPPoE Password`
    - Choose `Automatic` for `DNS Settings` and `MTU Settings`
    - Choose `Do Not Use` for `Proxy Server`
    - Click `Test Internet Connection` to communicate with your computer
8. At this point you can see a change in the `PPPwn Loader` GUI, it will start to run PPPwn, please be patient and wait for the result, if it shows "Done" at the end, it means that it was loaded successfully and you will see the result on your PS4.
9. Please keep in mind that the success rate of the current exploit is not 100%, if PPPwn fails, the PPPwn Loader will automatically restart PPPwn if `Auto Retry` is checked by default, you **don't** need to do anything but just wait for the PPPwn to finish automatically (in case of crashing, please follow the following [Second time exploit](### Second time exploit) to re-complete PPPwn).
### Second time exploit
**Note: If you have already successfully injected GoldHEN via PPPwn, you don't need to insert a USB flash drive**
1. When PS4 is not powered on, open the PPPwn Loader and click the `START` button.
2. Power on the PS4 and PPPwn will start automatically.
3. Wait for PPPwn to finish.
## Preview
![preview1](doc/preview1.png)
![preview2](doc/preview2.png)
![preview3](doc/preview3.png)
![preview4](doc/preview4.png)
![preview5](doc/preview5.png)
## Acknowledgments
[@TheOfficialFloW](https://github.com/TheOfficialFloW)
[@xfangfang](https://github.com/xfangfang)
[@Mochengvia](https://github.com/Mochengvia)