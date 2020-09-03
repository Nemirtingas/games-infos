# Games Infos

A simple collection of various information for your Steam/Epic Games library:

- Base Information
- Header image (currently not yet included)
- Achievements (including images) (currently not yet included)
- Items list (if the game has them)
- DLC parsing

## How to use

To get data for a Steam game, go to "steam", and search for the AppID of the game/DLC.
To get data for an Epic Store game, go to "epic" and have a look there.

### Can't find your game/DLC?

Get the steam_retriever (currently it can be found on the left in "CI/CD" section - just download the latest artifact which has passed the building), fork the project and add some games yourself.
Easiest way to add the games is to extract the files from the artifact into the main folder and just run the tool from there.

#### How to use steam_retriever

```
steam_retriever [-k key|--apikey=key] [-i|--download_images] [-o folder|--out=folder] [-l language|--language=language] appid1 appid2 ...
-k|--apikey key - Steam WebAPI key. Optional. Get it from here: https://steamcommunity.com/dev/apikey. If no API key is given, it will only get games' infos and DLCs.
-i|--download_images - Option to retrieve achievements images. Optional. Can take a while for some games.
-o|--out folder - Changes the output folder for the retriever. Optional. By default, it will save everything to the "steam" folder in your current working directory.
-l|--language language - Changes the language of the achievements definitions. Optional. Default is to retrieve english. It will be in english if there are no achievements in your language.
appid1 appid2 ... - Mandatory - A list of appid to get achievements, stats, DLCs and game infos. If unspecified, the app will try to retrieve data for all available Steam games.
--help - Shows the help about the commands available.
```
