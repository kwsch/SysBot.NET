using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Pokemon.Discord.Commands.Bots;
using System.Collections.Generic;
using System;
using System.Drawing;
using Color = System.Drawing.Color;
using DiscordColor = Discord.Color;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using SysBot.Pokemon.Helpers;
using PKHeX.Core.AutoMod;
using PKHeX.Drawing.PokeSprite;

namespace SysBot.Pokemon.Discord;

public static class QueueHelper<T> where T : PKM, new()
{
    private const uint MaxTradeCode = 9999_9999;

    // A dictionary to hold batch trade file paths and their deletion status
    private static readonly Dictionary<int, List<string>> batchTradeFiles = [];
    private static readonly Dictionary<ulong, int> userBatchTradeMaxDetailId = [];

    public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser trader, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, int formArgument = 0, bool isMysteryEgg = false, List<Pictocodes> lgcode = null)
    {
        if ((uint)code > MaxTradeCode)
        {
            await context.Channel.SendMessageAsync("Trade code should be 00000000-99999999!").ConfigureAwait(false);
            return;
        }

        try
        {
            if (!isBatchTrade || batchTradeNumber == 1)
            {
                if (trade is PB7 && lgcode != null)
                {
                    var (thefile, lgcodeembed) = CreateLGLinkCodeSpriteEmbed(lgcode);
                    await trader.SendFileAsync(thefile, $"Your trade code will be.", embed: lgcodeembed).ConfigureAwait(false);
                }
                else
                {
                    await trader.SendMessageAsync($"Your trade code will be: **{code:0000 0000}**.\nI will DM you when your trade is about to start.").ConfigureAwait(false);
                }
            }

            var result = await AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, isBatchTrade, batchTradeNumber, totalBatchTrades, formArgument, isMysteryEgg, lgcode).ConfigureAwait(false);

        }
        catch (HttpException ex)
        {
            await HandleDiscordExceptionAsync(context, trader, ex).ConfigureAwait(false);
        }
    }

    public static Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type)
    {
        return AddToQueueAsync(context, code, trainer, sig, trade, routine, type, context.User);
    }

    private static async Task<TradeQueueResult> AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName, RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, bool isBatchTrade, int batchTradeNumber, int totalBatchTrades, int formArgument = 0, bool isMysteryEgg = false, List<Pictocodes> lgcode = null)
    {
        var user = trader;
        var userID = user.Id;
        var name = user.Username;

        var trainer = new PokeTradeTrainerInfo(trainerName, userID);
        var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, trader, batchTradeNumber, totalBatchTrades, isMysteryEgg, lgcode);
        var detail = new PokeTradeDetail<T>(pk, trainer, notifier, t, code, sig == RequestSignificance.Favored, lgcode, batchTradeNumber, totalBatchTrades, isMysteryEgg);
        var trade = new TradeEntry<T>(detail, userID, type, name);
        var strings = GameInfo.GetStrings(1);
        var hub = SysCord<T>.Runner.Hub;
        var Info = hub.Queues.Info;
        var canAddMultiple = isBatchTrade || sig == RequestSignificance.Owner;
        var added = Info.AddToTradeQueue(trade, userID, canAddMultiple);

        if (added == QueueResultAdd.AlreadyInQueue)
        {
            return new TradeQueueResult(false);
        }

        var position = Info.CheckPosition(userID, type);
        var botct = Info.Hub.Bots.Count;
        var etaMessage = "";
        if (position.Position > botct)
        {
            var baseEta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
            // Increment ETA by 1 minute for each batch trade
            var adjustedEta = baseEta + (batchTradeNumber - 1);
            etaMessage = $"Estimated: {adjustedEta:F1} min(s) for trade {batchTradeNumber}/{totalBatchTrades}.";
        }
        else
        {
            var adjustedEta = (batchTradeNumber - 1); // Add 1 minute for each subsequent batch trade
            etaMessage = $"Estimated: {adjustedEta:F1} min(s) for trade {batchTradeNumber}/{totalBatchTrades}.";
        }

        // Format IVs for display
        int[] ivs = pk.IVs;
        string ivsDisplay = $"{ivs[0]}/{ivs[1]}/{ivs[2]}/{ivs[3]}/{ivs[4]}/{ivs[5]}";

        // Fetch the Pokémon's moves and their PP
        ushort[] moves = new ushort[4];
        pk.GetMoves(moves.AsSpan());
        int[] movePPs = { pk.Move1_PP, pk.Move2_PP, pk.Move3_PP, pk.Move4_PP };
        List<string> moveNames = [""];

        for (int i = 0; i < moves.Length; i++)
        {
            ushort moveId = moves[i];
            if (moveId == 0) continue; // Skip if no move is assigned to this slot
            string moveName = GameInfo.MoveDataSource.FirstOrDefault(m => m.Value == moveId)?.Text ?? "";
            moveNames.Add($"- {moveName} ({movePPs[i]}pp)");
        }
        string movesDisplay = string.Join("\n", moveNames);
        string abilityName = GameInfo.AbilityDataSource.FirstOrDefault(a => a.Value == pk.Ability)?.Text ?? "";
        string natureName = GameInfo.NatureDataSource.FirstOrDefault(n => n.Value == pk.Nature)?.Text ?? "";
        string teraTypeString = "";
        string scaleText = ""; 
        byte scaleNumber = 0; 

        if (pk is PK9 pk9)
        {
            teraTypeString = pk9.TeraTypeOverride == (MoveType)99 ? "Stellar" : pk9.TeraType.ToString();
            scaleText = $"{PokeSizeDetailedUtil.GetSizeRating(pk9.Scale)}"; 
            scaleNumber = pk9.Scale; 
        }
        int level = pk.CurrentLevel;
        string speciesName = GameInfo.GetStrings(1).Species[pk.Species];
        string formName = ShowdownParsing.GetStringFromForm(pk.Form, strings, pk.Species, pk.Context);
        string speciesAndForm = $"{speciesName}{(string.IsNullOrEmpty(formName) ? "" : $"-{formName}")}";
        string heldItemName = strings.itemlist[pk.HeldItem];
        string ballName = strings.balllist[pk.Ball];

        string formDecoration = "";
        if (pk.Species == (int)Species.Alcremie && formArgument != 0)
        {
            formDecoration = $"{(AlcremieDecoration)formArgument}";
        }

        // Determine if this is a clone or dump request
        bool isCloneRequest = type == PokeRoutineType.Clone;
        bool isDumpRequest = type == PokeRoutineType.Dump;
        bool FixOT = type == PokeRoutineType.FixOT;
        bool isSpecialRequest = type == PokeRoutineType.SeedCheck;

        // Check if the Pokémon is shiny and prepend the shiny emoji
        string shinyEmoji = pk.IsShiny ? "✨ " : "";
        string pokemonDisplayName = pk.IsNicknamed ? pk.Nickname : GameInfo.GetStrings(1).Species[pk.Species];
        string tradeTitle;

        if (isMysteryEgg)
        {
            tradeTitle = "✨ Shiny Mystery Egg ✨";
        }
        else if (isBatchTrade)
        {
            tradeTitle = $"Batch Trade #{batchTradeNumber} - {shinyEmoji}{pokemonDisplayName}";
        }
        else if (FixOT)
        {
            tradeTitle = $"FixOT Request";
        }
        else if (isSpecialRequest)
        {
            tradeTitle = $"Special Request";
        }
        else if (isCloneRequest)
        {
            tradeTitle = "Clone Pod Activated!";
        }
        else if (isDumpRequest)
        {
            tradeTitle = "Pokémon Dump";
        }
        else
        {
            tradeTitle = $"";
        }

        // Get the Pokémon's image URL and dominant color
        (string embedImageUrl, DiscordColor embedColor) = await PrepareEmbedDetails(context, pk, isCloneRequest || isDumpRequest, formName, formArgument);

        // Adjust the image URL for dump request
        if (isMysteryEgg)
        {
            embedImageUrl = "https://raw.githubusercontent.com/bdawg1989/sprites/main/mysteryegg2.png"; // URL for mystery egg
        }
        else if (isDumpRequest)
        {
            embedImageUrl = "https://raw.githubusercontent.com/bdawg1989/sprites/main/AltBallImg/128x128/dumpball.png"; // URL for dump request
        }
        else if (isCloneRequest)
        {
            embedImageUrl = "https://raw.githubusercontent.com/bdawg1989/sprites/main/clonepod.png"; // URL for clone request
        }
        else if (isSpecialRequest)
        {
            embedImageUrl = "https://raw.githubusercontent.com/bdawg1989/sprites/main/specialrequest.png"; // URL for special request
        }
        else if (FixOT)
        {
            embedImageUrl = "https://raw.githubusercontent.com/bdawg1989/sprites/main/AltBallImg/128x128/rocketball.png"; // URL for fixot request
        }
        string heldItemUrl = string.Empty;

        if (!string.IsNullOrWhiteSpace(heldItemName))
        {
            // Convert to lowercase and remove spaces
            heldItemName = heldItemName.ToLower().Replace(" ", "");
            heldItemUrl = $"https://serebii.net/itemdex/sprites/{heldItemName}.png";
        }
        // Check if the image URL is a local file path
        bool isLocalFile = File.Exists(embedImageUrl);
        string userName = user.Username;
        string isPkmShiny = pk.IsShiny ? "✨" : "";
        // Build the embed with the author title image
        string authorName;
        if (isMysteryEgg || FixOT || isCloneRequest || isDumpRequest || isSpecialRequest || isBatchTrade)
        {
            authorName = $"{userName}'s {tradeTitle}";
        }
        else // Normal trade
        {
            authorName = $"{userName}'s {isPkmShiny} {pokemonDisplayName}";
        }
        var embedBuilder = new EmbedBuilder()
            .WithColor(embedColor)
            .WithImageUrl(embedImageUrl)
            .WithFooter($"Current Position: {position.Position}\n{etaMessage}")
            .WithAuthor(new EmbedAuthorBuilder()
                .WithName(authorName)
                .WithIconUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithUrl("https://genpkm.com"));

        // Add the additional text at the top as its own field
        string additionalText = string.Join("\n", SysCordSettings.Settings.AdditionalEmbedText);
        if (!string.IsNullOrEmpty(additionalText))
        {
            embedBuilder.AddField("\u200B", additionalText, inline: false); 
        }

        if (!isMysteryEgg && !isCloneRequest && !isDumpRequest && !FixOT && !isSpecialRequest)
        {
            // Prepare the left side content
            string leftSideContent = $"**Trainer**: {user.Mention}\n" +
                                     $"**Species**: {speciesAndForm}\n";
            GameVersion gameVersion = (GameVersion)pk.Version;
            if (gameVersion == GameVersion.SL || gameVersion == GameVersion.VL)
            {
                leftSideContent += $"**TeraType**: {teraTypeString}\n" +
                    $"**Scale**: {scaleText} ({scaleNumber})\n";
            }
            leftSideContent += $"**Level**: {level}\n" +
                               $"**Ability**: {abilityName}\n" +
                               $"**Nature**: {natureName}\n" +
                               $"**IVs**: {ivsDisplay}";
            embedBuilder.AddField("**__Info__**", leftSideContent, inline: true);
            embedBuilder.AddField("\u200B", "\u200B", inline: true); 
            embedBuilder.AddField("**__Moves__**", movesDisplay, inline: true);
        }
        else
        {
            string specialDescription = $"**Trainer**: {user.Mention}\n" +
                                        (isMysteryEgg ? "Mystery Egg" : isSpecialRequest ? "Special Request" : isCloneRequest ? "Clone Request" : FixOT ? "FixOT Request" : "Dump Request");
            embedBuilder.AddField("\u200B", specialDescription, inline: false);
        }

        if (isCloneRequest || isSpecialRequest)
        {
            embedBuilder.WithThumbnailUrl("https://raw.githubusercontent.com/bdawg1989/sprites/main/profoak.png");
        }
        else if (!string.IsNullOrEmpty(heldItemUrl))
        {
            embedBuilder.WithThumbnailUrl(heldItemUrl);
        }
        if (isLocalFile)
        {
            embedBuilder.WithImageUrl($"attachment://{Path.GetFileName(embedImageUrl)}");
        }

        var embed = embedBuilder.Build();
        if (embed == null)
        {
            Console.WriteLine("Error: Embed is null.");
            await context.Channel.SendMessageAsync("An error occurred while preparing the trade details.");
            return new TradeQueueResult(false);
        }

        if (isLocalFile)
        {
            await context.Channel.SendFileAsync(embedImageUrl, embed: embed);

            if (isBatchTrade)
            {
                if (!userBatchTradeMaxDetailId.ContainsKey(userID) || userBatchTradeMaxDetailId[userID] < detail.ID)
                {
                    userBatchTradeMaxDetailId[userID] = detail.ID;
                }
                await ScheduleFileDeletion(embedImageUrl, 0, detail.ID);
                if (detail.ID == userBatchTradeMaxDetailId[userID] && batchTradeNumber == totalBatchTrades)
                {
                    DeleteBatchTradeFiles(detail.ID);
                }
            }
            else
            {
                await ScheduleFileDeletion(embedImageUrl, 0);
            }
        }
        else
        {
            await context.Channel.SendMessageAsync(embed: embed);
        }

        return new TradeQueueResult(true);
    }

    private static string GetImageFolderPath()
    {
        // Get the base directory where the executable is located
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Define the path for the images subfolder
        string imagesFolder = Path.Combine(baseDirectory, "Images");

        // Check if the folder exists, if not, create it
        if (!Directory.Exists(imagesFolder))
        {
            Directory.CreateDirectory(imagesFolder);
        }

        return imagesFolder;
    }

    private static string SaveImageLocally(System.Drawing.Image image)
    {
        // Get the path to the images folder
        string imagesFolderPath = GetImageFolderPath();

        // Create a unique filename for the image
        string filePath = Path.Combine(imagesFolderPath, $"image_{Guid.NewGuid()}.png");

        // Save the image to the specified path
        image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

        return filePath;
    }

    private static async Task<(string, DiscordColor)> PrepareEmbedDetails(SocketCommandContext context, T pk, bool isCloneRequest, string formName, int formArgument = 0)
    {
        string embedImageUrl;
        string speciesImageUrl;

        if (pk.IsEgg)
        {
            string eggImageUrl = "https://raw.githubusercontent.com/bdawg1989/sprites/main/egg.png";
            speciesImageUrl = AbstractTrade<T>.PokeImg(pk, false, true);
            System.Drawing.Image combinedImage = await OverlaySpeciesOnEgg(eggImageUrl, speciesImageUrl);
            embedImageUrl = SaveImageLocally(combinedImage);
        }
        else
        {
            bool canGmax = pk is PK8 pk8 && pk8.CanGigantamax;
            speciesImageUrl = AbstractTrade<T>.PokeImg(pk, canGmax, false);
            embedImageUrl = speciesImageUrl;
        }

        // Determine ball image URL
        var strings = GameInfo.GetStrings(1);
        string ballName = strings.balllist[pk.Ball];

        // Check for "(LA)" in the ball name
        if (ballName.Contains("(LA)"))
        {
            ballName = "la" + ballName.Replace(" ", "").Replace("(LA)", "").ToLower();
        }
        else
        {
            ballName = ballName.Replace(" ", "").ToLower();
        }

        string ballImgUrl = $"https://raw.githubusercontent.com/bdawg1989/sprites/main/AltBallImg/28x28/{ballName}.png";

        // Check if embedImageUrl is a local file or a web URL
        if (Uri.TryCreate(embedImageUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeFile)
        {
            // Load local image directly
            using (var localImage = System.Drawing.Image.FromFile(uri.LocalPath))
            using (var ballImage = await LoadImageFromUrl(ballImgUrl))
            {
                if (ballImage != null)
                {
                    using (var graphics = Graphics.FromImage(localImage))
                    {
                        var ballPosition = new Point(localImage.Width - ballImage.Width, localImage.Height - ballImage.Height);
                        graphics.DrawImage(ballImage, ballPosition);
                    }
                    embedImageUrl = SaveImageLocally(localImage);
                }
            }
        }
        else
        {
            // Load web image and overlay ball
            (System.Drawing.Image finalCombinedImage, bool ballImageLoaded) = await OverlayBallOnSpecies(speciesImageUrl, ballImgUrl);
            embedImageUrl = SaveImageLocally(finalCombinedImage);

            if (!ballImageLoaded)
            {
                Console.WriteLine($"Ball image could not be loaded: {ballImgUrl}");
               // await context.Channel.SendMessageAsync($"Ball image could not be loaded: {ballImgUrl}");
            }
        }

        (int R, int G, int B) = await GetDominantColorAsync(embedImageUrl);
        return (embedImageUrl, new DiscordColor(R, G, B));
    }

    private static async Task<(System.Drawing.Image, bool)> OverlayBallOnSpecies(string speciesImageUrl, string ballImageUrl)
    {
        using (var speciesImage = await LoadImageFromUrl(speciesImageUrl))
        {
            if (speciesImage == null)
            {
                Console.WriteLine("Species image could not be loaded.");
                return (null, false);
            }

            var ballImage = await LoadImageFromUrl(ballImageUrl);
            if (ballImage == null)
            {
                Console.WriteLine($"Ball image could not be loaded: {ballImageUrl}");
                return ((System.Drawing.Image)speciesImage.Clone(), false); // Return false indicating failure
            }

            using (ballImage)
            {
                using (var graphics = Graphics.FromImage(speciesImage))
                {
                    var ballPosition = new Point(speciesImage.Width - ballImage.Width, speciesImage.Height - ballImage.Height);
                    graphics.DrawImage(ballImage, ballPosition);
                }

                return ((System.Drawing.Image)speciesImage.Clone(), true); // Return true indicating success
            }
        }
    }
    private static async Task<System.Drawing.Image> OverlaySpeciesOnEgg(string eggImageUrl, string speciesImageUrl)
    {
        // Load both images
        System.Drawing.Image eggImage = await LoadImageFromUrl(eggImageUrl);
        System.Drawing.Image speciesImage = await LoadImageFromUrl(speciesImageUrl);

        // Calculate the ratio to scale the species image to fit within the egg image size
        double scaleRatio = Math.Min((double)eggImage.Width / speciesImage.Width, (double)eggImage.Height / speciesImage.Height);

        // Create a new size for the species image, ensuring it does not exceed the egg dimensions
        Size newSize = new Size((int)(speciesImage.Width * scaleRatio), (int)(speciesImage.Height * scaleRatio));

        // Resize species image
        System.Drawing.Image resizedSpeciesImage = new Bitmap(speciesImage, newSize);

        // Create a graphics object for the egg image
        using (Graphics g = Graphics.FromImage(eggImage))
        {
            // Calculate the position to center the species image on the egg image
            int speciesX = (eggImage.Width - resizedSpeciesImage.Width) / 2;
            int speciesY = (eggImage.Height - resizedSpeciesImage.Height) / 2;

            // Draw the resized and centered species image over the egg image
            g.DrawImage(resizedSpeciesImage, speciesX, speciesY, resizedSpeciesImage.Width, resizedSpeciesImage.Height);
        }

        // Dispose of the species image and the resized species image if they're no longer needed
        speciesImage.Dispose();
        resizedSpeciesImage.Dispose();

        // Calculate scale factor for resizing while maintaining aspect ratio
        double scale = Math.Min(128.0 / eggImage.Width, 128.0 / eggImage.Height);

        // Calculate new dimensions
        int newWidth = (int)(eggImage.Width * scale);
        int newHeight = (int)(eggImage.Height * scale);

        // Create a new 128x128 bitmap
        Bitmap finalImage = new Bitmap(128, 128);

        // Draw the resized egg image onto the new bitmap, centered
        using (Graphics g = Graphics.FromImage(finalImage))
        {
            // Calculate centering position
            int x = (128 - newWidth) / 2;
            int y = (128 - newHeight) / 2;

            // Draw the image
            g.DrawImage(eggImage, x, y, newWidth, newHeight);
        }

        // Dispose of the original egg image if it's no longer needed
        eggImage.Dispose();

        // The finalImage now contains the overlay, is resized, and maintains aspect ratio
        return finalImage;
    }

    private static async Task<System.Drawing.Image> LoadImageFromUrl(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to load image from {url}. Status code: {response.StatusCode}");
                return null;
            }

            Stream stream = await response.Content.ReadAsStreamAsync();
            if (stream == null || stream.Length == 0)
            {
                Console.WriteLine($"No data or empty stream received from {url}");
                return null;
            }

            try
            {
                return System.Drawing.Image.FromStream(stream);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Failed to create image from stream. URL: {url}, Exception: {ex}");
                return null;
            }
        }
    }

    private static async Task ScheduleFileDeletion(string filePath, int delayInMilliseconds, int batchTradeId = -1)
    {
        if (batchTradeId != -1)
        {
            // If this is part of a batch trade, add the file path to the dictionary
            if (!batchTradeFiles.ContainsKey(batchTradeId))
            {
                batchTradeFiles[batchTradeId] = new List<string>();
            }

            batchTradeFiles[batchTradeId].Add(filePath);
        }
        else
        {
            // If this is not part of a batch trade, delete the file after the delay
            await Task.Delay(delayInMilliseconds);
            DeleteFile(filePath);
        }
    }

    private static void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }
    }

    // Call this method after the last trade in a batch is completed
    private static void DeleteBatchTradeFiles(int batchTradeId)
    {
        if (batchTradeFiles.TryGetValue(batchTradeId, out var files))
        {
            foreach (var filePath in files)
            {
                DeleteFile(filePath);
            }
            batchTradeFiles.Remove(batchTradeId);
        }
    }

    public enum AlcremieDecoration
    {
        Strawberry = 0,
        Berry = 1,
        Love = 2,
        Star = 3,
        Clover = 4,
        Flower = 5,
        Ribbon = 6,
    }

    public static async Task<(int R, int G, int B)> GetDominantColorAsync(string imagePath)
    {
        try
        {
            Bitmap image = await LoadImageAsync(imagePath);

            var colorCount = new Dictionary<Color, int>();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixelColor = image.GetPixel(x, y);

                    if (pixelColor.A < 128 || pixelColor.GetBrightness() > 0.9) continue;

                    var brightnessFactor = (int)(pixelColor.GetBrightness() * 100);
                    var saturationFactor = (int)(pixelColor.GetSaturation() * 100);
                    var combinedFactor = brightnessFactor + saturationFactor;

                    var quantizedColor = Color.FromArgb(
                        pixelColor.R / 10 * 10,
                        pixelColor.G / 10 * 10,
                        pixelColor.B / 10 * 10
                    );

                    if (colorCount.ContainsKey(quantizedColor))
                    {
                        colorCount[quantizedColor] += combinedFactor;
                    }
                    else
                    {
                        colorCount[quantizedColor] = combinedFactor;
                    }
                }
            }

            image.Dispose();

            if (colorCount.Count == 0)
                return (255, 255, 255);

            var dominantColor = colorCount.Aggregate((a, b) => a.Value > b.Value ? a : b).Key;
            return (dominantColor.R, dominantColor.G, dominantColor.B);
        }
        catch (Exception ex)
        {
            // Log or handle exceptions as needed
            Console.WriteLine($"Error processing image from {imagePath}. Error: {ex.Message}");
            return (255, 255, 255);  // Default to white if an exception occurs
        }
    }

    private static async Task<Bitmap> LoadImageAsync(string imagePath)
    {
        if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(imagePath);
            using var stream = await response.Content.ReadAsStreamAsync();
            return new Bitmap(stream);
        }
        else
        {
            return new Bitmap(imagePath);
        }
    }

    private static async Task HandleDiscordExceptionAsync(SocketCommandContext context, SocketUser trader, HttpException ex)
    {
        string message = string.Empty;
        switch (ex.DiscordCode)
        {
            case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
                {
                    // Check if the exception was raised due to missing "Send Messages" or "Manage Messages" permissions. Nag the bot owner if so.
                    var permissions = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);
                    if (!permissions.SendMessages)
                    {
                        // Nag the owner in logs.
                        message = "You must grant me \"Send Messages\" permissions!";
                        Base.LogUtil.LogError(message, "QueueHelper");
                        return;
                    }
                    if (!permissions.ManageMessages)
                    {
                        var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                        var owner = app.Owner.Id;
                        message = $"<@{owner}> You must grant me \"Manage Messages\" permissions!";
                    }
                }
                break;
            case DiscordErrorCode.CannotSendMessageToUser:
                {
                    // The user either has DMs turned off, or Discord thinks they do.
                    message = context.User == trader ? "You must enable private messages in order to be queued!" : "The mentioned user must enable private messages in order for them to be queued!";
                }
                break;
            default:
                {
                    // Send a generic error message.
                    message = ex.DiscordCode != null ? $"Discord error {(int)ex.DiscordCode}: {ex.Reason}" : $"Http error {(int)ex.HttpCode}: {ex.Message}";
                }
                break;
        }
        await context.Channel.SendMessageAsync(message).ConfigureAwait(false);
    }

    public static (string, Embed) CreateLGLinkCodeSpriteEmbed(List<Pictocodes> lgcode)
    {
        int codecount = 0;
        List<System.Drawing.Image> spritearray = new();
        foreach (Pictocodes cd in lgcode)
        {


            var showdown = new ShowdownSet(cd.ToString());
            var sav = SaveUtil.GetBlankSAV(EntityContext.Gen7b, "pip");
            PKM pk = sav.GetLegalFromSet(showdown).Created;
            System.Drawing.Image png = pk.Sprite();
            var destRect = new Rectangle(-40, -65, 137, 130);
            var destImage = new Bitmap(137, 130);

            destImage.SetResolution(png.HorizontalResolution, png.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(png, destRect, 0, 0, png.Width, png.Height, GraphicsUnit.Pixel);

            }
            png = destImage;
            spritearray.Add(png);
            codecount++;
        }
        int outputImageWidth = spritearray[0].Width + 20;

        int outputImageHeight = spritearray[0].Height - 65;

        Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(outputImage))
        {
            graphics.DrawImage(spritearray[0], new Rectangle(0, 0, spritearray[0].Width, spritearray[0].Height),
                new Rectangle(new Point(), spritearray[0].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[1], new Rectangle(50, 0, spritearray[1].Width, spritearray[1].Height),
                new Rectangle(new Point(), spritearray[1].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[2], new Rectangle(100, 0, spritearray[2].Width, spritearray[2].Height),
                new Rectangle(new Point(), spritearray[2].Size), GraphicsUnit.Pixel);
        }
        System.Drawing.Image finalembedpic = outputImage;
        var filename = $"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png";
        finalembedpic.Save(filename);
        filename = System.IO.Path.GetFileName($"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png");
        Embed returnembed = new EmbedBuilder().WithTitle($"{lgcode[0]}, {lgcode[1]}, {lgcode[2]}").WithImageUrl($"attachment://{filename}").Build();
        return (filename, returnembed);
    }

}
