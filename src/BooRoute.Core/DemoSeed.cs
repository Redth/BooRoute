namespace BooRoute.Core;

public static class DemoSeed
{
    public static DemoState Create(DateTimeOffset localNow)
    {
        string[] names =
        [
            "The Pumpkin Porch", "Moonlight Cottage", "The Friendly Ghost",
            "Cobweb Corner", "Little Monsters Lodge", "The Witch's Kitchen",
            "Lantern Lane Lookout", "The Bat Attic", "Sugar Skull House", "The Last Pumpkin"
        ];
        string[] descriptions =
        [
            "Follow the smiling pumpkins to our porch. Paper treat bags and a friendly welcome await.",
            "A quiet stop with glowing lanterns, no flashing lights, and plenty of cheerful pumpkins.",
            "Our ghosts are made of bedsheets, and all their scares are optional. Little visitors welcome!",
            "A cobweb-covered doorway and a creaking gate. Brave visitors can meet our cardboard spider.",
            "A gentle monster party for the smallest trick-or-treaters. Costumes encouraged, never required.",
            "The cauldron is bubbling with pretend potions. Ask our resident witch for a Halloween riddle.",
            "A lantern-lit path leads to the door. A relaxed visit with decorations but no sudden surprises.",
            "Bats, shadows, and a spooky soundtrack. This stop is best for families who enjoy a fright.",
            "Bright paper decorations and friendly skeletons. Stop by for a festive neighborhood hello.",
            "The pumpkin patch has gone to sleep for the evening. Enjoy the decorations from the path."
        ];
        var opens = TimeOnly.FromDateTime(localNow.AddHours(-2).DateTime);
        var closes = TimeOnly.FromDateTime(localNow.AddHours(4).DateTime);
        var houses = Enumerable.Range(0, names.Length).Select(index => new House(
            Guid.NewGuid(), names[index], $"{index + 1} Make-Believe Lane",
            descriptions[index], opens, closes, (ScareLevel)(index % 3),
            index is 3 or 7 ? HouseStatus.OutOfTreats : index == 9 ? HouseStatus.Closed : HouseStatus.LightsOn,
            index is 0 or 1 or 2 or 4 or 6, index % 3 != 1)).ToList();
        return new(houses, []);
    }
}
