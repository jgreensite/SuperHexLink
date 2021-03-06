using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public class CS
    {
        //Player Defaults
        public const string NO_CLIENT_ID = "No Client ID";
        
        //Message Types
        public const string MESSAGE_COMMAND = "COMMAND";
        public const string MESSAGE_REPLY = "REPLY";
        public const string MESSAGE_EVENT = "EVENT";
        
        //Message Names - Client
        public const string MESSAGE_CWHO = "CWHO";
        public const string MESSAGE_CBEG = "CBEG";
        public const string MESSAGE_CDIC = "CDIC";
        
        //Message Names - Server
        public const string SERVER_ID = "SERVER";
        public const string SERVER_NAME = "I_AM_THE_SERVER";
        public const string MESSAGE_SWHO = "SWHO";
        public const string MESSAGE_SCNN = "SCNN";
        public const string MESSAGE_SBEG = "SBEG";
        public const string MESSAGE_SDIC = "SDIC";

        //Names of Objects
        public const string OBJ_NAME_ROOT_CARD = "Card Root";

        //Layers
        public const string OBJ_LOCATION_LAYER_GAMEBOARD = "Hex";
        public const string OBJ_LOCATION_LAYER_GAMEMODEL = "Model";
        public const string OBJ_LOCATION_LAYER_GAMETEXT = "Text";
        public const string OBJ_LOCATION_LAYER_GAMESTRUCTURE = "Structure";
        public const string OBJ_LOCATION_LAYER_PLAYERHAND = "Hand";
        
        //Owners
        public const string OBJ_OWNER_PLAYER = "Player";
        public const string OBJ_OWNER_CALLER = "Caller";
        
        //Tag make the same as layers, note that these must be set in the editor
        public const string OBJ_LOCATION_TAG_GAMEBOARD = OBJ_LOCATION_LAYER_GAMEBOARD;
        public const string OBJ_LOCATION_TAG_PLAYERHAND = OBJ_LOCATION_LAYER_PLAYERHAND;

        //Card Types
        public const string CAR_TYPE_FOREST = "forest";
        public const string CAR_TYPE_PASTURE = "pasture";
        public const string CAR_TYPE_FIELD = "field";
        public const string CAR_TYPE_HILL = "hill";
        public const string CAR_TYPE_MOUNTAIN = "mountain";
        public const string CAR_TYPE_MINE = "mine";
        public const string CAR_TYPE_GOLD = "gold";
        public const string CAR_TYPE_SEA = "sea";
        public const string CAR_TYPE_HARBOUR = "harbour";
        public const string CAR_TYPE_DESERT = "desert";
        public const string CAR_TYPE_NONE = "none";
        public const string CAR_TYPE_WORD_NULL = "null";
        public const string CAR_TYPE_EMPTY = "";

        //Sub Type Keywords
        public const string CAR_TYPE_SUB_KEYWORD = "resources";

        //Card SubTypes
        public const string CAR_TYPE_SUB_FOREST = "wood";
        public const string CAR_TYPE_SUB_PASTURE = "wool";
        public const string CAR_TYPE_SUB_FIELD = "wheat";
        public const string CAR_TYPE_SUB_HILL = "brick";
        public const string CAR_TYPE_SUB_MOUNTAIN = "ore";
        public const string CAR_TYPE_SUB_3_1 = "3_1";

        //Card Location
        public const string CAR_LOCATION_GAMEBOARD = "card is on the Gameboard";
        public const string CAR_LOCATION_RED_DECK = "card is in the Red Deck";
        public const string CAR_LOCATION_BLUE_DECK = "card is in the Blue Deck";
        public const string CAR_LOCATION_RED_HAND = "card is in the Red Hand";
        public const string CAR_LOCATION_BLUE_HAND = "card is in the Blue Hand";
        public const string CAR_LOCATION_DISCARD_DECK = "card is in the Discard Deck";

        //Card Status
        public const string CAR_REVEAL_HIDDEN = "card face is hidden";
        public const string CAR_REVEAL_SHOWN = "card face is shown";

        //Card when playable
        public const string CWP_PLAY_PLAYER_TURN = "on the players turn";
        public const string CWP_PLAY_ANY_TURN = "on any players turn";

        //Card effect playable
        //Effects
        public const string CEP_EFFECT_RANDOM_REVEAL_CARD = "randomly reveals a card on the board";
        public const string CEP_EFFECT_RANDOM_CHANGE_CARD = "randomly changes a card on the board";
        public const string CEP_EFFECT_RANDOM_REMOVE_CARD = "randomly removes a card from the board";

        //Affects
        public const string CEP_AFFECT_GAMEBOARD = "affects the game board";
        public const string CEP_AFFECT_OWN_DECK = "affects your deck";
        public const string CEP_AFFECT_OPPONENT_DECK = "affects your opponent's deck";
        public const string CEP_AFFECT_OWN_HAND = "affects your hand";
        public const string CEP_AFFECT_OPPONENT_HAND = "affects opponent's hand";

        //Number of Cards in player's hands
        public const int CSCARDHANDDIM = 3;

        //Gameboard dimensions

        //Game Winstates
        public const string NONEWIN = "nonewin";
        public const string BLUEWIN = "bluewin";
        public const string REDWIN = "redwin";
        
        //Card Class and Team constants
        public const string NO_TEAM = "noteam";
        public const string BLUE_TEAM = "blue";
        public const string RED_TEAM = "red";
        public const string YELLOW_TEAM = "yellow";
        public const string ORANGE_TEAM = "orange";
        public const string GREEN_TEAM = "green";
        public const string BROWN_TEAM = "brown";
        public const int IDCARDFRONTMATERIAL = 1;
        public const int IDCARDBACKMATERIAL = 0;
        public const int NUMCARDMATERIALS = 4;

        //Turn constants
        public const int TEP_NUM_DRAW = 1;
        
        //Flow Control constants
        public const string GOOD = "good";
        public const string BAD = "bad";
        public const string EMPTY = "empty";
        public const string ERROR = "error";
        public const string CREATE = "create";
        public const string READ = "read";
        public const string UPDATE = "update";
        public const string DELETE = "delete";
        public const string END = "end";


        //UI Label constants
        public const string UILABELPANZOOM = "Pan/Zoom";
        public const string UILABELSELECT = "Select";
        public const string UILABELBACK = "<-Back";
        public const string UILABELCANCEL = "Cancel";
        public const string UILABELSTART = "Start";
        public const string UILABELHOSTLOCAL = "Host Local";
        public const string UILABELHOSTREMOTE = "Host Remote";
        public const string UILABELCONNECT = "Connect";

        //Gamecard Offsets
        public const float CALLERCARDOFFSET = 12.35f;
        
        //Networking constants
        public const string GAMESERVERLOCALADDRESS = "127.0.0.1";

        //TODO - Make Server and port selectable
        public const string GAMESERVERREMOTEADDRESS = "35.177.228.70";
        public const int GAMESERVERPORT = 6321;

        //Build script constants
        public const string SERVERSCENECOLLECTION = "server";
        public const string CLIENTSCENECOLLECTION = "client";
        public const string OSXBUILDPLATFORM = "OSX";
        public const string UNXBUILDPLATFORM = "UNX";
        public const string WINBUILDPLATFORM = "WIN";
        public const string ANDBUILDPLATFORM = "AND";

        public const string IOSBUILDPLATFORM = "IOS";

        //Put all the Effects in here to make it is easy to ramdonly pick one
        //public static string[] CEP_EFFECTS = {CEP_EFFECT_RANDOM_REVEAL_CARD, CEP_EFFECT_RANDOM_CHANGE_CARD, CEP_EFFECT_RANDOM_REMOVE_CARD};
        public static string[] CEP_EFFECTS = {CEP_EFFECT_RANDOM_REVEAL_CARD, CEP_EFFECT_RANDOM_REVEAL_CARD, CEP_EFFECT_RANDOM_REVEAL_CARD};
    }
}