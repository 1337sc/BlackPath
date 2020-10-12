using CsvHelper;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tgBot
{
    static class GameDataProcessor
    {
        private const int DelayMinutes = 100;
        private const string DataFileExtension = ".bpdf";
        private const string ResourceFileExtension = ".csv";
        private const string DataFileFolder = "./userdata/";
        private static readonly List<Player> activePlayers = new List<Player>();
        private static readonly List<Player> inDialoguePlayers = new List<Player>();
        public static readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// Deserializes a player and, if everything's fine, adds it to active.
        /// </summary>
        /// <param name="id">Player's id</param>
        /// <returns>0 if a new player is added, 1 if the player has already registered recently.</returns>
        public static async Task<int> AddActivePlayer(long id)
        {
            int res = 0;
            Player p = await DeserializePlayer(id);
            if (p == null)
            {
                p = new Player(id);
                await SerializePlayer(p);
                res = 1;
            }
            if (!activePlayers.Contains(p))
            {
                activePlayers.Add(p);
            }
            // TODO: return exceptions instead of codes
            return res;
        }
        public static async Task<bool> RemoveActivePlayer(long id)
        {
            var p = activePlayers.Find(p => p.Id == id);
            await SerializePlayer(p);
            activePlayers.Remove(p);
            return true;
        }
        /// <summary>
        /// External method for serializing a player.
        /// </summary>
        /// <param name="player">The player to be serialized</param>
        private static async Task SerializePlayer(Player player)
        {
            Type playerType = player.GetType();
            try
            {
                CheckCreateDataFolder();
                using var fs = new FileStream($"{DataFileFolder}{player.Id}{DataFileExtension}", FileMode.Create);
                await player.SerializeTo(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }
        public static async Task SerializeValueOfType(Type type, FileStream fs, object value)
        {
            if (type == typeof(int))
            {
                await fs.WriteAsync(BitConverter.GetBytes((int)value));
            }
            if (type == typeof(long))
            {
                await fs.WriteAsync(BitConverter.GetBytes((long)value));
            }
            if (type == typeof(double))
            {
                await fs.WriteAsync(BitConverter.GetBytes((double)value));
            }
            if (type == typeof(bool))
            {
                await fs.WriteAsync(BitConverter.GetBytes((bool)value));
            }
            if (type == typeof(string))
            {
                await WriteStrToStream(fs, value.ToString());
            }
        }

        internal static async Task<object> DeserializeValueOfType(Type type, FileStream fs)
        {
            ConvertibleByteArray convertibleBuffer;
            if (type == typeof(int))
            {
                convertibleBuffer = new ConvertibleByteArray((byte[])Array.CreateInstance(
                   typeof(byte), sizeof(int)));
                await fs.ReadAsync(convertibleBuffer.Value);
                return Convert.ChangeType(convertibleBuffer, type);
            }
            if (type == typeof(long))
            {
                convertibleBuffer = new ConvertibleByteArray((byte[])Array.CreateInstance(
                   typeof(byte), sizeof(long)));
                await fs.ReadAsync(convertibleBuffer.Value);
                return Convert.ChangeType(convertibleBuffer, type);
            }
            if (type == typeof(double))
            {
                convertibleBuffer = new ConvertibleByteArray((byte[])Array.CreateInstance(
                   typeof(byte), sizeof(double)));
                await fs.ReadAsync(convertibleBuffer.Value);
                return Convert.ChangeType(convertibleBuffer, type);
            }
            if (type == typeof(bool))
            {
                convertibleBuffer = new ConvertibleByteArray((byte[])Array.CreateInstance(
                   typeof(byte), sizeof(bool)));
                await fs.ReadAsync(convertibleBuffer.Value);
                return Convert.ChangeType(convertibleBuffer, type);
            }
            if (type == typeof(string))
            {
                byte[] lenBuffer = new byte[sizeof(int)];
                await fs.ReadAsync(lenBuffer);
                int currentSrtLen = BitConverter.ToInt32(lenBuffer, 0);
                return await ReadStrFromStream(fs, currentSrtLen);
            }
            return new object();
        }
        /// <summary>
        /// Writes a length of a string and the string itself into a FileStream
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="param"></param>
        private static async Task WriteStrToStream(FileStream fs, string param)
        {
            await fs.WriteAsync(BitConverter.GetBytes(param.Length));
            await fs.WriteAsync(Encoding.UTF8.GetBytes(param));
        }

        /// <summary>
        /// External method for deserializing a player.
        /// </summary>
        /// <param name="id">Id of the player to be found.</param>
        /// <returns>The player with a specified id. Null, if no such players</returns>
        private static async Task<Player> DeserializePlayer(long id)
        {
            Player res = null;
            try
            {
                CheckCreateDataFolder();
                using var fs = new FileStream($"{DataFileFolder}{id}{DataFileExtension}", FileMode.Open);
                {
                    res = new Player(id);
                    await res.DeserializeFrom(fs);
                }
            }
            catch (FileNotFoundException)
            {
                res = null;
                Console.WriteLine($"{id} has not been registered yet or deleted its data");
            }
            catch (Exception ex)
            {
                res = null;
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            return res;
        }
        private static async Task<string> ReadStrFromStream(Stream fs, int length)
        {
            var bufferBytes = new byte[length];
            await fs.ReadAsync(bufferBytes, 0, length);
            return Encoding.UTF8.GetString(bufferBytes);
        }

        private static void CheckCreateDataFolder()
        {
            DirectoryInfo dataFolder = new DirectoryInfo(DataFileFolder);
            if (!dataFolder.Exists)
            {
                dataFolder.Create();
            }
        }

        /// <summary>
        /// Gets a player from active players list by id.
        /// </summary>
        /// <param name="id">Player's id</param>
        /// <returns>The player with a specified id. Null, if no such players</returns>
        public static Player GetActivePlayer(long id)
        {
            return activePlayers.Find(p => p.Id == id);
        }
        /// <summary>
        /// Gets a player from processed players list by id.
        /// </summary>
        /// <param name="id">Player's id</param>
        /// <returns>The player with a specified id. Null, if no such players</returns>
        public static Player GetPlayerInDialogue(long id)
        {
            return inDialoguePlayers.Find(p => p.Id == id);
        }
        public static void AddPlayerInDialogue(Player p)
        {
            inDialoguePlayers.Add(p);
        }
        public static bool RemovePlayerInDialogue(long id)
        {
            return inDialoguePlayers.RemoveAll(x => x.Id == id) > 0;
        }
        /// <summary>
        /// Resets the player's field and other stats. Used if a player started a new game.
        /// </summary>
        /// <param name="id">The player's id</param>
        public static async Task<bool> ResetPlayer(long id)
        {
            Player p = new Player(id);
            if (!await RemoveActivePlayer(id))
            {
                return false;
            }
            await SerializePlayer(p);
            await AddActivePlayer(id);
            return true;
        }
        public static void UpdatePlayerTimestamp(Player p)
        {
            p.TimeStamp = DateTime.Now;
        }
        /// <summary>
        /// Checks for modifications and, if the player uses no ones, loads default resources
        /// </summary>
        /// <param name="p"></param>
        public static async void GetPlayerResources(Player p)
        {
            FileInfo curResFile = new FileInfo($"{DataFileFolder}{p.Id}-cells{ResourceFileExtension}");
            StreamReader reader = null;
            if (curResFile.Exists)
            {
                reader = new StreamReader(curResFile.FullName);
            }
            else
            {
                try
                {
                    reader = new StreamReader($"{DataFileFolder}default-cells{ResourceFileExtension}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not read default resources: {ex.Message + ex.StackTrace}");
                }
            }
            using (reader)
            {
                using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                try
                {
                    csvReader.Read();
                    csvReader.ReadHeader();
                    p.CellsList = new List<Cell>();
                    while (csvReader.Read())
                    {
                        Cell newCell = new Cell()
                        {
                            Name = csvReader.GetField("Name"),
                            Type = Cell.CellTypesDict.TryGetValue(csvReader.GetField("Type"), out Cell.CellTypes type) ? type : Cell.CellTypes.ErrType,
                            Colour = csvReader.GetField("Colour"),
                            Figure = Cell.FiguresDict.TryGetValue(csvReader.GetField("Figure"), out Cell.Figures fig) ? fig : Cell.Figures.None,
                            FigureColour = csvReader.GetField("FigureColour"),
                            Fill = csvReader.GetField<bool>("Fill"),
                            HasDialogue = csvReader.GetField<bool>("Dialogue"),
                            Effect = csvReader.GetField("Effect"),
                            Desc = csvReader.GetField("Desc")
                        };
                        newCell.Opened = newCell.Type == Cell.CellTypes.Player ? true : false;
                        newCell.SetCellActions();
                        if (newCell.Type != Cell.CellTypes.ErrType)
                            p.CellsList.Add(newCell);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not read resources: {ex.Message + ex.StackTrace}");
                    await GameInterfaceProcessor.CheckAndSendAsync(p.Id,
                        "A problem has occured during the reading of your modification pack(s). Try setting it to default.");
                }
            }
            curResFile = new FileInfo($"{DataFileFolder}{p.Id}-items{ResourceFileExtension}");
            if (curResFile.Exists)
            {
                reader = new StreamReader(curResFile.FullName);
            }
            else
            {
                try
                {
                    reader = new StreamReader($"{DataFileFolder}default-items{ResourceFileExtension}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not read default resources: {ex.Message + ex.StackTrace}");
                }
            }
            reader.Close();
            //TODO: read items & effects when they are developed o_o
        }
        public static void SetDefaultResources(long chatId)
        {
            var curFileInfo = new FileInfo($"{DataFileFolder}{chatId}-cells{ResourceFileExtension}");
            if (curFileInfo.Exists)
            {
                curFileInfo.Delete();
            }
            curFileInfo = new FileInfo($"{chatId}-items{ResourceFileExtension}");
            if (curFileInfo.Exists)
            {
                curFileInfo.Delete();
            }
            curFileInfo = new FileInfo($"{chatId}-effects{ResourceFileExtension}");
            if (curFileInfo.Exists)
            {
                curFileInfo.Delete();
            }
        }
        public static void LoadCustomResources(long chatId, string filePath, string fileName)
        {
            string resourceType = "";
            if (fileName.Contains("cells"))
            {
                resourceType = "cells";
            }
            else if (fileName.Contains("items"))
            {
                resourceType = "items";
            }
            else if (fileName.Contains("effects"))
            {
                resourceType = "effects";
            }
            using var client = new WebClient();
            client.DownloadFile($"https://api.telegram.org/file/bot{Program.Token}/{filePath}", $"{DataFileFolder}{chatId}-{resourceType}{ResourceFileExtension}");
        }
        /// <summary>
        /// Saves the picture of the player's field and returns the FileStream containing the image.
        /// The returned FileStream needs to be disposed.
        /// </summary>
        /// <param name="p">The player whose field is to be saved</param>
        /// <returns></returns>
        public static async Task<Stream> SavePlayerField(Player p)
        {
            await writeLock.WaitAsync();
            using (var createFs = new FileStream($"{DataFileFolder}{p.Id}-field.png", FileMode.Create))
            {
                await Task.Run(new Action(() => p.DrawField().Save(createFs, ImageFormat.Png)));
            }
            var openFs = new FileStream($"{DataFileFolder}{p.Id}-field.png", FileMode.Open, FileAccess.Read);
            return openFs;
        }
    }
}
