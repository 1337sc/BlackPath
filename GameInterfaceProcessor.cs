using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace tgBot
{
    public static class GameInterfaceProcessor
    {
        private const string MarkerRight = "Right";
        private const string MarkerLeft = "Left";
        private const string MarkerUp = "Up";
        private const string MarkerDown = "Down";
        private const string MarkerUpRight = "Up right";
        private const string MarkerDownRight = "Down right";
        private const string MarkerDownLeft = "Down left";
        private const string MarkerUpLeft = "Up left";
        private const string MarkerChangeMode = "Mode: ";
        private const string MarkerModpack = "Load modified resources";
        private const string MarkerStandardModpack = "Get default resources";
        private const string MarkerSetStandardModpack = "Delete modifications and reset resources";
        private const string MarkerBack = "Back";
        private const string MarkerAskDirection = "\"Where's the exit?\"";
        private const string MarkerTrade = "\"What do you have for sale?\"";
        private const string MarkerAskAround = "\"What about the neighbouring cells?\"";
        private const int FieldSizeSmall = 10;
        private const int FieldSizeMedium = 20;
        private const int FieldSizeLarge = 25;
        private const int FieldSizeExtraLarge = 50;
        private static readonly string MarkerSizeSmall = $"Small ({FieldSizeSmall}*{FieldSizeSmall})";
        private static readonly string MarkerSizeMedium = $"Medium ({FieldSizeMedium}*{FieldSizeMedium})";
        private static readonly string MarkerSizeLarge = $"Large ({FieldSizeLarge}*{FieldSizeLarge})";
        private static readonly string MarkerSizeExtraLarge = $"!Extra large ({FieldSizeExtraLarge}*{FieldSizeExtraLarge})!";
        private const string MarkerNewGame = "New game";
        private const string MarkerLoadGame = "Load saved game";
        private const double DiagonalThreshold = 11d / 18d; // a threshold for characters to tell a diagonal direction
        public static readonly Dictionary<string, string> commands = new Dictionary<string, string>()
        {
            ["hello"] = "Greet the bot :)",
            ["start"] = "Start the bot",
            ["help"] = "Get some help with commands",
            ["id"] = "Get your ID (advanced)",
            ["game"] = "Start a new game or load the previous one (if first, the existing game will be abandoned)",
            ["stop"] = "Save and stop the existing game (won't work if no game is started)",
            ["settings"] = "Change the way the game behaves, set up modpacks etc.",
            //["changeopen"] = "A cheat",
            ["pos"] = "Get your current position",
            ["map"] = "Get your map drawn again"
        };

        private enum AnswerModes
        {
            DirectionAnswer,
            NeighborhoodAnswer,
            TradeAnswer
        }
        internal static async void BotClient_HandleMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            Telegram.Bot.Types.Message msg = e.Message;
            string cmd;
            if (msg.Type == MessageType.Text)
            {
                cmd = msg.Text;
            }
            else
            {
                cmd = msg.Type.ToString();
            }
            var chatId = msg.Chat.Id;
            await Logger.Log(msg.Chat.Username + " : " + cmd);
            if (cmd[0].CompareTo('/') == 0) //if it is a command
            {
                await HandleCommand(msg, cmd, chatId);
            }
            else //if it is something different from a command
            {
                Player curPlayer = GameDataProcessor.GetActivePlayer(chatId);
                if (curPlayer != null) //if the user started a game
                {
                    await HandleGameStartupMessage(cmd, chatId, curPlayer);
                }
                else //if the user didn't start a game (entered settings)
                {
                    switch (msg.Type) //if the user sent a modifications pack
                    {
                        case MessageType.Document when GameDataProcessor.GetPlayerInDialogue(chatId) != null:
                            await HandleModPack(msg, chatId);
                            break;
                        default:
                            await HandleSettingsCommand(cmd, chatId);
                            break;
                    }
                }
            }
        }

        private static async Task HandleGameStartupMessage(string cmd, long chatId, Player curPlayer)
        {
            if (cmd == MarkerNewGame) //if the user has restarted the game
            {
                await GameDataProcessor.ResetPlayer(chatId);
                await AskForGameFieldSize(chatId);
            }
            else if (curPlayer.Field != null) //if the user had already started a game earlier
            {
                await HandleInGameCommands(cmd, chatId, curPlayer);
            }
            else //if the user hadn't started the game yet
            {
                HandleFieldSize(cmd, chatId, curPlayer);
            }
        }

        private static async Task HandleSettingsCommand(string cmd, long chatId)
        {
            switch (cmd)
            {
                default:
                    break;
                case MarkerBack: //the user changed his mind
                    await CheckAndSendAsync(chatId, "Hello again!", new ReplyKeyboardRemove());
                    GameDataProcessor.RemovePlayerInDialogue(chatId);
                    break;
                case MarkerModpack: //the user decided to install a modification
                    await CheckAndSendAsync(chatId, "Send a modification file named \"cells.csv\", " +
                        "\"items.csv\" or \"effects.csv\" depending on the resource you want to modify",
                        new ReplyKeyboardMarkup(new KeyboardButton(MarkerBack)));
                    GameDataProcessor.AddPlayerInDialogue(new Player(chatId));
                    break;
                case MarkerStandardModpack: //the user needs to be sent the pack of the default resources
                    await CheckAndSendAsync(chatId, "You will be sent the files with the " +
                        "default resources in csv format.", new ReplyKeyboardRemove());
                    await SendDefaultResources(chatId);
                    break;
                case MarkerSetStandardModpack: //the user wants to delete his modpacks
                    await CheckAndSendAsync(chatId, "Your resource packs will be set to " +
                        "default. If you've got modified packs, they'll be deleted.",
                        new ReplyKeyboardRemove());
                    GameDataProcessor.SetDefaultResources(chatId);
                    break;
            }
        }

        private static async Task HandleModPack(Telegram.Bot.Types.Message msg, long chatId)
        {
            var resource = await Program.botClient.GetFileAsync(msg.Document.FileId);
            if (!msg.Document.FileName.EndsWith(".csv")) //all the modpacks do need the extension ".csv"!
            {
                await CheckAndSendAsync(chatId, "Wrong extension! All BlackPath modpacks should have " +
                    ".csv extension");
            }
            GameDataProcessor.LoadCustomResources(chatId, resource.FilePath, msg.Document.FileName);
            if (GameDataProcessor.RemovePlayerInDialogue(chatId)) //we don't process the user any more
            {
                await CheckAndSendAsync(chatId, "Your modified data will be applied within " +
                    "the next game started."); //informing the user that the operation has gone OK
            }
        }

        private static void HandleFieldSize(string cmd, long chatId, Player curPlayer)
        {
            if (cmd == MarkerSizeSmall)
            {
                curPlayer.CreateField(FieldSizeSmall);
            }
            else if (cmd == MarkerSizeMedium)
            {
                curPlayer.CreateField(FieldSizeMedium);
            }
            else if (cmd == MarkerSizeLarge)
            {
                curPlayer.CreateField(FieldSizeLarge);
            }
            else if (cmd == MarkerSizeExtraLarge)
            {
                curPlayer.CreateField(FieldSizeExtraLarge);
            }
            AskForAction(chatId);
        }

        private static async Task HandleInGameCommands(string cmd, long chatId, Player curPlayer)
        {
            //a separate part for performing dialogues
            if (GameDataProcessor.GetPlayerInDialogue(chatId) != null)
            {
                switch (cmd)
                {
                    default:
                        break;
                    case MarkerAskDirection:
                        CharacterAnswer(curPlayer, AnswerModes.DirectionAnswer);
                        break;
                    case MarkerAskAround:
                        break;
                    case MarkerTrade:
                        break;
                    case MarkerBack:
                        GameDataProcessor.RemovePlayerInDialogue(chatId);
                        curPlayer.AskedDirection = false;
                        curPlayer.AskedNeighbourhood = false;
                        AskForAction(chatId);
                        break;
                }
            }
            else
            {
                switch (cmd)
                {
                    default:
                        break;
                    case MarkerLoadGame: //if the user wants to load his last saved game
                        GameDataProcessor.UpdatePlayerTimestamp(curPlayer);
                        AskForAction(chatId);
                        break;
                    case MarkerUp:
                        curPlayer.Move(0, -1);
                        break;
                    case MarkerUpRight:
                        curPlayer.Move(1, -1);
                        break;
                    case MarkerRight:
                        curPlayer.Move(1, 0);
                        break;
                    case MarkerDownRight:
                        curPlayer.Move(1, 1);
                        break;
                    case MarkerDown:
                        curPlayer.Move(0, 1);
                        break;
                    case MarkerDownLeft:
                        curPlayer.Move(-1, 1);
                        break;
                    case MarkerLeft:
                        curPlayer.Move(-1, 0);
                        break;
                    case MarkerUpLeft:
                        curPlayer.Move(-1, -1);
                        break;
                }
                if (cmd == MarkerChangeMode + curPlayer.Mode.ToString())
                {
                    curPlayer.Mode = (PlayerModes)Math.Abs((int)curPlayer.Mode - 1); //since there are only 2 modes
                                                                                     //available, we can change them
                                                                                     //with this magic
                    await CheckAndSendAsync(curPlayer.Id, $"I will {curPlayer.Mode} now");
                    AskForAction(curPlayer.Id, false);
                }
            }
        }

        private static async Task HandleCommand(Telegram.Bot.Types.Message msg, string cmd, long chatId)
        {
            Player curActivePlayer;
            switch (CheckForCommands(cmd))
            {
                default:
                    break;
                case "hello": //quick test 
                    await CheckAndSendAsync(chatId, "Hello, " + msg.From.FirstName +
                        " " + msg.From.LastName);
                    break;
                case "start": //when the bot has been just started
                    await CheckAndSendAsync(chatId, "Hello! I am the bot for " +
                        "the Black Path game! Type \"/help\" to learn about commands!");
                    break;
                case "help": //find out how to work with the bot
                    string res = "";
                    foreach (var c in commands)
                    {
                        res += "/" + c.Key + $" - {c.Value}\n";
                    }
                    await CheckAndSendAsync(chatId, res);
                    break;
                case "id": //find out what ID does the user have
                    await CheckAndSendAsync(chatId, chatId.ToString());
                    break;
                case "pos": //get your position, format: X Y
                    curActivePlayer = GameDataProcessor.GetActivePlayer(chatId);
                    await CheckAndSendAsync(chatId, curActivePlayer.X.ToString() + " " +
                        curActivePlayer.Y.ToString());
                    break;
                case "game": //starts the game itself
                    if (GameDataProcessor.GetActivePlayer(chatId) != null)
                    {
                        await CheckAndSendAsync(chatId, "You can't have two active games " +
                            "at one time! Type /stop to end the current session.");
                        break;
                    }
                    if (await GameDataProcessor.AddActivePlayer(chatId) == 0)
                    {
                        await CheckAndSendAsync(chatId, "Looks like you've already got " +
                            "a saved game. What'd you like to do?",
                            new ReplyKeyboardMarkup(new List<KeyboardButton> {
                                        new KeyboardButton(MarkerNewGame),
                                        new KeyboardButton(MarkerLoadGame)
                                }, oneTimeKeyboard: true));
                    }
                    else
                    {
                        await AskForGameFieldSize(chatId);
                    }
                    break;
                case "map":
                    curActivePlayer = GameDataProcessor.GetActivePlayer(chatId);
                    if (curActivePlayer != null)
                    {
                        await SendCurrentField(curActivePlayer);
                    }
                    else
                    {
                        await CheckAndSendAsync(chatId, "You are not in game!");
                    }
                    break;
                case "changeopen":
                    if (chatId != 458715080) break; //maybe I would be ought to leave this for the whole society
                    curActivePlayer = GameDataProcessor.GetActivePlayer(chatId);
                    if (curActivePlayer != null)
                    {
                        foreach (var c in curActivePlayer.Field)
                        {
                            c.Opened = !c.Opened;
                        }
                    }
                    break;
                case "stop": //stops the current game
                    curActivePlayer = GameDataProcessor.GetActivePlayer(chatId);
                    if (curActivePlayer == null)
                    {
                        await CheckAndSendAsync(chatId, "You have no active session.",
                            new ReplyKeyboardRemove());
                        break;
                    }
                    await CheckAndSendAsync(chatId, "Your session will be ended in few seconds...\n" +
                        "Don't type anything else!", new ReplyKeyboardRemove());
                    if (await GameDataProcessor.RemoveActivePlayer(chatId))
                    {
                        await CheckAndSendAsync(chatId, "Your session has been over! Type " +
                            "/game to start a game or load it.");
                    }
                    break;
                case "settings": //change the settings of the game, especially the modpacks
                    if (GameDataProcessor.GetActivePlayer(chatId) != null)
                    {
                        await CheckAndSendAsync(chatId, "You can only change settings " +
                            "when not in the game! Type /stop to end the current session.");
                        break;
                    }
                    await CheckAndSendAsync(chatId, $"{MarkerModpack}: change the modpack you are using or set a new one.\n" +
                        $"{MarkerStandardModpack}: get the modpack the game uses by default.\n" +
                        $"{MarkerSetStandardModpack}: set the used modpack to default. Warning! This operation will delete all your mods from the server!\n" +
                        $"{MarkerBack}: exit Settings menu");
                    var markup = new ReplyKeyboardMarkup(new KeyboardButton[]
                    {
                            new KeyboardButton(MarkerModpack),
                            new KeyboardButton(MarkerStandardModpack),
                            new KeyboardButton(MarkerSetStandardModpack),
                            new KeyboardButton(MarkerBack)
                    });
                    await CheckAndSendAsync(chatId, "What do you want to do?", markup);
                    break;
            }
        }

        private static async void CharacterAnswer(Player p, AnswerModes mode)
        {
            switch (mode)
            {
                case AnswerModes.DirectionAnswer:
                    if (!p.AskedDirection)
                    {
                        var (X, Y) = p.FindExitPosition();
                        int deltaX = p.X - X;
                        int deltaY = p.Y - Y;
                        //finding the ratio of deltas to decide the direction
                        double absTanY = Math.Abs(deltaX / (double)deltaY);
                        double absTanX = Math.Abs(deltaY / (double)deltaX); //the result will be double
                        await Logger.Log("Honest!");
                        if (!p.CheckHonesty())
                        {
                            await Logger.Log("Dishonest!");
                            var rnd = new Random();
                            //messing the values up so they won't tell the truth even accidently
                            deltaX += rnd.Next(1, p.Field.GetLength(0) - deltaX);
                            deltaY += rnd.Next(1, p.Field.GetLength(1) - deltaY);
                        }
                        string answer;
                        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
                        {
                            if (absTanY > DiagonalThreshold)
                            {
                                answer = deltaX > 0
                                    ? deltaY > 0 ? MarkerDownRight : MarkerDownLeft
                                    : deltaY > 0 ? MarkerUpRight : MarkerUpLeft;
                            }
                            else
                            {
                                answer = deltaX > 0 ? MarkerDown : MarkerUp;
                            }
                        }
                        else
                        {
                            if (absTanX > DiagonalThreshold)
                            {
                                answer = deltaY > 0
                                    ? deltaX > 0 ? MarkerDownRight : MarkerDownLeft
                                    : deltaX < 0 ? MarkerUpRight : MarkerUpLeft;
                            }
                            else
                            {
                                answer = deltaY > 0 ? MarkerDown : MarkerUp;
                            }
                        }
                        await CheckAndSendAsync(p.Id, $"The {p.Field[p.X, p.Y].Name} answers: " +
                            $"\"I've heard about a mysterious portal nearby. Go {answer.ToLower()}\"");
                    }
                    break;
                case AnswerModes.NeighborhoodAnswer:
                    break;
                case AnswerModes.TradeAnswer:
                    break;
                default:
                    break;
            }
        }

        static string CheckForCommands(string msg)
        {
            string comp = "";
            for (int i = 1; i < msg.Length; i++)
            {
                if (msg[i] == '@') break; //to ignore @BlackPathBot... as it is present in hints dropdown
                comp += msg[i];
            }
            return comp;
        }
        public static async Task CheckAndSendAsync(long chatId, string msg,
            IReplyMarkup keyboardMarkup = null)
        {
            try //to catch non-fatal exceptions connected with a specific user
            {
                await Program.botClient.SendTextMessageAsync(chatId, msg,
                    replyMarkup: keyboardMarkup);
            }
            catch (Exception ex)
            {
                await Logger.Log($"{chatId} caused an exception: {ex.Message}");
            }
        }
        public static async Task CheckAndSendAsync(long chatId, InputOnlineFile inputMedia, string photoCaption = null,
            IReplyMarkup keyboardMarkup = null) //reminds me about that meme about two Spider-Men
        {
            try
            {
                if (inputMedia.FileName.EndsWith(".png"))
                {
                    await Program.botClient.SendPhotoAsync(chatId, inputMedia, caption: photoCaption,
                        replyMarkup: keyboardMarkup);
                }
                else
                {
                    await Program.botClient.SendDocumentAsync(chatId, inputMedia,
                        replyMarkup: keyboardMarkup);
                }
            }
            catch (Exception ex)
            {
                await Logger.Log($"{chatId} caused an exception: {ex.Message}");
            }
        }
        private static async Task AskForGameFieldSize(long chatId)
        {
            var buttons = new List<KeyboardButton[]>()
            {
                new KeyboardButton[]
                {
                    new KeyboardButton(MarkerSizeSmall),
                    new KeyboardButton(MarkerSizeMedium),
                    new KeyboardButton(MarkerSizeLarge)
                },
                new KeyboardButton[]
                {
                    new KeyboardButton(MarkerSizeExtraLarge)
                }
            };
            ReplyKeyboardMarkup replyKeyboard = new ReplyKeyboardMarkup(buttons);
            await CheckAndSendAsync(chatId, "Choose the size of the game field: ", replyKeyboard);
        }
        /// <summary>
        /// If the player isn't being held by entities asks him where would he like to go.
        /// </summary>
        /// <param name="chatId">The player's id</param>
        /// <returns></returns>
        public static async void AskForAction(long chatId, bool sendField = true)
        {
            Player curPlayer = GameDataProcessor.GetActivePlayer(chatId);
            if (curPlayer != null)
            {
                List<KeyboardButton[]> buttons;
                if (sendField)
                    await SendCurrentField(curPlayer);
                if (GameDataProcessor.GetPlayerInDialogue(chatId) == null) // if the player didn't start a conversation
                {
                    buttons = new List<KeyboardButton[]>()
                    {
                        new KeyboardButton[] { MarkerUpLeft, MarkerUp, MarkerUpRight },
                        new KeyboardButton[] { MarkerLeft, MarkerChangeMode + curPlayer.Mode.ToString(),
                            MarkerRight },
                        new KeyboardButton[] { MarkerDownLeft, MarkerDown, MarkerDownRight }
                    };
                }
                else
                {
                    buttons = new List<KeyboardButton[]>()
                    {
                        new KeyboardButton[] { MarkerAskDirection, MarkerAskAround },
                        new KeyboardButton[] { MarkerTrade, MarkerBack }
                    };
                }
                ReplyKeyboardMarkup replyKeyboard = new ReplyKeyboardMarkup(buttons);
                await CheckAndSendAsync(chatId, "Choose the next action: ",
                replyKeyboard);
            }
        }
        internal static void AskForActionAdapter(Player p)
        {
            AskForAction(p.Id);
        }
        public static async Task SendDefaultResources(long chatId)
        {
            InputOnlineFile inputFile = null;
            using var fs = new FileStream("default-resources.zip", FileMode.Open);
            inputFile = new InputOnlineFile(fs, "default-resources.zip");
            await CheckAndSendAsync(chatId, inputFile);
        }
        /// <summary>
        /// Sends the picture of the player's field
        /// </summary>
        /// <param name="p">The player whose field is to be sent</param>
        /// <returns></returns>
        public static async Task SendCurrentField(Player p)
        {
            try
            {
                using var fs = await GameDataProcessor.SavePlayerField(p);
                InputOnlineFile inputFile = new InputOnlineFile(fs, $"{p.Id}-field.png");
                await CheckAndSendAsync(p.Id, inputFile, photoCaption:
                    $"{p.Field[p.X, p.Y].Name}: {p.Field[p.X, p.Y].Desc}\n" +
                    $"HP: {p.HP}\n" +
                    $"Money: {p.Money} UAH\n" +
                    $"Glances left: {p.GlanceCount}");
                GameDataProcessor.writeLock.Release();
            }
            catch (Exception ex)
            {
                await Logger.Log($"Exception during sending a field: {p.Id} caused an exception: {ex.Message}");
            }
        }
    }
}
