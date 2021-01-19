# Stress Test
A vendor plugin for [Saber](https://saber.datasilk.io) that allows webmasters to stress test their web server.

### Prerequisites
* [Saber](https://saber.datasilk.io) ([latest release](https://github.com/Datasilk/Saber/releases))

### Installation
#### For Visual Studio Users
* Clone this repository inside your Saber project within `/App/Vendors/` and name the folder **StressTest**
	> NOTE: use `git clone` instead of `git submodule add` since the contents of the Vendor folder is ignored by git
* Run `gulp vendors` from the root of your Saber project folder

#### For DevOps Users
While using the latest release of Saber, do the following:
* Download latest release of [Saber.Vendors.StressTest](https://github.com/Datasilk/Saber-StressTest/releases)
* Extract all files & folders from either the `win-x64` or `linux-x64` zip folder to Saber's `/Vendors/` folder

### Publish
* run command `./publish.bat`
* publish `bin/Publish/StressTest.7z` as latest release