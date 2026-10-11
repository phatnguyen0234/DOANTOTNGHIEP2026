public interface ITradeReceiver
{
    bool CanAcceptItem(ItemData item);
    void ProcessTransaction(ItemData item, int amount, Inventory playerInventory, PlayerMoney playerMoney);
}