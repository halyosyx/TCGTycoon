using System;
using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Every visible label of the store website (STORE_UI_REQUIREMENTS STR-04), edited in the
    /// Inspector. The UXML's text is sample text only; code overwrites every label from here.
    /// </summary>
    [Serializable]
    public sealed class StoreStrings
    {
        [SerializeField] private string _yourBalance = "Your balance";
        [SerializeField] private string _cart = "Cart";
        [SerializeField] private string _allSets = "All sets";
        [SerializeField] private string _searchProducts = "Search products";
        [SerializeField] private string _type = "Type";
        [SerializeField] private string _sort = "Sort";
        [SerializeField] private string _any = "Any";
        [SerializeField] private string _unitPrice = "Unit price";
        [SerializeField] private string _amount = "Amount";
        [SerializeField] private string _total = "Total";
        [SerializeField] private string _addToCart = "Add to cart";
        [SerializeField] private string _comingSoon = "Coming soon";
        [SerializeField] private string _notStockedYet = "Not stocked yet";
        [SerializeField] private string _soldOutTonight = "Sold out tonight";
        [SerializeField] private string _restocksNextPrepNight = "Restocks next Prep Night";

        [SerializeField, Tooltip("{0} = units left.")]
        private string _leftTonightFormat = "{0} left tonight";

        [SerializeField] private string _inPrint = "In print";
        [SerializeField] private string _outOfPrint = "Out of print";
        [SerializeField] private string _yourCart = "Your cart";
        [SerializeField] private string _cartEmpty = "Your cart is empty";
        [SerializeField] private string _packs = "packs";
        [SerializeField] private string _balance = "Balance";
        [SerializeField] private string _balanceAfter = "Balance after";
        [SerializeField] private string _placeOrder = "Place order";
        [SerializeField] private string _orderPlaced = "Order placed";
        [SerializeField] private string _orderPlacedNote = "They're on your table, ready to open.";
        [SerializeField] private string _done = "Done";

        public string YourBalance => _yourBalance;

        public string Cart => _cart;

        public string AllSets => _allSets;

        public string SearchProducts => _searchProducts;

        public string Type => _type;

        public string Sort => _sort;

        public string Any => _any;

        public string UnitPrice => _unitPrice;

        public string Amount => _amount;

        public string Total => _total;

        public string AddToCart => _addToCart;

        public string ComingSoon => _comingSoon;

        public string NotStockedYet => _notStockedYet;

        public string SoldOutTonight => _soldOutTonight;

        public string RestocksNextPrepNight => _restocksNextPrepNight;

        public string LeftTonightFormat => _leftTonightFormat;

        public string InPrint => _inPrint;

        public string OutOfPrint => _outOfPrint;

        public string YourCart => _yourCart;

        public string CartEmpty => _cartEmpty;

        public string Packs => _packs;

        public string Balance => _balance;

        public string BalanceAfter => _balanceAfter;

        public string PlaceOrder => _placeOrder;

        public string OrderPlaced => _orderPlaced;

        public string OrderPlacedNote => _orderPlacedNote;

        public string Done => _done;
    }
}
