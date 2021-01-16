# BlackPath
A game that uses Telegram API to work

# How to play
If you need help while using the bot - type `/help` in the chat with the bot.
If you haven't used the bot before you should type `/start` to launch it. 
Type `/game` to start a new game or to load an existing one.
The goal of the game is to reach the exit and not to loose all the HP.

# How to launch a project
1. Create a new bot using `@BotFather` - a Telegram bot for managing other bots - and save its token.
2. In the project folder create a file `Program.cs` with the following content (a temporary solution, should be changed to editing a JSON file or something):
```csharp
using System;
using System.Timers;
using Telegram.Bot;
using tgBot.Game;

namespace tgBot
{
    public class Program
    {
        internal const string Token = "your_bot_token";
        public static readonly TelegramBotClient botClient = new TelegramBotClient(Token);
        static void Main()
        {
            botClient.OnMessage += GameCore.BotClient_HandleMessage;
            botClient.StartReceiving(); 
            Console.Read();
        }
    }
}
```
3. Call the command prompt (for Windows - open Start menu and type `cmd`).
4. In the command prompt change directory to the project folder (where .csproj file is stored).
5. Type `dotnet run`.
6. Open the chat with the bot and type `/start`.
7. If the bot responds, then everything is done correctly.
