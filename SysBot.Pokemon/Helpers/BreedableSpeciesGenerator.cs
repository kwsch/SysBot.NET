using PKHeX.Core;
using System;
using System.IO;
using System.Collections.Generic;

public static class BreedableSpeciesGenerator
{
    private static readonly List<string> BreedableSpeciesNames = new List<string>
    {
        "Applin","Arrokuda","Axew","Azurill","Bagon","Basculin","Beldum","Bellsprout","Bergmite","Bidoof","Blitzle","Bounsweet","Bramblin",
        "Bronzor","Budew","Buizel","Bulbasaur","Cacnea","Carbink","Cetoddle","Charcadet","Charmander","Chespin","Chewtle",
        "Chimchar","Chinchou","Clauncher","Clefable","Cleffa","Clodsire","Combee","Cottonee","Cramorant","Cranidos","Croagunk","Cubchoo",
        "Cufant","Cutiefly","Cyndaquil","Deerling","Deino","Dewpider","Diglett","Diglett-Alola","Doduo","Donphan","Dratini","Dreepy","Drifloon","Drilbur",
        "Drowzee","Ducklett","Dunsparce","Eevee","Ekans","Elekid","Espurr","Exeggcute","Feebas","Fennekin","Fidough",
        "Finizen","Flabebe","Flabebe-Orange","Flabebe-Blue","Flabebe-White","Flabebe-Yellow","Fletchling","Flittle","Fomantis","Foongus","Frigibax","Froakie","Fuecoco",
        "Gastly","Geodude","Geodude-Alola","Gible","Gligar","Glimmet","Goldeen","Golett","Goomy","Gossifleur","Gothita","Greavard","Grimer","Grimer-Alola",
        "Grookey","Growlithe","Growlithe-Hisui","Grubbin","Gulpin","Happiny","Hatenna","Hippopotas","Hoothoot","Hoppip","Horsea","Houndour","Igglybuff","Inkay","Joltik","Jumpluff","Klawf","Koffing","Krabby",
        "Kricketot","Kubfu","Larvesta","Lechonk","Litleo","Litten","Litwick","Lokix","Lotad","Lycanroc","Magby","Magikarp","Magnemite",
        "Makuhita","Mankey","Mareep","Marill","Maschiff","Meditite","Meowth","Meowth-Galar","Meowth-Alola","Mienfoo","Minccino","Minun","Morgrem","Mudbray",
        "Munchlax","Murkrow","Nacli","Noibat","Nosepass","Numel","Nymble","Oddish","Oricorio","Orthworm","Oshawott","Pawmi","Pawniard","Petilil",
        "Phanpy","Pichu","Pidgey","Pikipek","Pineco","Piplup","Plusle","Poliwag","Ponyta","Poochyena","Popplio","Psyduck",
        "Quaxly","Ralts","Rellor","Riolu","Rockruff","Rolycoly","Rookidee","Rowlet","Rufflet","Salandit","Sandile","Sandshrew","Sandygast",
        "Scorbunny","Scraggy","Seedot","Seel","Sentret","Sewaddle","Shellder","Shellos","Shieldon","Shinx","Shroodle","Shroomish","Shuppet","Silicobra",
        "Skorupi","Skrelp","Skwovet","Slakoth","Slowpoke","Slowpoke-Galar","Slugma","Smoliv","Snivy","Snom","Snorunt","Snover","Snubbull",
        "Sobble","Solosis","Spearow","Spidops","Spinarak","Spoink","Sprigatito","Squirtle","Stantler","Starly","Sunkern","Swablu","Swanna","Swinub","Tadbulb",
        "Tandemaus","Tauros","Tauros-Paldea-Combat","Tauros-Paldea-Blaze","Tauros-Paldea-Aqua","Teddiursa","Tentacool","Tepig","Timburr","Tinkatink","Toedscool",
        "Torchic","Toxapex","Trapinch","Tynamo","Tyrogue","Varoom","Venonat","Vibrava","Voltorb","Vullaby","Vulpix","Vulpix-Alola","Wattrel","Wiglett",
        "Wingull","Wooper","Yanma","Yungoos","Zorua",
    };

    public static List<ushort> GetBreedableSpeciesForSV()
    {
        var breedableSpecies = new List<ushort>();
        foreach (var name in BreedableSpeciesNames)
        {
            ushort speciesId = ConvertNameToSpeciesId(name);
            if (speciesId != 0) // Assuming 0 is an invalid species ID
                breedableSpecies.Add(speciesId);
        }

        return breedableSpecies;
    }

    private static ushort ConvertNameToSpeciesId(string name)
    {
        if (Enum.TryParse(typeof(Species), name, out var result))
        {
            return (ushort)result;
        }
        // Handle the case where the name is not found in the enum
        // You might want to log this case or handle it as per your application's needs
        Console.WriteLine($"Warning: Species name '{name}' not found in Species enum.");
        return 0; // Assuming 0 is an invalid species ID
    }
}
