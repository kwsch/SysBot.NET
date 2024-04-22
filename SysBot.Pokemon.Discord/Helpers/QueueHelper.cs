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

    public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser trader, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isHiddenTrade = false, bool isMysteryEgg = false, List<Pictocodes>? lgcode = null, bool ignoreAutoOT = false)
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

            var result = await AddToTradeQueue(context, trade, code, trainer, sig, routine, isBatchTrade ? PokeTradeType.Batch : type, trader, isBatchTrade, batchTradeNumber, totalBatchTrades, isHiddenTrade, isMysteryEgg, lgcode, ignoreAutoOT).ConfigureAwait(false);
        }
        catch (HttpException ex)
        {
            await HandleDiscordExceptionAsync(context, trader, ex).ConfigureAwait(false);
        }
    }

    public static Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, bool ignoreAutoOT = false)
    {
        return AddToQueueAsync(context, code, trainer, sig, trade, routine, type, context.User, ignoreAutoOT: ignoreAutoOT);
    }

    private static async Task<TradeQueueResult> AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName, RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, bool isBatchTrade, int batchTradeNumber, int totalBatchTrades, bool isHiddenTrade, bool isMysteryEgg = false, List<Pictocodes>? lgcode = null, bool ignoreAutoOT = false)
    {
        var user = trader;
        var userID = user.Id;
        var name = user.Username;

        var trainer = new PokeTradeTrainerInfo(trainerName, userID);
        var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, trader, batchTradeNumber, totalBatchTrades, isMysteryEgg, lgcode);
        var uniqueTradeID = GenerateUniqueTradeID();
        var detail = new PokeTradeDetail<T>(pk, trainer, notifier, t, code, sig == RequestSignificance.Favored, lgcode, batchTradeNumber, totalBatchTrades, isMysteryEgg, uniqueTradeID, ignoreAutoOT);
        var trade = new TradeEntry<T>(detail, userID, PokeRoutineType.LinkTrade, name, uniqueTradeID);
        var strings = GameInfo.GetStrings(1);
        var hub = SysCord<T>.Runner.Hub;
        var Info = hub.Queues.Info;
        var canAddMultiple = isBatchTrade || sig == RequestSignificance.None;
        var added = Info.AddToTradeQueue(trade, userID, canAddMultiple);
        bool useTypeEmojis = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MoveTypeEmojis;
        string maleEmojiString = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MaleEmoji.EmojiString;
        string femaleEmojiString = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.FemaleEmoji.EmojiString;
        bool showScale = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowScale;
        bool showTeraType = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowTeraType;
        bool showLevel = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowLevel;
        bool showAbility = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowAbility;
        bool showNature = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowNature;
        bool showIVs = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowIVs;
        int totalTradeCount = 0;
        TradeCodeStorage.TradeCodeDetails? tradeDetails = null;
        TradeCodeStorage? tradeCodeStorage = null;
        if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            tradeCodeStorage = new TradeCodeStorage();
            totalTradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            tradeDetails = tradeCodeStorage.GetTradeDetails(trader.Id);
        }
        string otText = tradeDetails?.OT != null ? $"OT: {tradeDetails?.OT}" : "";
        string tidText = tradeDetails?.TID != 0 ? $"TID: {tradeDetails?.TID}" : "";
        if (added == QueueResultAdd.AlreadyInQueue)
        {
            return new TradeQueueResult(false);
        }
        var typeEmojis = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.CustomTypeEmojis
                             .Where(e => !string.IsNullOrEmpty(e.EmojiCode))
                             .ToDictionary(
                                 e => e.MoveType,
                                 e => $"{e.EmojiCode}"
                             );

        // Basic Pokémon details
        int[] ivs = pk.IVs;
        ushort[] moves = new ushort[4];
        pk.GetMoves(moves.AsSpan());
        List<int> movePPs = [pk.Move1_PP, pk.Move2_PP, pk.Move3_PP, pk.Move4_PP];
        List<string> moveNames = [];
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i] == 0) continue;
            string moveName = GameInfo.MoveDataSource.FirstOrDefault(m => m.Value == moves[i])?.Text ?? "";
            byte moveTypeId = MoveInfo.GetType(moves[i], default);
            MoveType moveType = (MoveType)moveTypeId;
            string formattedMove = $"{moveName} ({movePPs[i]}pp)";
            if (useTypeEmojis && typeEmojis.TryGetValue(moveType, out var moveEmoji))
            {
                formattedMove = $"{moveEmoji} {formattedMove}";
            }
            moveNames.Add($"\u200B{formattedMove}"); // Adding a zero-width space for formatting purposes if needed
        }
        int level = pk.CurrentLevel;

        // Pokémon appearance and type details
        string teraTypeString = "", scaleText = "", abilityName, natureName, speciesName, formName, speciesAndForm, heldItemName, ballName, formDecoration = "";
        byte scaleNumber = 0;
        if (pk is PK9 pk9)
        {
            teraTypeString = GetTeraTypeString(pk9);
            scaleText = $"{PokeSizeDetailedUtil.GetSizeRating(pk9.Scale)}";
            scaleNumber = pk9.Scale;
        }

        // Pokémon identity and special attributes
        abilityName = GameInfo.AbilityDataSource.FirstOrDefault(a => a.Value == pk.Ability)?.Text ?? "";
        natureName = GameInfo.NatureDataSource.FirstOrDefault(n => n.Value == (int)pk.Nature)?.Text ?? "";
        speciesName = GameInfo.GetStrings(1).Species[pk.Species];
        string alphaMarkSymbol = string.Empty;
        string mightyMarkSymbol = string.Empty;
        string markTitle = string.Empty;
        if (pk is IRibbonSetMark9 ribbonSetMark)
        {
            alphaMarkSymbol = ribbonSetMark.RibbonMarkAlpha ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.AlphaMarkEmoji.EmojiString : string.Empty;
            mightyMarkSymbol = ribbonSetMark.RibbonMarkMightiest ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MightiestMarkEmoji.EmojiString : string.Empty;
        }
        if (pk is IRibbonIndex ribbonIndex)
        {
            AbstractTrade<T>.HasMark(ribbonIndex, out RibbonIndex result, out markTitle);
        }
        string alphaSymbol = (pk is IAlpha alpha && alpha.IsAlpha) ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.AlphaPLAEmoji.EmojiString : string.Empty;
        string shinySymbol = pk.ShinyXor == 0 ? "◼ " : pk.IsShiny ? "★ " : string.Empty;
        string genderSymbol = GameInfo.GenderSymbolASCII[pk.Gender];
        string displayGender = genderSymbol switch
        {
            "M" => !string.IsNullOrEmpty(maleEmojiString) ? maleEmojiString : "(M) ",
            "F" => !string.IsNullOrEmpty(femaleEmojiString) ? femaleEmojiString : "(F) ",
            _ => ""
        };
        string mysteryGiftEmoji = pk.FatefulEncounter ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MysteryGiftEmoji.EmojiString : "";
        displayGender += alphaSymbol + mightyMarkSymbol + alphaMarkSymbol + mysteryGiftEmoji;
        formName = ShowdownParsing.GetStringFromForm(pk.Form, strings, pk.Species, pk.Context);
        string toppingName = "";
        if (pk.Species == (int)Species.Alcremie && pk is IFormArgument formArgument)
        {
            AlcremieDecoration topping = (AlcremieDecoration)formArgument.FormArgument;
            toppingName = $"-{topping}";
            formName += toppingName;
        }
        speciesAndForm = $"**{shinySymbol}{speciesName}{(string.IsNullOrEmpty(formName) ? "" : $"-{formName}")}{(!string.IsNullOrEmpty(markTitle) ? markTitle : "")} {displayGender}**";
        heldItemName = strings.itemlist[pk.HeldItem];
        ballName = strings.balllist[pk.Ball];

        // Request type flags
        bool isCloneRequest = type == PokeRoutineType.Clone;
        bool isDumpRequest = type == PokeRoutineType.Dump;
        bool FixOT = type == PokeRoutineType.FixOT;
        bool isSpecialRequest = type == PokeRoutineType.SeedCheck;

        // Display elements
        string ivsDisplay = $"{ivs[0]}/{ivs[1]}/{ivs[2]}/{ivs[3]}/{ivs[4]}/{ivs[5]}";
        string MetDate = $"{pk.MetDate}";
        string movesDisplay = string.Join("\n", moveNames);
        string shinyEmoji = pk.IsShiny ? "✨ " : "";
        string pokemonDisplayName = pk.IsNicknamed ? pk.Nickname : GameInfo.GetStrings(1).Species[pk.Species];

        // Queue position and ETA calculation
        var position = Info.CheckPosition(userID, uniqueTradeID, type);
        var botct = Info.Hub.Bots.Count;
        var baseEta = position.Position > botct ? Info.Hub.Config.Queues.EstimateDelay(position.Position, botct) : 0;
        var etaMessage = $"Estimated: {baseEta:F1} min(s) for trade {batchTradeNumber}/{totalBatchTrades}.";

        // Determining trade title based on trade type
        string tradeTitle;
        tradeTitle = isMysteryEgg ? "✨ Shiny Mystery Egg ✨" :
                     isBatchTrade ? $"Batch Trade #{batchTradeNumber} - {shinyEmoji}{pokemonDisplayName}" :
                     FixOT ? "FixOT Request" :
                     isSpecialRequest ? "Special Request" :
                     isCloneRequest ? "Clone Pod Activated!" :
                     isDumpRequest ? "Pokémon Dump" :
                     "";

        // Prepare embed details for Discord message
        (string embedImageUrl, DiscordColor embedColor) = await PrepareEmbedDetails(pk);

        // Adjust image URL based on request type
        embedImageUrl = isMysteryEgg ? "https://raw.githubusercontent.com/bdawg1989/sprites/main/mysteryegg2.png" :
                        isDumpRequest ? "https://raw.githubusercontent.com/bdawg1989/sprites/main/AltBallImg/128x128/dumpball.png" :
                        isCloneRequest ? "https://raw.githubusercontent.com/bdawg1989/sprites/main/clonepod.png" :
                        isSpecialRequest ? "https://raw.githubusercontent.com/bdawg1989/sprites/main/specialrequest.png" :
                        FixOT ? "https://raw.githubusercontent.com/bdawg1989/sprites/main/AltBallImg/128x128/rocketball.png" :
                        embedImageUrl; // Keep original if none of the above

        // Prepare held item image URL if available
        string heldItemUrl = string.Empty;
        if (!string.IsNullOrWhiteSpace(heldItemName))
        {
            heldItemName = heldItemName.ToLower().Replace(" ", "");
            heldItemUrl = $"https://serebii.net/itemdex/sprites/{heldItemName}.png";
        }

        // Checking if the image URL points to a local file
        bool isLocalFile = File.Exists(embedImageUrl);
        string userName = user.Username;
        string isPkmShiny = pk.IsShiny ? "Shiny " : "";

        // Building the embed author name based on the type of trade
        string authorName = isMysteryEgg || FixOT || isCloneRequest || isDumpRequest || isSpecialRequest || isBatchTrade ?
                            $"{userName}'s {tradeTitle}" :
                            $"{userName}'s {isPkmShiny}{pokemonDisplayName}";

        // Build footer
        string footerText = $"Current Position: {position.Position}";
        string userDetailsText = "";

        if (totalTradeCount > 0)
        {
            userDetailsText = $"Trades: {totalTradeCount}";
        }

        if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.StoreTradeCodes && tradeCodeStorage != null)
        {
            TradeCodeStorage.TradeCodeDetails userDetails = tradeCodeStorage.GetTradeDetails(trader.Id);

            if (!string.IsNullOrEmpty(tradeDetails?.OT))
            {
                userDetailsText += $" | OT: {tradeDetails?.OT}";
            }
            if (userDetails?.TID != null)
            {
                userDetailsText += $" | TID: {userDetails?.TID}";
            }
        }

        if (!string.IsNullOrEmpty(userDetailsText))
        {
            footerText += $"\n{userDetailsText}";
        }
        footerText += $"\n{etaMessage}";

        // Initializing the embed builder with general settings
        var embedBuilder = new EmbedBuilder()
            .WithColor(embedColor)
            .WithImageUrl(isLocalFile ? $"attachment://{Path.GetFileName(embedImageUrl)}" : embedImageUrl)
            .WithFooter(footerText)
            .WithAuthor(new EmbedAuthorBuilder()
                .WithName(authorName)
                .WithIconUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithUrl("https://genpkm.com"));

        // Adding additional text to the embed, if any
        string additionalText = string.Join("\n", SysCordSettings.Settings.AdditionalEmbedText);
        if (!string.IsNullOrEmpty(additionalText))
        {
            embedBuilder.AddField("\u200B", additionalText, inline: false);
        }

        // Constructing the content of the embed based on the trade type
        if (!isMysteryEgg && !isCloneRequest && !isDumpRequest && !FixOT && !isSpecialRequest)
        {
            // Preparing content for normal trades
            string leftSideContent = $"**Trainer:** {user.Mention}\n";
            leftSideContent +=
                (pk.Version is GameVersion.SL or GameVersion.VL && showTeraType ? $"**Tera Type:** {teraTypeString}\n" : "") +
                (pk.Version is GameVersion.SL or GameVersion.VL && showScale ? $"**Scale:** {scaleText} ({scaleNumber})\n" : "") +
                (showLevel ? $"**Level:** {level}\n" : "") +
                (showLevel ? $"**MetDate:** {MetDate}\n" : "") +
                (showAbility ? $"**Ability:** {abilityName}\n" : "") +
                (showNature ? $"**Nature**: {natureName}\n" : "") +
                (showIVs ? $"**IVs**: {ivsDisplay}\n" : "");

            leftSideContent = leftSideContent.TrimEnd('\n');
            embedBuilder.AddField($"{speciesAndForm}", leftSideContent, inline: true);
            embedBuilder.AddField("\u200B", "\u200B", inline: true); // Spacer
            embedBuilder.AddField("**Moves:**", movesDisplay, inline: true);
        }
        else
        {
            // Preparing content for special types of trades
            string specialDescription = $"**Trainer:** {user.Mention}\n" +
                                        (isMysteryEgg ? "Mystery Egg" : isSpecialRequest ? "Special Request" : isCloneRequest ? "Clone Request" : FixOT ? "FixOT Request" : "Dump Request");
            embedBuilder.AddField("\u200B", specialDescription, inline: false);
        }

        // Adding thumbnails for clone and special requests, or held items
        if (isCloneRequest || isSpecialRequest)
        {
            embedBuilder.WithThumbnailUrl("https://raw.githubusercontent.com/bdawg1989/sprites/main/profoak.png");
        }
        else if (!string.IsNullOrEmpty(heldItemUrl))
        {
            embedBuilder.WithThumbnailUrl(heldItemUrl);
        }

        if (!isHiddenTrade)
        {
            // Building and sending the embed message
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
                    userBatchTradeMaxDetailId[userID] = Math.Max(userBatchTradeMaxDetailId.GetValueOrDefault(userID), detail.ID);
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
        }
        else
        {
            var message = $"{trader.Mention} - Added to the LinkTrade queue. Current Position: {position.Position}. Receiving: {speciesName}.\n{etaMessage}";
            await context.Channel.SendMessageAsync(message);
        }

        return new TradeQueueResult(true);
    }

    private static int GenerateUniqueTradeID()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomValue = new Random().Next(1000);
        int uniqueTradeID = (int)(timestamp % int.MaxValue) * 1000 + randomValue;
        return uniqueTradeID;
    }

    private static string GetTeraTypeString(PK9 pk9)
    {
        if (pk9.TeraTypeOverride == (MoveType)TeraTypeUtil.Stellar)
        {
            return "Stellar";
        }
        else if ((int)pk9.TeraType == 99) // Terapagos
        {
            return "Stellar";
        }
        else
        {
            return pk9.TeraType.ToString();
        }
    }

    private static string GetImageFolderPath()
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string imagesFolder = Path.Combine(baseDirectory, "Images");

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

    private static async Task<(string, DiscordColor)> PrepareEmbedDetails(T pk)
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

        var strings = GameInfo.GetStrings(1);
        string ballName = strings.balllist[pk.Ball];
        if (ballName.Contains("(LA)"))
        {
            ballName = "la" + ballName.Replace(" ", "").Replace("(LA)", "").ToLower();
        }
        else
        {
            ballName = ballName.Replace(" ", "").ToLower();
        }

        string ballImgUrl = $"https://raw.githubusercontent.com/bdawg1989/sprites/main/AltBallImg/20x20/{ballName}.png";

        // Check if embedImageUrl is a local file or a web URL
        if (Uri.TryCreate(embedImageUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeFile)
        {
            // Load local image directly
            using var localImage = System.Drawing.Image.FromFile(uri.LocalPath);
            using var ballImage = await LoadImageFromUrl(ballImgUrl);
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
        else
        {
            (System.Drawing.Image finalCombinedImage, bool ballImageLoaded) = await OverlayBallOnSpecies(speciesImageUrl, ballImgUrl);
            embedImageUrl = SaveImageLocally(finalCombinedImage);

            if (!ballImageLoaded)
            {
                Console.WriteLine($"Ball image could not be loaded: {ballImgUrl}");
                // await context.Channel.SendMessageAsync($"Ball image could not be loaded: {ballImgUrl}"); // for debugging purposes
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
                return ((System.Drawing.Image)speciesImage.Clone(), false);
            }

            using (ballImage)
            {
                using (var graphics = Graphics.FromImage(speciesImage))
                {
                    var ballPosition = new Point(speciesImage.Width - ballImage.Width, speciesImage.Height - ballImage.Height);
                    graphics.DrawImage(ballImage, ballPosition);
                }

                return ((System.Drawing.Image)speciesImage.Clone(), true);
            }
        }
    }
    private static async Task<System.Drawing.Image> OverlaySpeciesOnEgg(string eggImageUrl, string speciesImageUrl)
    {
        System.Drawing.Image eggImage = await LoadImageFromUrl(eggImageUrl);
        System.Drawing.Image speciesImage = await LoadImageFromUrl(speciesImageUrl);
        double scaleRatio = Math.Min((double)eggImage.Width / speciesImage.Width, (double)eggImage.Height / speciesImage.Height);
        Size newSize = new Size((int)(speciesImage.Width * scaleRatio), (int)(speciesImage.Height * scaleRatio));
        System.Drawing.Image resizedSpeciesImage = new Bitmap(speciesImage, newSize);
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
        eggImage.Dispose();
        return finalImage;
    }

    private static async Task<System.Drawing.Image?> LoadImageFromUrl(string url)
    {
        using HttpClient client = new();
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

    private static async Task ScheduleFileDeletion(string filePath, int delayInMilliseconds, int batchTradeId = -1)
    {
        if (batchTradeId != -1)
        {
            // If this is part of a batch trade, add the file path to the dictionary
            if (!batchTradeFiles.TryGetValue(batchTradeId, out List<string>? value))
            {
                value = ([]);
                batchTradeFiles[batchTradeId] = value;
            }

            value.Add(filePath);
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
        List<System.Drawing.Image> spritearray = [];
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
        var filename = $"{Directory.GetCurrentDirectory()}//finalcode.png";
        finalembedpic.Save(filename);
        filename = Path.GetFileName($"{Directory.GetCurrentDirectory()}//finalcode.png");
        Embed returnembed = new EmbedBuilder().WithTitle($"{lgcode[0]}, {lgcode[1]}, {lgcode[2]}").WithImageUrl($"attachment://{filename}").Build();
        return (filename, returnembed);
    }
}
