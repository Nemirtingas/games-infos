# Games Infos

A simple collection of various information for your Steam/Epic Games library:

- Base Information
- Header image (currently not yet included)
- Achievements (including images) (currently not yet included)
- Items list (if the game has them)
- DLC parsing

## Where are the datas ?

Public datas are stored here: https://github.com/Nemirtingas/games-infos-datas

### Can't find your game/DLC?

Get the binaries ([releases](../../releases)), or fork the project and add some games yourself.
Easiest way to add the games is to extract the files from the artifact into the main folder and just run the tool from there.

#### How to use steam_retriever

```
steam_retriever [-u|--username user] [-p|--password password] [-k key|--apikey=key] [-i|--download_images] [-o folder|--out=folder] [-l language|--language=language] appid1 appid2 ...
   --help                 : Shows the help about the commands available.
-u|--username user        : The Steam user name.
-p|--password password    : The Steam user password.
-r|--remember-password    : Save password for future login.
-l|--language language    : Sets the output language (if available). Default value is english.
-k|--apikey key           : Steam WebAPI key. Optional. Get it from here: https://steamcommunity.com/dev/apikey. If no API key is given, it will only get games' infos and DLCs.
-i|--download_images      : Option to retrieve achievements images. Optional. Can take a while for some games.
-c|--controller-config    : Download all the controllers configurations.
-d|--dlcs                 : Do NOT retrieve dlcs when querying apps (Only match the appids in parameters).
-f|--force                : Force to download game's infos, ignoring cache (usefull if you want to refresh a game).
  |--cache-only           : Use the cached datas only, don't query Steam.
  |--cache-out steam_cache: Where to output the metadata cache.
-o|--out folder           : Changes the output folder for the retriever. Optional. By default, it will save everything to the "steam" folder in your current working directory.
appid1 appid2 ...         : Mandatory - A list of appid to get achievements, stats, DLCs and game infos. If unspecified, the app will try to retrieve data for all available Steam games.
```

#### How to use epic_retriever

```
epic_retriever [-i|--download-images] [-f|--force] [-o|--out epic] [-c|--cache-out epic_cache] [-w|--from-web] [--no-infos] [-N|--namespace] [-C|--catalog-item] [-U|--game-user] [-P|--game-password] [-D|--game-deployement-id] [-A|--game-app-id] [--games-credentials-directory]
-i|--download-images                        : Download images or not when retrieving applications details.
-f|--force                                  : Force re-downloading everything even if already in cache.
-o|--out epic                               : Where to save the applications details       (default: epic).
-c|--cache-out epic_cache                   : Where to save the applications details cache (default: epic_cache).
-w|--from-web                               : Try to deduce the game's catalog id by its web store page. (Not recommended, very few store pages have this)
   --no-infos                               : If you use -N,-C,-U,-P,-D,-A or --games-credentials-directory, will not retrieve the applications details but only the achievements.
-N|--namespace NAMESPACE                    : The game's namespace to dump achievements.
-C|--catalog-item CATALOG ID                : The game's catalog item id to dump achievements.
-U|--game-user GAME USER                    : The game's user to dump achievements.
-P|--game-password GAME PASSWORD            : The game's password to dump achievements.
-D|--game-deployement-id GAME DEPLOYEMENT ID: The game's deployement id to dump achievements.
-A|--game-app-id GAME APP ID                : The game's app id to dump achievements.
--games-credentials-directory credentials   : If specified, will not use any -N,-C,-U,-P,-D,-A, but rather will read all files containing a json with the game's credentials to dump achievements. (Usefull for batch dump).

Game's credentials json format:
{
  "AppId": "same content as -A",
  "EOS_AUDIENCE": "same content as -U",
  "EOS_SECRET_KEY": "same content as -P",
  "EOS_DEPLOYEMENT_ID": "same content as -D",
  "EOS_SANDBOX_ID": "same content as -N",
  "EOS_PRODUCT_ID": "Game's product id (not in the application details), can use the value Unreal"
}
```