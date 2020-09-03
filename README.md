(this readme will be a work in progress, duh...)

## Games Infos

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
steam_retriever [appids] -k [key] (-i) (-o [folder])
[appids] - the AppID list of the game info you want to retrieve. Separated by Space. Mandatory-ish, because otherwise (if unspecified or wrongly separated) it will try to retrieve information for ALL the AppIDs available.
-k [key] - Steam WebAPI key. Mandatory. Get it from [here](https://steamcommunity.com/dev/apikey).
-i - option to retrieve achievements images. Optional. Can take a while for some games.
-o [folder] - changes the output folder for the retriever. Optional. By default, it will save everything to the "steam" folder alongside the retriever.
```