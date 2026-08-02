using UnityEngine;

namespace BottleBattle
{
    /// <summary>
    /// Small persistent coin wallet. The testing balance is reset on every app launch
    /// and can be replaced with the production economy later.
    /// </summary>
    public static class CoinWallet
    {
        private const string CoinBalanceKey = "BottleBattle.Coins";
        private const int TestingBalance = 99999;

        public static int Balance => PlayerPrefs.GetInt(CoinBalanceKey, TestingBalance);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GiveTestingBalance()
        {
            PlayerPrefs.SetInt(CoinBalanceKey, TestingBalance);
            PlayerPrefs.Save();
        }

        public static bool TrySpend(int amount)
        {
            int balance = Balance;
            if (amount < 0 || balance < amount)
            {
                return false;
            }

            PlayerPrefs.SetInt(CoinBalanceKey, balance - amount);
            PlayerPrefs.Save();
            return true;
        }
    }
}
