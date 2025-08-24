# Auto-Type Exclude
[![Version](https://img.shields.io/github/release/michue/autotypeexclude)](https://github.com/michue/autotypeexclude/releases/latest)
[![Releasedate](https://img.shields.io/github/release-date/michue/autotypeexclude)](https://github.com/michue/autotypeexclude/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/michue/autotypeexclude/total?color=%2300cc00)](https://github.com/michue/autotypeexclude/releases/latest/download/AutoTypeExclude.plgx)\
[![License: GPLv3](https://img.shields.io/github/license/michue/autotypeexclude)](https://www.gnu.org/licenses/gpl-3.0)

This KeePass plugin extends KeePass' Auto-Type feature with an additional placeholder, `{EXCLUDE_ENTRY}`. This allows you to exclude an entry from auto-typing based on the target window specifier.

# Table of Contents
- [Usage](#usage)
- [Translations](#translations)
- [Download & updates](#download--updates)
- [Requirements](#requirements)

# Usage
The reduce the configuration amount for type-type, KeePass uses entry titles as filters for window titles by default.
But sometimes you find yourself in the situation, that the entry title matches more windows than you would've liked.
To exclude the entry from auto-typing on those windows, **Auto-Type Exclude** provides the placeholder `{EXCLUDE_ENTRY}`.

Simply create a custom sequence with matching the title of the window, on which you want the entry to be excluded from auto-typing.
As the sequence select the new placeholder `{EXCLUDE_ENTRY}`.
The plugin looks for this placeholder and excludes the entry, if the window specifier matches the active window title.

*Note:* It is sufficient, if the placeholder is only part of the sequence - excluding will work nonetheless.
But since the the rest of the sequence will never be used, it is recommended to trim the sequence to only `{EXCLUDE_ENTRY}` for clarity.

## Example
Let's assume you have an entry for the gaming platform [Steam](https://store.steampowered.com/), but you also are a big fan of industrial heritage and keep an account on [Steam Heritage](https://www.steamheritage.co.uk/).
Using Auto-Type on the login page of *Steam Heritage* will show the selection dialog, since both entry titles match the window title.
But you are never gonna choose your *Steam* credentials in this occasion and are stuck with an additional click each time.

![Auto-Type Entry Selection window](images/AutoTypeExclude%20-%20Auto-Type%20Entry%20Selection.png)

To exclude your *Steam* entry from matching the *Steam Heritage* with this plugin, you open the *Steam* entry and add a custom Auto-Type sequence for this entry.
The window specifier should match the site you want this entry to be excluded from (i.e. `*Steam Heritage*`) and the sequence is simply the new placeholder `{EXCLUDE_ENTRY}`.

![Edit Steam Entry window](images/AutoTypeExclude%20-%20Edit%20Steam%20Entry.png)

The next time you use Auto-Type to login in to your *Steam Heritage* account, you won't to bothered by the selection dialog and can enjoy Britain's industrial heritage right away.

# Translations
Auto-Type Exclude does not expose any user facing text. Therefore translations are not necessary.

# Download & updates
Please follow these links to download the plugin file itself.
- [Download newest release](https://github.com/michue/autotypeexclude/releases/latest/download/AutoTypeExclude.zip)
- [Download history](https://github.com/michue/autotypeexclude/releases)

If you're interested in any of the available translations in addition, please download them from the [Translations](Translations) folder.

In addition to the manual way of downloading the plugin, you can use [EarlyUpdateCheck](https://github.com/rookiestyle/earlyupdatecheck/) to update both the plugin and its translations automatically.
See the corresponding [wiki](https://github.com/Rookiestyle/EarlyUpdateCheck/wiki/One-click-plugin-update) for more details.

# Requirements
* KeePass: 2.59
* .NET framework: 3.5
