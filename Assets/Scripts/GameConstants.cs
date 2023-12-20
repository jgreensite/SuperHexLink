using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//todo - really should have private setters and public getters on each of these

[CreateAssetMenu(fileName = "GameConstants", menuName = "ScriptableObjects/GameConstants", order = 1)]
public class GameConstants : ScriptableObject
{
    // Hex Materials
    public Material forestMaterial;
    public Material pastureMaterial;
    public Material fieldMaterial;
    public Material hillMaterial;
    public Material mountainMaterial;
    public Material desertMaterial;
    public Material mineMaterial;
    public Material seaMaterial;
    public Material goldMaterial;
    public Material glowMaterial;

    // Define a mapping of HexType values to materials
    [ShowInInspector]
    public Dictionary<string, Material> materialMap { get; private set; }

    //Player Defaults
    public static string NO_CLIENT_ID = "No Client ID";
    
    //Message Types
    public static string MESSAGE_COMMAND = "COMMAND";
    public static string MESSAGE_REPLY = "REPLY";
    public static string MESSAGE_EVENT = "EVENT";
    
    //Message Names - Client
    public static string MESSAGE_CWHO = "CWHO";
    public static string MESSAGE_CBEG = "CBEG";
    public static string MESSAGE_CDIC = "CDIC";
    
    //Message Names - Server
    public static string SERVER_ID = "SERVER";
    public static string SERVER_NAME = "I_AM_THE_SERVER";
    public static string MESSAGE_SWHO = "SWHO";
    public static string MESSAGE_SCNN = "SCNN";
    public static string MESSAGE_SBEG = "SBEG";
    public static string MESSAGE_SDIC = "SDIC";

    //Names of Objects
    public static string OBJ_NAME_ROOT_CARD = "Card Root";

    //Layers
    public static string OBJ_LOCATION_LAYER_GAMEBOARD = "Hex";
    public static string OBJ_LOCATION_LAYER_GAMEMODEL = "Model";
    public static string OBJ_LOCATION_LAYER_GAMETEXT = "Text";
    public static string OBJ_LOCATION_LAYER_GAMESTRUCTURE = "Structure";
    public static string OBJ_LOCATION_LAYER_PLAYERHAND = "Hand";
    
    //Owners
    public static string OBJ_OWNER_PLAYER = "Player";
    public static string OBJ_OWNER_CALLER = "Caller";
    
    //Tag make the same as layers, note that these must be set in the editor
    public static string OBJ_LOCATION_TAG_GAMEBOARD = OBJ_LOCATION_LAYER_GAMEBOARD;
    public static string OBJ_LOCATION_TAG_PLAYERHAND = OBJ_LOCATION_LAYER_PLAYERHAND;

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

    //Number Token Colours
    public Color HIGHEST_PROBABILITY_COLOR;
    public Color LOWEST_PROBABILITY_COLOR;

    //Floating Menus
    public const float FLOATING_MENU_OFFSET = 0.05f;

    //Card Location
    public const string CAR_LOCATION_GAMEBOARD = "card is on the Gameboard";
    public const string CAR_LOCATION_RED_DECK = "card is in the Red Deck";
    public const string CAR_LOCATION_BLUE_DECK = "card is in the Blue Deck";
    public const string CAR_LOCATION_RED_HAND = "card is in the Red Hand";
    public const string CAR_LOCATION_BLUE_HAND = "card is in the Blue Hand";
    public const string CAR_LOCATION_DISCARD_DECK = "card is in the Discard Deck";

    //Card Status
    public static string CAR_REVEAL_HIDDEN = "card face is hidden";
    public static string CAR_REVEAL_SHOWN = "card face is shown";

    //Card when playable
    public static string CWP_PLAY_PLAYER_TURN = "on the players turn";
    public static string CWP_PLAY_ANY_TURN = "on any players turn";

    //Card effect playable
    //Effects
    public static string CEP_EFFECT_RANDOM_REVEAL_CARD = "randomly reveals a card on the board";
    public static string CEP_EFFECT_RANDOM_CHANGE_CARD = "randomly changes a card on the board";
    public static string CEP_EFFECT_RANDOM_REMOVE_CARD = "randomly removes a card from the board";

    //Affects
    public static string CEP_AFFECT_GAMEBOARD = "affects the game board";
    public static string CEP_AFFECT_OWN_DECK = "affects your deck";
    public static string CEP_AFFECT_OPPONENT_DECK = "affects your opponent's deck";
    public static string CEP_AFFECT_OWN_HAND = "affects your hand";
    public static string CEP_AFFECT_OPPONENT_HAND = "affects opponent's hand";

    //Number of Cards in player's hands
    public static int CSCARDHANDDIM = 3;

    //Gameboard dimensions

    //Game Winstates
    public static string NONEWIN = "nonewin";
    public static string BLUEWIN = "bluewin";
    public static string REDWIN = "redwin";
    
    //Card Class and Team staticants
    public static string NO_TEAM = "noteam";
    public static string BLUE_TEAM = "blue";
    public static string RED_TEAM = "red";
    public static string YELLOW_TEAM = "yellow";
    public static string ORANGE_TEAM = "orange";
    public static string GREEN_TEAM = "green";
    public static string BROWN_TEAM = "brown";
    public static int IDCARDFRONTMATERIAL = 1;
    public static int IDCARDBACKMATERIAL = 0;
    public static int NUMCARDMATERIALS = 4;

    //Turn staticants
    public static int TEP_NUM_DRAW = 1;
    
    //Flow Control staticants
    public static string GOOD = "good";
    public static string BAD = "bad";
    public static string EMPTY = "empty";
    public static string ERROR = "error";
    public static string CREATE = "create";
    public static string READ = "read";
    public static string UPDATE = "update";
    public static string DELETE = "delete";
    public static string END = "end";


    //UI Label staticants
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
    
    //Networking staticants
    public const string GAMESERVERLOCALADDRESS = "127.0.0.1";

    //TODO - Make Server and port selectable
    public const string GAMESERVERREMOTEADDRESS = "35.177.228.70";
    public const int GAMESERVERPORT = 6321;

    //Build script staticants
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
    
    private void OnEnable()
    {
            // Initialize materialMap
        materialMap = new Dictionary<string, Material>()
        {
            {CAR_TYPE_WORD_NULL, null},
            {CAR_TYPE_NONE, null},
            {CAR_TYPE_EMPTY, null},
            {CAR_TYPE_FOREST, forestMaterial},
            {CAR_TYPE_PASTURE, pastureMaterial},
            {CAR_TYPE_FIELD, fieldMaterial},
            {CAR_TYPE_HILL, hillMaterial},
            {CAR_TYPE_MOUNTAIN, mountainMaterial},
            {CAR_TYPE_MINE, mineMaterial},
            {CAR_TYPE_SEA, seaMaterial},
            {CAR_TYPE_HARBOUR, seaMaterial},
            {CAR_TYPE_DESERT, desertMaterial},
            {CAR_TYPE_GOLD, goldMaterial},
        };
    }
    private void Start()
    {

    }


}
