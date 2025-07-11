using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class JokeModule : ModuleBase<SocketCommandContext>
{
    private readonly Random _random = new Random();

    private readonly List<string> _jokes = new List<string>
    {
        "You're so ugly, when your mom dropped you off at school, she got a fine for littering.",
        "If laughter is the best medicine, your face must be curing the world.",
        "You're so ugly, you scared the crap out of the toilet.",
        "I'd like to see things from your perspective, but I can't seem to get my head that far up my ass.",
        "I thought of you today. It reminded me to take out the trash.",
        "If your brain was dynamite, there wouldn’t be enough to blow your hat off.",
        "You're the reason the gene pool needs a lifeguard.",
        "I'd agree with you, but then we'd both be wrong.",
        "Is your ass jealous of the amount of crap that comes out of your mouth?",
        "I’d slap you, but that would be animal abuse.",
        "I’d tell you to go to hell, but I never want to see you again.",
        "The only way you'll ever get laid is if you crawl up a chicken's ass and wait.",
        "I’d call you a tool, but even they serve a purpose.",
        "You're not good looking enough to be that stupid.",
        "You're about as useful as a screen door on a submarine.",
        "Even Bob Ross couldn't paint you a pretty picture.",
        "If you were any less intelligent, you’d have to be watered twice a week.",
        "I'd challenge you to a battle of wits, but I see you're unarmed.",
        "If ignorance is bliss, you must be the happiest person on Earth.",
        "My dad died when we couldn’t remember his blood type. As he died, he kept insisting for us to “be positive,” but it’s hard without him.",
        "When does a joke become a dad joke? When it leaves and never comes back.",
        "What does your dad have in common with Nemo? They both can’t be found.",
        "You don’t need a parachute to go skydiving. You need a parachute to go skydiving twice.",
        "A mother at the playground asked me which one was mine, and I just told her that I haven't decided yet.",
        "I childproofed my house... but somehow they still manage to get back in!",
        "My dentist said “this will hurt a little,” and I just nodded. He then said he slept with my wife. Yes, it did hurt.",
        "What is the worst combination of illnesses? Alzheimer’s and diarrhea. You’re running but can’t remember where the hell you're going.",
        "I was raised as an only child, which really sucked for my sister.",
        "This chick on a date loved that I worked with animals. She asked me if I was a vet and I told her “No, I’m a butcher”",
        "My wife says she wants another baby. I'm so glad because I also really don't like the first one.",
        "A new study recently found that humans eat more bananas than monkeys. It's true. I can't remember the last time I ate a monkey.",
        "I just read that in New York, someone gets stabbed every 52 seconds. Poor guy.",
        "I made a website for orphans. It doesn’t have a home page.",
        "What do you call cheap circumcision? A rip-off.",
        "If a guy remembers the color of your eyes after the first date, chances are you have small boobs.",
        "My daughter asked me how stars die. I told her it was usually from an overdose.",
        "The doctor gave me one year to live, so I shot him. The judge gave me 15 years. Problem solved.",
        "I was playing chess with my friend and he said, “Let’s make this interesting.” So we stopped playing chess.",
        "What’s the first thing you should do if an epileptic is having a seizure in the bathtub? Throw in your dirty laundry.",
        "What’s the difference between a fetus and a jar of pickles? Pickles aren't as tasty in a jar.",
        "Have you ever tried Ethiopian food? Neither have they.",
        "How do you make the world’s greatest Harlem Shake? Throw a flashbang into a room full of epileptics.",
        "I hate it when I'm driving in a school zone, and the speedbump starts screaming.",
        "Just like a little boy with cancer, dark humor never gets old.",
        "A joke becomes a dad joke when it leaves and never comes back.",
        "I asked Siri why I’m still single. It activated the front camera.",
        "A nun in a wheelchair is known as virgin mobile.",
        "I asked my girlfriend if I was the only one she’s been with. She said, “Yes. The others were at least sevens.”",
        "You know people don’t like you when you get handed the camera for group photos.",
        "You’re not completely useless because you can serve as a bad example.",
        "You can’t fool an aborted baby. It wasn’t born yesterday.",
        "I’m starting to think Pearl Harbor was an accident going by how terrible Asians are at driving.",
        "I surprised a blind person by leaving a plunger in the toilet.",
        "One man’s trash is another man’s treasure. The worst way to find out you’re adopted.",
        "Every zodiac sign has a signature hairstyle except for cancer.",
        "A deaf gynecologist is also known as a lip reader.",
        "Stephen Hawking doesn’t do comedy shows. He can’t do stand-up. Also, he's dead now, so there's that...",
        "Pimps and farmers have one thing in common. They need a hoe to stay in business.",
        "Sally fell off the swing because she didn’t have arms.",
        "An alcoholic and a necrophiliac have one thing in common. They both like to crack open a cold one.",
        "I had a crush on my teacher. It was confusing because I was homeschooled.",
        "I love telling jokes about orphans. It's not like they're going to tell their parents on me.",
        "The difference between a light bulb and a pregnant woman is that you can’t unscrew the pregnant woman.",
        "The difference between a knife and your life is that a knife actually has a point.",
        "Peanut butter and strippers have one thing in common. They both spread for bread.",
        "My husband is mad that I have no sense of direction. So I packed up my stuff and right.",
        "My wife told me she’ll slam my head into the keyboard if I don’t get off the computer. I’m not too worried though. I think she’s jokindkdkslalkdl",
        "When I see the names of lovers engraved on a tree, I don’t find it cute or romantic. I find it weird how many people take knives with them on dates.",
        "My mother said one man’s trash is another man’s treasure. Turns out I’m adopted.",
        "What's the difference between Paul Walker and a computer? I actually care when my computer crashes.",
        "So I suggested to my wife that she'd look sexier with her hair back… Which is apparently an insensitive thing to say to a cancer patient.",
        "How many feminists does it take to change a light bulb? Don't be stupid, feminists can't change anything.",
        "How do you get a nun pregnant? Dress her up like an altarboy.",
        "What does a storm cloud wear under his raincoat? Thunderwear.",
        "Why did the soccer player take so long to eat dinner? Because he thought he couldn’t use his hands.",
        "Why isn't there a pregnant Barbie doll? Because Ken came in another box.",
        "Why did the snowman suddenly smile? He could see the snowblower coming.",
        "What did Cinderella say to Prince Charming? Want to see if it fits?",
        "How did Burger King get Dairy Queen pregnant? He forgot to wrap his Whopper.",
        "Which animal has the largest chest? A Z-bra.",
        "My wife asked me to spoon in bed, but I’d rather fork.",
        "What does the horny toad say? Rub it.",
        "What does a hot dog use for protection? Condoments.",
        "What does a robot do after a one-night stand? He nuts and bolts.",
        "What do you call an Italian hooker? A pasta-tute.",
        "What's the difference between a snowman and a snow woman? Snowballs.",
        "Why did the male chicken wear underwear on its head? Because its pecker was on its face.",
        "Why couldn’t the lizard get a girlfriend? Because he had a reptile dysfunction.",
        "What kind of bees produce milk? Boo-bees.",
        "What's the difference between 'Oooh!' and 'Aaah!'? About three inches.",
        "What is Peter Pan’s favorite place to eat out? Wendy’s.",
        "Why did the mermaid wear seashells? She outgrew her b-shells.",
        "What does one boob say to the other boob? If we don't get support, people will think we're nuts.",
        "What did Cinderella do when she got to the ball? She gagged.",
        "Why did the sperm cross the road? Because I put on the wrong sock this morning.",
        "Do you work at Dick’s? Because you’re sporting the goods.",
        "What's the difference between a woman's husband and her boyfriend? 60 minutes.",
        "What's the difference between a microwave and a woman? A man will actually press and pull a microwave's buttons and knobs.",
        "Are you Little Caesars? Because I'm hot and I'm ready.",
        "What's the difference between you and an egg? An egg gets laid.",
        "What did the toaster say to the slice of bread? I want you inside me.",
        "Are you a firefighter? Because you make me hot and leave me wet.",
        "Are you my homework? Because I'm definitely not doing you",
        "Why did the pool table scream? It got hit in its balls.",
        "I asked my wife if she ever fantasizes about me, and she said yes – about me taking out the trash, mowing the lawn, and doing the dishes. Talk about living in a fantasy lol.",
        "Did you hear about the guy who died of a Viagra overdose? They couldn't close his casket.",
        "What did the elephant ask the naked man? How do you breathe out of that thing.",
        "What's Moby Dick's dad's name? Papa Boner.",
        "What's green and smells like pork? Kermit's finger."



    };

    [Command("joke")]
    [Alias("lol", "insult")]
    [Summary("Tells a random joke or insults the user.")]
    public async Task InsultAsync()
    {
        int index = _random.Next(_jokes.Count);
        string insult = _jokes[index];

        await ReplyAsync(insult);
    }
}
