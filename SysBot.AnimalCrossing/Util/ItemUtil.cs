namespace SysBot.AnimalCrossing
{
    public static class ItemUtil
    {
        public static int GetItemDropOption(this Item item)
        {
            if (Item.DIYRecipe == item.ItemId)
                return 1;
            if (item.IsWrapped)
                return 0;

            return 1;
        }

        public static bool ShouldWrapItem(this Item item)
        {
            if (Item.DIYRecipe == item.ItemId)
                return false;

            return true;
        }
    }
}
