using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class InventoryScreenListener : IGameStateManagerListener
    {
        private readonly Action _inventoryScreenOpened;

        public InventoryScreenListener(Action inventoryScreenOpened)
        {
            _inventoryScreenOpened = inventoryScreenOpened;
        }

        public void OnCreateState(GameState gameState)
        {
        }

        public void OnPushState(GameState gameState, bool isTopGameState)
        {
            if (gameState is InventoryState) _inventoryScreenOpened.Invoke();
        }

        public void OnPopState(GameState gameState)
        {
        }

        public void OnCleanStates()
        {
        }

        public void OnSavedGameLoadFinished()
        {
        }
    }
}