(this readme will be a work in progress, duh...)

# Games Infos

A simple collection of various information for your Steam/Epic Games:

- Base Information
- Header image
- Achievements (including images)
- Item list (if the game has them)
- DLC parsing

## How to use

To get data for a Steam game, go to "steam", and search for the AppID of the game/DLC.
To get data for an Epic Store game, go to "epic" and have a look there.

### Can't find your game/DLC?

Get the steam_retriever (currently it can be found on the left in "CI/CD" section - just download the latest artifact which has passed the building), fork the project and add some games yourself.

### steam_retriever usage

```
steam_retriever [-k key|--apikey=key] [-i|--download_images] [-o folder|--out=folder] [-l language|--language=language] appid1 appid2 ...
-k|--apikey key - Steam WebAPI key. Optional. Get it from [here](https://steamcommunity.com/dev/apikey). If no apikey is given, it will only get game's infos and dlcs.
-i|--download_images - Option to retrieve achievements images. Optional. Can take a while for some games.
-o|--out folder - Changes the output folder for the retriever. Optional. By default, it will save everything  to the "steam" folder in your current working directory.
-l|--language language - Changes the language of the achievements definitions. Optional. Default is to retrieve english. It will be in english if there are no achievements in your language.
appid1 appid2 ... - Mandatory - A list of appid to get achievements, stats, dlcs and game infos.

```